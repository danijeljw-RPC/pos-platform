using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Application.Tax;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Tax;

public sealed record CreateTaxCategoryDefinitionRequest(Guid TaxCategoryId, Guid TaxDefinitionId, Guid? LocationId, int Priority, Guid? TenantId = null);

public sealed record TaxCategoryDefinitionResponse(
    Guid Id,
    Guid TenantId,
    Guid TaxCategoryId,
    Guid TaxDefinitionId,
    Guid? LocationId,
    int Priority,
    bool IsActive,
    DateTimeOffset CreatedAtUtc)
{
    public static TaxCategoryDefinitionResponse FromEntity(TaxCategoryDefinition definition) =>
        new(definition.Id, definition.TenantId, definition.TaxCategoryId, definition.TaxDefinitionId,
            definition.LocationId, definition.Priority, definition.IsActive, definition.CreatedAtUtc);
}

/// <summary>
/// Create/list/delete endpoints for <see cref="TaxCategoryDefinition"/> (PLAN-0004 Milestone C) —
/// the mapping row that resolves a <see cref="TaxCategory"/> to the <see cref="TaxDefinition"/>(s)
/// it applies (organisation-wide when <see cref="TaxCategoryDefinition.LocationId"/> is null, or
/// scoped to one location). Unlike <see cref="TaxDefinitionEndpoints"/>/<see cref="TaxCategoryEndpoints"/>,
/// this row supports hard delete — it is a pure mapping, not itself a financial record (ADR-0010);
/// removing it does not retroactively affect any already-calculated tax snapshot. Has no
/// <c>OrganisationId</c> column of its own, so every organisation check here walks through its
/// referenced <see cref="TaxCategory"/> (and, for the create path, <see cref="TaxDefinition"/>/
/// <see cref="Location"/> too), matching the <see cref="Identity.TerminalEndpoints"/> precedent for
/// entities with no direct organisation column. Enforces ADR-0006's per-line design limit — at most
/// <see cref="TaxCalculationEngine.MaxComponentsPerLine"/> active mappings per
/// (<see cref="TaxCategoryDefinition.TaxCategoryId"/>, <see cref="TaxCategoryDefinition.LocationId"/>)
/// pair — at creation time, since that pair is exactly what a future resolution step will look up.
/// </summary>
public static class TaxCategoryDefinitionEndpoints
{
    public static void MapTaxCategoryDefinitionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tax-category-definitions").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapDelete("/{taxCategoryDefinitionId:guid}", DeleteAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateTaxCategoryDefinitionRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        if (request.Priority < 0)
        {
            return Results.BadRequest("Priority must not be negative.");
        }

        var authContext = authContextAccessor.Current!;

        // Every referenced row must exist (tenant filter) and belong to the caller's organisation —
        // ADR-0015 Context Provenance, matching TerminalEndpoints' walk-through-the-parent pattern.
        // A mismatch on any of the three is 404, never a validation error.
        var taxCategory = await dbContext.TaxCategories.SingleOrDefaultAsync(t => t.Id == request.TaxCategoryId);
        if (taxCategory is null || taxCategory.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var taxDefinition = await dbContext.TaxDefinitions.SingleOrDefaultAsync(t => t.Id == request.TaxDefinitionId);
        if (taxDefinition is null || taxDefinition.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        if (request.LocationId is not null)
        {
            var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == request.LocationId);
            if (location is null || location.OrganisationId != authContext.OrganisationId)
            {
                return Results.NotFound();
            }
        }

        // ADR-0006's per-line design limit: at most 10 active tax components resolved for one
        // (TaxCategory, Location) pair — Location null (organisation-wide) is its own bucket,
        // separate from any one location's bucket.
        var activeCount = await dbContext.TaxCategoryDefinitions
            .Where(d => d.TaxCategoryId == request.TaxCategoryId && d.LocationId == request.LocationId && d.IsActive)
            .CountAsync();

        if (activeCount >= TaxCalculationEngine.MaxComponentsPerLine)
        {
            return Results.BadRequest($"A tax category may resolve to at most {TaxCalculationEngine.MaxComponentsPerLine} active tax components per location.");
        }

        var now = DateTimeOffset.UtcNow;
        var definition = new TaxCategoryDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            TaxCategoryId = request.TaxCategoryId,
            TaxDefinitionId = request.TaxDefinitionId,
            LocationId = request.LocationId,
            Priority = request.Priority,
            IsActive = true,
            CreatedAtUtc = now,
        };

        dbContext.TaxCategoryDefinitions.Add(definition);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new TaxCategoryDefinitionChangedDomainEvent(
            definition.TenantId, authContext.OrganisationId!.Value, definition.Id, definition.LocationId, authContext.UserId,
            "Created", null, JsonSerializer.Serialize(ToSnapshot(definition)), now));

        return Results.Created($"/api/v1/tax-category-definitions/{definition.Id}", TaxCategoryDefinitionResponse.FromEntity(definition));
    }

    private static async Task<IResult> ListAsync(
        Guid? taxCategoryId,
        Guid? locationId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var query =
            from d in dbContext.TaxCategoryDefinitions
            join c in dbContext.TaxCategories on d.TaxCategoryId equals c.Id
            where c.OrganisationId == authContext.OrganisationId && d.IsActive
            select d;

        if (taxCategoryId is not null)
        {
            query = query.Where(d => d.TaxCategoryId == taxCategoryId);
        }

        if (locationId is not null)
        {
            query = query.Where(d => d.LocationId == locationId);
        }

        var definitions = await query.OrderBy(d => d.Priority).ToListAsync();

        return Results.Ok(definitions.Select(TaxCategoryDefinitionResponse.FromEntity));
    }

    private static async Task<IResult> DeleteAsync(
        Guid taxCategoryDefinitionId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;

        var definition = await FindAuthorizedAsync(taxCategoryDefinitionId, authContext.OrganisationId, dbContext);

        if (definition is null)
        {
            return Results.NotFound();
        }

        var before = ToSnapshot(definition);

        dbContext.TaxCategoryDefinitions.Remove(definition);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new TaxCategoryDefinitionChangedDomainEvent(
            definition.TenantId, authContext.OrganisationId!.Value, definition.Id, definition.LocationId, authContext.UserId,
            "Deleted", JsonSerializer.Serialize(before), null, DateTimeOffset.UtcNow));

        return Results.NoContent();
    }

    /// <summary>
    /// Fetches a mapping row (tenant filter applies) and confirms it belongs to
    /// <paramref name="callerOrganisationId"/> via its <see cref="TaxCategory"/>, since the mapping
    /// has no organisation column of its own. Returns <c>null</c> for either a missing row or an
    /// organisation mismatch — callers must not distinguish the two (404 either way).
    /// </summary>
    private static async Task<TaxCategoryDefinition?> FindAuthorizedAsync(Guid taxCategoryDefinitionId, Guid? callerOrganisationId, DaxaDbContext dbContext)
    {
        var definition = await dbContext.TaxCategoryDefinitions.SingleOrDefaultAsync(d => d.Id == taxCategoryDefinitionId);

        if (definition is null)
        {
            return null;
        }

        var taxCategory = await dbContext.TaxCategories.SingleOrDefaultAsync(c => c.Id == definition.TaxCategoryId);

        return taxCategory is null || taxCategory.OrganisationId != callerOrganisationId ? null : definition;
    }

    private static object ToSnapshot(TaxCategoryDefinition definition) => new
    {
        definition.TaxCategoryId,
        definition.TaxDefinitionId,
        definition.LocationId,
        definition.Priority,
    };
}
