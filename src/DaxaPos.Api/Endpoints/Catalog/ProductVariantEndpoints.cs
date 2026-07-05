using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Catalog;

public sealed record CreateProductVariantRequest(string Name, Guid ProductId, decimal PriceDelta, string? Sku, Guid? TenantId = null);

public sealed record UpdateProductVariantRequest(string Name, decimal PriceDelta, string? Sku, Guid? TenantId = null);

public sealed record ProductVariantResponse(
    Guid Id,
    Guid TenantId,
    Guid ProductId,
    string Name,
    decimal PriceDelta,
    string? Sku,
    bool IsActive,
    DateTimeOffset CreatedAtUtc)
{
    public static ProductVariantResponse FromEntity(ProductVariant variant) =>
        new(variant.Id, variant.TenantId, variant.ProductId, variant.Name, variant.PriceDelta,
            variant.Sku, variant.IsActive, variant.CreatedAtUtc);
}

/// <summary>
/// Create/read/update/deactivate/reactivate endpoints for <see cref="ProductVariant"/> (PLAN-0004
/// Milestone E). <see cref="ProductVariant"/> carries no <c>OrganisationId</c> column of its own, so
/// every organisation check here walks <c>ProductVariant -&gt; Product -&gt; OrganisationId</c>,
/// matching <see cref="Identity.TerminalEndpoints"/>'s <c>Terminal -&gt; Location</c> precedent
/// exactly. <see cref="PriceDelta"/> may be positive, zero, or negative (a delta on the resolved
/// base price, not an absolute amount) — deliberately not validated with <see cref="Product"/>'s
/// <c>&gt;= 0</c> rule. No hard delete.
/// </summary>
public static class ProductVariantEndpoints
{
    public static void MapProductVariantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/product-variants").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapGet("/{productVariantId:guid}", GetByIdAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPatch("/{productVariantId:guid}", UpdateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPost("/{productVariantId:guid}/deactivate", DeactivateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPost("/{productVariantId:guid}/reactivate", ReactivateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateProductVariantRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest("Name is required.");
        }

        var authContext = authContextAccessor.Current!;

        // Context provenance (ADR-0015): the referenced product must exist (tenant filter) and
        // belong to the caller's organisation — a missing product and a cross-organisation product
        // are indistinguishable to the caller (404 either way).
        var product = await dbContext.Products.SingleOrDefaultAsync(p => p.Id == request.ProductId);
        if (product is null || product.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            ProductId = request.ProductId,
            Name = request.Name,
            PriceDelta = request.PriceDelta,
            Sku = request.Sku,
            IsActive = true,
            CreatedAtUtc = now,
        };

        dbContext.ProductVariants.Add(variant);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ProductVariantLifecycleDomainEvent(
            variant.TenantId, product.OrganisationId, variant.Id, authContext.UserId,
            "Created", null, JsonSerializer.Serialize(new { variant.Name, variant.PriceDelta, variant.Sku }), now));

        return Results.Created($"/api/v1/product-variants/{variant.Id}", ProductVariantResponse.FromEntity(variant));
    }

    private static async Task<IResult> ListAsync(
        Guid? productId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var query =
            from v in dbContext.ProductVariants
            join p in dbContext.Products on v.ProductId equals p.Id
            where p.OrganisationId == authContext.OrganisationId && v.IsActive
            select v;

        if (productId is not null)
        {
            query = query.Where(v => v.ProductId == productId);
        }

        var variants = await query.OrderBy(v => v.Name).ToListAsync();

        return Results.Ok(variants.Select(ProductVariantResponse.FromEntity));
    }

    private static async Task<IResult> GetByIdAsync(Guid productVariantId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;
        var variant = await FindAuthorizedVariantAsync(productVariantId, authContext.OrganisationId, dbContext);

        return variant is null ? Results.NotFound() : Results.Ok(ProductVariantResponse.FromEntity(variant));
    }

    private static async Task<IResult> UpdateAsync(
        Guid productVariantId,
        UpdateProductVariantRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest("Name is required.");
        }

        var authContext = authContextAccessor.Current!;
        var variant = await FindAuthorizedVariantAsync(productVariantId, authContext.OrganisationId, dbContext);

        if (variant is null)
        {
            return Results.NotFound();
        }

        var before = new { variant.Name, variant.PriceDelta, variant.Sku };

        variant.Name = request.Name;
        variant.PriceDelta = request.PriceDelta;
        variant.Sku = request.Sku;
        await dbContext.SaveChangesAsync();

        var organisationId = await ResolveOrganisationIdAsync(variant.ProductId, dbContext);

        await dispatcher.DispatchAsync(new ProductVariantLifecycleDomainEvent(
            variant.TenantId, organisationId, variant.Id, authContext.UserId,
            "Updated", JsonSerializer.Serialize(before), JsonSerializer.Serialize(new { variant.Name, variant.PriceDelta, variant.Sku }), DateTimeOffset.UtcNow));

        return Results.Ok(ProductVariantResponse.FromEntity(variant));
    }

    private static Task<IResult> DeactivateAsync(
        Guid productVariantId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(productVariantId, isActive: false, action: "Deactivated", authContextAccessor, dbContext, dispatcher);

    private static Task<IResult> ReactivateAsync(
        Guid productVariantId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(productVariantId, isActive: true, action: "Reactivated", authContextAccessor, dbContext, dispatcher);

    private static async Task<IResult> SetActiveAsync(
        Guid productVariantId,
        bool isActive,
        string action,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;
        var variant = await FindAuthorizedVariantAsync(productVariantId, authContext.OrganisationId, dbContext);

        if (variant is null)
        {
            return Results.NotFound();
        }

        var beforeIsActive = variant.IsActive;
        variant.IsActive = isActive;
        await dbContext.SaveChangesAsync();

        var organisationId = await ResolveOrganisationIdAsync(variant.ProductId, dbContext);

        await dispatcher.DispatchAsync(new ProductVariantLifecycleDomainEvent(
            variant.TenantId,
            organisationId,
            variant.Id,
            authContext.UserId,
            action,
            JsonSerializer.Serialize(new { IsActive = beforeIsActive }),
            JsonSerializer.Serialize(new { variant.IsActive }),
            DateTimeOffset.UtcNow));

        return Results.Ok(ProductVariantResponse.FromEntity(variant));
    }

    /// <summary>
    /// Fetches a variant (tenant filter applies) and confirms it belongs to
    /// <paramref name="callerOrganisationId"/> via its <see cref="Product"/>. Returns <c>null</c>
    /// for either a missing variant or an organisation mismatch — callers must not distinguish the
    /// two in their response (404 either way).
    /// </summary>
    private static async Task<ProductVariant?> FindAuthorizedVariantAsync(Guid productVariantId, Guid? callerOrganisationId, DaxaDbContext dbContext)
    {
        var variant = await dbContext.ProductVariants.SingleOrDefaultAsync(v => v.Id == productVariantId);

        if (variant is null)
        {
            return null;
        }

        var product = await dbContext.Products.SingleOrDefaultAsync(p => p.Id == variant.ProductId);

        return product is null || product.OrganisationId != callerOrganisationId ? null : variant;
    }

    private static async Task<Guid> ResolveOrganisationIdAsync(Guid productId, DaxaDbContext dbContext) =>
        (await dbContext.Products.SingleAsync(p => p.Id == productId)).OrganisationId;
}
