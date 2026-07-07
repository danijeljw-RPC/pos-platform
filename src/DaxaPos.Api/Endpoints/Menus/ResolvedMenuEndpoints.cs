using DaxaPos.Api.Authorization;
using DaxaPos.Application.Identity;
using DaxaPos.Application.Pricing;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Menus;

public sealed record ResolvedModifierResponse(Guid Id, string Name, decimal PriceDelta);

public sealed record ResolvedModifierGroupResponse(
    Guid Id,
    string Name,
    int SelectionMin,
    int SelectionMax,
    bool IsRequired,
    int DisplayOrder,
    IReadOnlyList<ResolvedModifierResponse> Modifiers);

public sealed record ResolvedMenuItemResponse(
    Guid ProductId,
    string ProductName,
    int DisplayOrder,
    decimal Price,
    bool IsTaxInclusive,
    string TaxCategoryCode,
    TaxTreatment TaxTreatment,
    IReadOnlyList<ResolvedModifierGroupResponse> ModifierGroups);

public sealed record ResolvedMenuSectionResponse(
    Guid MenuId,
    Guid MenuSectionId,
    string SectionName,
    int DisplayOrder,
    IReadOnlyList<ResolvedMenuItemResponse> Items);

public sealed record ResolvedMenuResponse(Guid LocationId, IReadOnlyList<ResolvedMenuSectionResponse> Sections);

/// <summary>
/// <c>GET /api/v1/menus/resolved?locationId={id}</c> (PLAN-0004 Milestone G) — the sales-screen-ready
/// projection a POS operator reads to know what to sell. Gated <c>.RequireAuthorization()</c> only,
/// deliberately no <see cref="Permissions"/> code and no <c>rejectStaffPin</c> check (approved Human
/// Decision #1) — the plan's other deliberately staff-accessible endpoint, alongside
/// <see cref="Catalog.ProductSoldOutEndpoints"/>, but via a different mechanism (no permission gate
/// at all, rather than an <c>Operational</c>-category permission). A staff-PIN session's own
/// <c>AuthContext.LocationId</c> (bound to its device) must match the requested <paramref
/// name="locationId"/> — same independent check <see cref="Catalog.ProductSoldOutEndpoints"/> uses —
/// an organisation-scoped admin session (<c>LocationId</c> null) has no such restriction.
/// </summary>
/// <remarks>
/// Merge precedence (approved Human Decision #7): an organisation-wide <see cref="Menu"/>
/// (<c>LocationId == null</c>) and a location-specific one may both apply to <paramref
/// name="locationId"/>; for any <see cref="Product"/> appearing in both, the location-specific
/// occurrence wins and the organisation-wide occurrence is dropped entirely (not merged section-by-
/// section — this milestone does not attempt to reconcile two different sections by name).
/// <see cref="MenuAvailabilityRule"/> evaluation: zero active rules means always available; one or
/// more means available only during at least one matching window, evaluated against
/// <see cref="Location.TimeZoneId"/> (never UTC-naively) with <c>Start &lt;= now &lt; End</c>
/// semantics. Fails closed (404) when <see cref="VenueTaxConfiguration"/> is missing for the
/// location, matching <see cref="Tax.VenueTaxConfigurationEndpoints"/>'s own missing-config
/// behaviour (approved Human Decision #5) — this endpoint does not silently default. Only a bare
/// <see cref="Product.BasePrice"/> resolution is performed (no variant, no modifiers) since
/// <see cref="MenuSectionItem"/> carries only a <see cref="Product"/> reference — variant/modifier
/// selection happens at order time (PLAN-0005), not on the menu display itself.
/// </remarks>
public static class ResolvedMenuEndpoints
{
    public static void MapResolvedMenuEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/v1/menus")
            .RequireAuthorization()
            .MapGet("/resolved", GetResolvedMenuAsync);
    }

    private static async Task<IResult> GetResolvedMenuAsync(
        Guid locationId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        // A location-bound session (staff PIN, from its device's registered location) may only
        // resolve its own location's menu — checked independently of the organisation match below,
        // matching ProductSoldOutEndpoints' identical rule.
        if (authContext.LocationId is not null && authContext.LocationId != locationId)
        {
            return Results.NotFound();
        }

        var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == locationId);
        if (location is null || location.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var venueTaxConfiguration = await dbContext.VenueTaxConfigurations.SingleOrDefaultAsync(v => v.LocationId == locationId);
        if (venueTaxConfiguration is null)
        {
            return Results.NotFound();
        }

        var menus = await dbContext.Menus
            .Where(m => m.OrganisationId == authContext.OrganisationId
                && m.IsActive
                && (m.LocationId == null || m.LocationId == locationId))
            .ToListAsync();

        var menuIds = menus.Select(m => m.Id).ToList();

        var activeRules = await dbContext.MenuAvailabilityRules
            .Where(r => menuIds.Contains(r.MenuId) && r.IsActive)
            .ToListAsync();

        // TimeZoneInfo lookup assumes Location.TimeZoneId is a valid IANA/BCL id — the only value
        // possible today is the "UTC" default, since no endpoint yet lets a caller set it (see the
        // TimeZoneId doc comment on Location).
        var locationTimeZone = TimeZoneInfo.FindSystemTimeZoneById(location.TimeZoneId);
        var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, locationTimeZone);
        var nowDayMask = ToDaysOfWeekMask(nowLocal.DayOfWeek);
        var nowTimeOfDay = TimeOnly.FromDateTime(nowLocal.DateTime);

        bool IsMenuAvailable(Guid menuId)
        {
            var rules = activeRules.Where(r => r.MenuId == menuId).ToList();
            if (rules.Count == 0)
            {
                return true;
            }

            return rules.Any(r =>
                (r.DaysOfWeekMask & nowDayMask) != 0
                && nowTimeOfDay >= r.StartTimeLocal
                && nowTimeOfDay < r.EndTimeLocal);
        }

        var availableMenus = menus.Where(m => IsMenuAvailable(m.Id)).ToList();
        var locationMenuIds = availableMenus.Where(m => m.LocationId == locationId).Select(m => m.Id).ToHashSet();
        var orgWideMenuIds = availableMenus.Where(m => m.LocationId == null).Select(m => m.Id).ToHashSet();
        var availableMenuIds = locationMenuIds.Concat(orgWideMenuIds).ToHashSet();

        var sections = await dbContext.MenuSections
            .Where(s => availableMenuIds.Contains(s.MenuId) && s.IsActive)
            .ToListAsync();

        var sectionIds = sections.Select(s => s.Id).ToList();
        var sectionById = sections.ToDictionary(s => s.Id);

        var items = await dbContext.MenuSectionItems
            .Where(i => sectionIds.Contains(i.MenuSectionId))
            .ToListAsync();

        var locationClaimedProductIds = items
            .Where(i => locationMenuIds.Contains(sectionById[i.MenuSectionId].MenuId))
            .Select(i => i.ProductId)
            .ToHashSet();

        var effectiveItems = items
            .Where(i => !orgWideMenuIds.Contains(sectionById[i.MenuSectionId].MenuId)
                || !locationClaimedProductIds.Contains(i.ProductId))
            .ToList();

        var productIds = effectiveItems.Select(i => i.ProductId).Distinct().ToList();
        var productsById = await dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var overridesByProductId = await dbContext.ProductLocationOverrides
            .Where(o => o.LocationId == locationId && productIds.Contains(o.ProductId))
            .ToDictionaryAsync(o => o.ProductId);

        var taxCategoryIds = productsById.Values.Select(p => p.TaxCategoryId).Distinct().ToList();
        var taxCategoriesById = await dbContext.TaxCategories
            .Where(t => taxCategoryIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        var modifierGroupsByProductId = await LoadModifierGroupsByProductIdAsync(dbContext, productIds);

        var resultSections = new List<ResolvedMenuSectionResponse>();

        foreach (var section in sections.OrderBy(s => s.DisplayOrder))
        {
            var sectionItems = new List<ResolvedMenuItemResponse>();

            foreach (var item in effectiveItems.Where(i => i.MenuSectionId == section.Id).OrderBy(i => i.DisplayOrder))
            {
                if (!productsById.TryGetValue(item.ProductId, out var product) || !product.IsActive || product.IsArchived)
                {
                    continue;
                }

                overridesByProductId.TryGetValue(item.ProductId, out var over);
                if (over is not null && (!over.IsAvailable || over.IsSoldOut))
                {
                    continue;
                }

                var priceResult = PriceResolver.Resolve(product, null, [], over, venueTaxConfiguration);
                if (!priceResult.IsSuccess)
                {
                    continue;
                }

                var taxCategory = taxCategoriesById[product.TaxCategoryId];

                sectionItems.Add(new ResolvedMenuItemResponse(
                    product.Id,
                    product.Name,
                    item.DisplayOrder,
                    priceResult.ResolvedPrice!.Amount,
                    priceResult.ResolvedPrice.IsTaxInclusive,
                    taxCategory.Code,
                    taxCategory.TaxTreatment,
                    modifierGroupsByProductId.TryGetValue(product.Id, out var groups)
                        ? groups
                        : []));
            }

            resultSections.Add(new ResolvedMenuSectionResponse(section.MenuId, section.Id, section.Name, section.DisplayOrder, sectionItems));
        }

        return Results.Ok(new ResolvedMenuResponse(locationId, resultSections));
    }

    /// <summary>
    /// Batch-loads active modifier groups/options linked to <paramref name="productIds"/>
    /// (Milestone C.1) — three flat queries regardless of product count, never an N+1 per item.
    /// </summary>
    private static async Task<Dictionary<Guid, List<ResolvedModifierGroupResponse>>> LoadModifierGroupsByProductIdAsync(
        DaxaDbContext dbContext, List<Guid> productIds)
    {
        var links = await dbContext.ProductModifierGroups
            .Where(l => productIds.Contains(l.ProductId))
            .ToListAsync();

        var groupIds = links.Select(l => l.ModifierGroupId).Distinct().ToList();

        var groupsById = await dbContext.ModifierGroups
            .Where(g => groupIds.Contains(g.Id) && g.IsActive)
            .ToDictionaryAsync(g => g.Id);

        var modifiersByGroupId = (await dbContext.Modifiers
                .Where(m => groupIds.Contains(m.ModifierGroupId) && m.IsActive)
                .ToListAsync())
            .GroupBy(m => m.ModifierGroupId)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.Name).ToList());

        var result = new Dictionary<Guid, List<ResolvedModifierGroupResponse>>();

        foreach (var productLinks in links.GroupBy(l => l.ProductId))
        {
            var groups = new List<ResolvedModifierGroupResponse>();

            foreach (var link in productLinks.OrderBy(l => l.DisplayOrder))
            {
                if (!groupsById.TryGetValue(link.ModifierGroupId, out var group))
                {
                    continue;
                }

                modifiersByGroupId.TryGetValue(group.Id, out var modifiers);

                groups.Add(new ResolvedModifierGroupResponse(
                    group.Id,
                    group.Name,
                    group.SelectionMin,
                    group.SelectionMax,
                    group.IsRequired,
                    link.DisplayOrder,
                    (modifiers ?? [])
                        .Select(m => new ResolvedModifierResponse(m.Id, m.Name, m.PriceDelta))
                        .ToList()));
            }

            result[productLinks.Key] = groups;
        }

        return result;
    }

    private static DaysOfWeekMask ToDaysOfWeekMask(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => DaysOfWeekMask.Monday,
        DayOfWeek.Tuesday => DaysOfWeekMask.Tuesday,
        DayOfWeek.Wednesday => DaysOfWeekMask.Wednesday,
        DayOfWeek.Thursday => DaysOfWeekMask.Thursday,
        DayOfWeek.Friday => DaysOfWeekMask.Friday,
        DayOfWeek.Saturday => DaysOfWeekMask.Saturday,
        DayOfWeek.Sunday => DaysOfWeekMask.Sunday,
        _ => DaysOfWeekMask.None,
    };
}
