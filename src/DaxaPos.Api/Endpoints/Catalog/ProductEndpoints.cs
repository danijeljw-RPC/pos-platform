using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Catalog;

public sealed record CreateProductRequest(
    string Name,
    Guid OrganisationId,
    Guid ProductCategoryId,
    Guid TaxCategoryId,
    string? Description,
    string? Sku,
    string? Barcode,
    decimal BasePrice,
    Guid? TenantId = null);

public sealed record UpdateProductRequest(
    string Name,
    Guid ProductCategoryId,
    Guid TaxCategoryId,
    string? Description,
    string? Sku,
    string? Barcode,
    decimal BasePrice,
    Guid? TenantId = null);

public sealed record ProductResponse(
    Guid Id,
    Guid TenantId,
    Guid OrganisationId,
    Guid ProductCategoryId,
    string Name,
    string? Description,
    string? Sku,
    string? Barcode,
    Guid TaxCategoryId,
    decimal BasePrice,
    bool IsActive,
    bool IsArchived,
    DateTimeOffset? ArchivedAtUtc,
    Guid? SupersededByProductId,
    DateTimeOffset CreatedAtUtc,
    Guid? PreviousProductId = null)
{
    public static ProductResponse FromEntity(Product product, Guid? previousProductId = null) =>
        new(product.Id, product.TenantId, product.OrganisationId, product.ProductCategoryId, product.Name,
            product.Description, product.Sku, product.Barcode, product.TaxCategoryId, product.BasePrice,
            product.IsActive, product.IsArchived, product.ArchivedAtUtc, product.SupersededByProductId,
            product.CreatedAtUtc, previousProductId);
}

/// <summary>
/// Create/read/update/deactivate/reactivate endpoints for <see cref="Product"/> (PLAN-0004
/// Milestone D, OI-0007). Same <c>AuthContext.OrganisationId</c> cross-check pattern as every other
/// Milestone C/D endpoint — a mismatch on the product itself, its <see cref="ProductCategory"/>, or
/// its <see cref="TaxCategory"/> is 404, never 403/400. <see cref="UpdateAsync"/> is the one
/// endpoint in this milestone with branching behaviour: a <c>PATCH</c> that leaves
/// <c>TaxCategoryId</c> unchanged updates in place (200 OK); a <c>PATCH</c> that changes it triggers
/// OI-0007's archive-and-replace (201 Created, pointing at the new row) — see
/// <see cref="ArchiveAndReplaceAsync"/>. No hard delete, and no further writes are permitted once
/// <see cref="Product.IsArchived"/> is set (it is a permanent, historical state).
/// </summary>
public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/products").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapGet("/{productId:guid}", GetByIdAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPatch("/{productId:guid}", UpdateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPost("/{productId:guid}/deactivate", DeactivateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPost("/{productId:guid}/reactivate", ReactivateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateProductRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        var validationError = ValidateFields(request.Name, request.BasePrice);
        if (validationError is not null)
        {
            return validationError;
        }

        var authContext = authContextAccessor.Current!;

        if (authContext.OrganisationId != request.OrganisationId)
        {
            return Results.NotFound();
        }

        // Context provenance (ADR-0015): both references must exist (tenant filter) and belong to
        // the caller's organisation — a missing row and a cross-organisation row are
        // indistinguishable to the caller (404 either way), matching TaxCategoryDefinitionEndpoints.
        var productCategory = await dbContext.ProductCategories.SingleOrDefaultAsync(c => c.Id == request.ProductCategoryId);
        if (productCategory is null || productCategory.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var taxCategory = await dbContext.TaxCategories.SingleOrDefaultAsync(t => t.Id == request.TaxCategoryId);
        if (taxCategory is null || taxCategory.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            OrganisationId = request.OrganisationId,
            ProductCategoryId = request.ProductCategoryId,
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            Barcode = request.Barcode,
            TaxCategoryId = request.TaxCategoryId,
            BasePrice = request.BasePrice,
            IsActive = true,
            IsArchived = false,
            CreatedAtUtc = now,
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ProductLifecycleDomainEvent(
            product.TenantId, product.OrganisationId, product.Id, authContext.UserId,
            "Created", null, JsonSerializer.Serialize(ToSnapshot(product)), now));

        return Results.Created($"/api/v1/products/{product.Id}", ProductResponse.FromEntity(product));
    }

    private static async Task<IResult> ListAsync(
        Guid? productCategoryId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        // List hides inactive and archived products by default; single GET does not — the two
        // flags are independent (Domain Assumptions) and are checked separately here on purpose.
        var query = dbContext.Products
            .Where(p => p.OrganisationId == authContext.OrganisationId && p.IsActive && !p.IsArchived);

        if (productCategoryId is not null)
        {
            query = query.Where(p => p.ProductCategoryId == productCategoryId);
        }

        var products = await query.OrderBy(p => p.Name).ToListAsync();

        return Results.Ok(products.Select(p => ProductResponse.FromEntity(p)));
    }

    private static async Task<IResult> GetByIdAsync(Guid productId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var product = await dbContext.Products.SingleOrDefaultAsync(p => p.Id == productId);

        if (product is null || product.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        return Results.Ok(ProductResponse.FromEntity(product));
    }

    private static async Task<IResult> UpdateAsync(
        Guid productId,
        UpdateProductRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        var validationError = ValidateFields(request.Name, request.BasePrice);
        if (validationError is not null)
        {
            return validationError;
        }

        var authContext = authContextAccessor.Current!;
        var product = await dbContext.Products.SingleOrDefaultAsync(p => p.Id == productId);

        if (product is null || product.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        if (product.IsArchived)
        {
            return Results.Conflict("Cannot update an archived product; it is a permanent historical record.");
        }

        var newProductCategory = await dbContext.ProductCategories.SingleOrDefaultAsync(c => c.Id == request.ProductCategoryId);
        if (newProductCategory is null || newProductCategory.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var newTaxCategory = await dbContext.TaxCategories.SingleOrDefaultAsync(t => t.Id == request.TaxCategoryId);
        if (newTaxCategory is null || newTaxCategory.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        return request.TaxCategoryId == product.TaxCategoryId
            ? await UpdateInPlaceAsync(product, request, authContext, dbContext, dispatcher)
            : await ArchiveAndReplaceAsync(product, request, authContext, dbContext, dispatcher);
    }

    /// <summary>A non-tax-affecting change (Name/Description/Sku/Barcode/BasePrice/ProductCategoryId) — ordinary in-place PATCH.</summary>
    private static async Task<IResult> UpdateInPlaceAsync(
        Product product,
        UpdateProductRequest request,
        AuthContext authContext,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var before = ToSnapshot(product);

        product.Name = request.Name;
        product.Description = request.Description;
        product.Sku = request.Sku;
        product.Barcode = request.Barcode;
        product.ProductCategoryId = request.ProductCategoryId;
        product.BasePrice = request.BasePrice;

        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ProductLifecycleDomainEvent(
            product.TenantId, product.OrganisationId, product.Id, authContext.UserId,
            "Updated", JsonSerializer.Serialize(before), JsonSerializer.Serialize(ToSnapshot(product)), DateTimeOffset.UtcNow));

        return Results.Ok(ProductResponse.FromEntity(product));
    }

    /// <summary>
    /// OI-0007's archive-and-replace: a <c>TaxCategoryId</c> change archives <paramref name="product"/>
    /// (never mutated further) and creates a brand-new row carrying every requested field value.
    /// The documented concurrency race (two simultaneous tax-category-changing edits to the same
    /// product both reading the same "current" row before either archives it) is an accepted MVP
    /// risk per the plan's Risks section, matching OI-0013's precedent — no row-locking is added.
    /// </summary>
    private static async Task<IResult> ArchiveAndReplaceAsync(
        Product product,
        UpdateProductRequest request,
        AuthContext authContext,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var before = ToSnapshot(product);
        var now = DateTimeOffset.UtcNow;

        var replacement = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = product.TenantId,
            OrganisationId = product.OrganisationId,
            ProductCategoryId = request.ProductCategoryId,
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            Barcode = request.Barcode,
            TaxCategoryId = request.TaxCategoryId,
            BasePrice = request.BasePrice,
            IsActive = product.IsActive,
            IsArchived = false,
            CreatedAtUtc = now,
        };

        product.IsArchived = true;
        product.ArchivedAtUtc = now;
        product.SupersededByProductId = replacement.Id;

        dbContext.Products.Add(replacement);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ProductLifecycleDomainEvent(
            product.TenantId, product.OrganisationId, product.Id, authContext.UserId,
            "Archived", JsonSerializer.Serialize(before),
            JsonSerializer.Serialize(new { product.IsArchived, product.ArchivedAtUtc, product.SupersededByProductId }), now));

        await dispatcher.DispatchAsync(new ProductLifecycleDomainEvent(
            replacement.TenantId, replacement.OrganisationId, replacement.Id, authContext.UserId,
            "CreatedFromReplace", null,
            JsonSerializer.Serialize(ToSnapshot(replacement) with { PreviousProductId = product.Id }), now));

        return Results.Created($"/api/v1/products/{replacement.Id}", ProductResponse.FromEntity(replacement, previousProductId: product.Id));
    }

    private static Task<IResult> DeactivateAsync(
        Guid productId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(productId, isActive: false, action: "Deactivated", authContextAccessor, dbContext, dispatcher);

    private static Task<IResult> ReactivateAsync(
        Guid productId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(productId, isActive: true, action: "Reactivated", authContextAccessor, dbContext, dispatcher);

    private static async Task<IResult> SetActiveAsync(
        Guid productId,
        bool isActive,
        string action,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;

        var product = await dbContext.Products.SingleOrDefaultAsync(p => p.Id == productId);

        if (product is null || product.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        if (product.IsArchived)
        {
            return Results.Conflict("Cannot change IsActive on an archived product; it is a permanent historical record.");
        }

        var beforeIsActive = product.IsActive;
        product.IsActive = isActive;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ProductLifecycleDomainEvent(
            product.TenantId,
            product.OrganisationId,
            product.Id,
            authContext.UserId,
            action,
            JsonSerializer.Serialize(new { IsActive = beforeIsActive }),
            JsonSerializer.Serialize(new { product.IsActive }),
            DateTimeOffset.UtcNow));

        return Results.Ok(ProductResponse.FromEntity(product));
    }

    private static IResult? ValidateFields(string name, decimal basePrice)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Results.BadRequest("Name is required.");
        }

        if (basePrice < 0)
        {
            return Results.BadRequest("BasePrice must not be negative.");
        }

        return null;
    }

    private static ProductSnapshot ToSnapshot(Product product) => new(
        product.Name, product.Description, product.Sku, product.Barcode,
        product.ProductCategoryId, product.TaxCategoryId, product.BasePrice, PreviousProductId: null);

    private sealed record ProductSnapshot(
        string Name,
        string? Description,
        string? Sku,
        string? Barcode,
        Guid ProductCategoryId,
        Guid TaxCategoryId,
        decimal BasePrice,
        Guid? PreviousProductId);
}
