using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Catalog;

public sealed record CreateProductLocationOverrideRequest(Guid ProductId, Guid LocationId, bool IsAvailable, bool IsSoldOut, decimal? PriceOverride, Guid? TenantId = null);

public sealed record UpdateProductLocationOverrideRequest(bool IsAvailable, bool IsSoldOut, decimal? PriceOverride, Guid? TenantId = null);

public sealed record ProductLocationOverrideResponse(
    Guid Id,
    Guid TenantId,
    Guid LocationId,
    Guid ProductId,
    bool IsAvailable,
    bool IsSoldOut,
    decimal? PriceOverride,
    DateTimeOffset CreatedAtUtc)
{
    public static ProductLocationOverrideResponse FromEntity(ProductLocationOverride o) =>
        new(o.Id, o.TenantId, o.LocationId, o.ProductId, o.IsAvailable, o.IsSoldOut, o.PriceOverride, o.CreatedAtUtc);
}

/// <summary>
/// Create/read/update/delete endpoints for <see cref="ProductLocationOverride"/> (PLAN-0004
/// Milestone F) — the full record, including <see cref="ProductLocationOverride.PriceOverride"/>.
/// Gated <c>pricing.manage</c> + <c>rejectStaffPin: true</c>, per the plan's exact permission
/// table (not <c>catalog.manage</c> — price configuration is <c>pricing.manage</c>'s surface).
/// Contrast with <see cref="ProductSoldOutEndpoints"/>, the separate, narrower, staff-accessible
/// endpoint that may only ever touch <see cref="ProductLocationOverride.IsSoldOut"/>. Hard delete
/// (a pure config override, not a financial record, ADR-0010) — removing the row reverts to the
/// organisation-wide <see cref="Product"/> defaults per ADR-0003's default-and-override model.
/// </summary>
public static class ProductLocationOverrideEndpoints
{
    public static void MapProductLocationOverrideEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/product-location-overrides").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.PricingManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.PricingManage, rejectStaffPin: true);
        group.MapGet("/{productLocationOverrideId:guid}", GetByIdAsync).RequirePermission(Permissions.PricingManage, rejectStaffPin: true);
        group.MapPatch("/{productLocationOverrideId:guid}", UpdateAsync).RequirePermission(Permissions.PricingManage, rejectStaffPin: true);
        group.MapDelete("/{productLocationOverrideId:guid}", DeleteAsync).RequirePermission(Permissions.PricingManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateProductLocationOverrideRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        if (request.PriceOverride is < 0)
        {
            return Results.BadRequest("PriceOverride must not be negative.");
        }

        var authContext = authContextAccessor.Current!;

        var product = await dbContext.Products.SingleOrDefaultAsync(p => p.Id == request.ProductId);
        if (product is null || product.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == request.LocationId);
        if (location is null || location.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        if (await dbContext.ProductLocationOverrides.AnyAsync(o => o.LocationId == request.LocationId && o.ProductId == request.ProductId))
        {
            return Results.Conflict("A location override for this product already exists at this location.");
        }

        var now = DateTimeOffset.UtcNow;
        var over = new ProductLocationOverride
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            LocationId = request.LocationId,
            ProductId = request.ProductId,
            IsAvailable = request.IsAvailable,
            IsSoldOut = request.IsSoldOut,
            PriceOverride = request.PriceOverride,
            CreatedAtUtc = now,
        };

        dbContext.ProductLocationOverrides.Add(over);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ProductLocationOverrideChangedDomainEvent(
            over.TenantId, location.OrganisationId, over.Id, over.ProductId, over.LocationId, authContext.UserId, StaffMemberId: null,
            "Created", null, JsonSerializer.Serialize(ToSnapshot(over)), now));

        return Results.Created($"/api/v1/product-location-overrides/{over.Id}", ProductLocationOverrideResponse.FromEntity(over));
    }

    private static async Task<IResult> ListAsync(
        Guid? productId,
        Guid? locationId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var query =
            from o in dbContext.ProductLocationOverrides
            join l in dbContext.Locations on o.LocationId equals l.Id
            where l.OrganisationId == authContext.OrganisationId
            select o;

        if (productId is not null)
        {
            query = query.Where(o => o.ProductId == productId);
        }

        if (locationId is not null)
        {
            query = query.Where(o => o.LocationId == locationId);
        }

        var overrides = await query.ToListAsync();

        return Results.Ok(overrides.Select(ProductLocationOverrideResponse.FromEntity));
    }

    private static async Task<IResult> GetByIdAsync(Guid productLocationOverrideId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;
        var over = await FindAuthorizedAsync(productLocationOverrideId, authContext.OrganisationId, dbContext);

        return over is null ? Results.NotFound() : Results.Ok(ProductLocationOverrideResponse.FromEntity(over));
    }

    private static async Task<IResult> UpdateAsync(
        Guid productLocationOverrideId,
        UpdateProductLocationOverrideRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        if (request.PriceOverride is < 0)
        {
            return Results.BadRequest("PriceOverride must not be negative.");
        }

        var authContext = authContextAccessor.Current!;
        var over = await FindAuthorizedAsync(productLocationOverrideId, authContext.OrganisationId, dbContext);

        if (over is null)
        {
            return Results.NotFound();
        }

        var before = ToSnapshot(over);

        over.IsAvailable = request.IsAvailable;
        over.IsSoldOut = request.IsSoldOut;
        over.PriceOverride = request.PriceOverride;
        await dbContext.SaveChangesAsync();

        var organisationId = await ResolveOrganisationIdAsync(over.LocationId, dbContext);

        await dispatcher.DispatchAsync(new ProductLocationOverrideChangedDomainEvent(
            over.TenantId, organisationId, over.Id, over.ProductId, over.LocationId, authContext.UserId, StaffMemberId: null,
            "Updated", JsonSerializer.Serialize(before), JsonSerializer.Serialize(ToSnapshot(over)), DateTimeOffset.UtcNow));

        return Results.Ok(ProductLocationOverrideResponse.FromEntity(over));
    }

    private static async Task<IResult> DeleteAsync(
        Guid productLocationOverrideId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;
        var over = await FindAuthorizedAsync(productLocationOverrideId, authContext.OrganisationId, dbContext);

        if (over is null)
        {
            return Results.NotFound();
        }

        var before = ToSnapshot(over);
        var organisationId = await ResolveOrganisationIdAsync(over.LocationId, dbContext);

        dbContext.ProductLocationOverrides.Remove(over);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ProductLocationOverrideChangedDomainEvent(
            over.TenantId, organisationId, over.Id, over.ProductId, over.LocationId, authContext.UserId, StaffMemberId: null,
            "Deleted", JsonSerializer.Serialize(before), null, DateTimeOffset.UtcNow));

        return Results.NoContent();
    }

    /// <summary>
    /// Fetches an override (tenant filter applies) and confirms it belongs to
    /// <paramref name="callerOrganisationId"/> via its <see cref="Location"/>, since the override
    /// has no organisation column of its own (404 either way for missing/cross-organisation).
    /// </summary>
    private static async Task<ProductLocationOverride?> FindAuthorizedAsync(Guid productLocationOverrideId, Guid? callerOrganisationId, DaxaDbContext dbContext)
    {
        var over = await dbContext.ProductLocationOverrides.SingleOrDefaultAsync(o => o.Id == productLocationOverrideId);

        if (over is null)
        {
            return null;
        }

        var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == over.LocationId);

        return location is null || location.OrganisationId != callerOrganisationId ? null : over;
    }

    private static async Task<Guid> ResolveOrganisationIdAsync(Guid locationId, DaxaDbContext dbContext) =>
        (await dbContext.Locations.SingleAsync(l => l.Id == locationId)).OrganisationId;

    private static object ToSnapshot(ProductLocationOverride over) => new { over.IsAvailable, over.IsSoldOut, over.PriceOverride };
}
