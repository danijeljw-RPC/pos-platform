using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Catalog;

public sealed record CreateProductCategoryRequest(string Name, int DisplayOrder, Guid OrganisationId, Guid? TenantId = null);

public sealed record UpdateProductCategoryRequest(string Name, int DisplayOrder, Guid? TenantId = null);

public sealed record ProductCategoryResponse(
    Guid Id,
    Guid TenantId,
    Guid OrganisationId,
    string Name,
    int DisplayOrder,
    bool IsActive,
    DateTimeOffset CreatedAtUtc)
{
    public static ProductCategoryResponse FromEntity(ProductCategory category) =>
        new(category.Id, category.TenantId, category.OrganisationId, category.Name, category.DisplayOrder,
            category.IsActive, category.CreatedAtUtc);
}

/// <summary>
/// Create/read/update/deactivate/reactivate endpoints for <see cref="ProductCategory"/> (PLAN-0004
/// Milestone D). Same <c>AuthContext.OrganisationId</c> cross-check pattern as
/// <see cref="Tax.TaxCategoryEndpoints"/>/<see cref="Identity.LocationEndpoints"/> — a mismatch is
/// 404, never 403. No hard delete. Unlike <see cref="Tax.TaxCategoryEndpoints"/>/
/// <see cref="Tax.TaxDefinitionEndpoints"/>, <see cref="ProductCategory"/> has no <c>Code</c>
/// field — just <c>Name</c> — so, matching the <see cref="Identity.LocationEndpoints"/>/
/// <see cref="Identity.OrganisationEndpoints"/> precedent for name-only entities, duplicate names
/// are not rejected here.
/// </summary>
public static class ProductCategoryEndpoints
{
    public static void MapProductCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/product-categories").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapGet("/{productCategoryId:guid}", GetByIdAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPatch("/{productCategoryId:guid}", UpdateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPost("/{productCategoryId:guid}/deactivate", DeactivateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPost("/{productCategoryId:guid}/reactivate", ReactivateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateProductCategoryRequest request,
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

        if (authContext.OrganisationId != request.OrganisationId)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var category = new ProductCategory
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            OrganisationId = request.OrganisationId,
            Name = request.Name,
            DisplayOrder = request.DisplayOrder,
            IsActive = true,
            CreatedAtUtc = now,
        };

        dbContext.ProductCategories.Add(category);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ProductCategoryLifecycleDomainEvent(
            category.TenantId, category.OrganisationId, category.Id, authContext.UserId,
            "Created", null, JsonSerializer.Serialize(new { category.Name, category.DisplayOrder }), now));

        return Results.Created($"/api/v1/product-categories/{category.Id}", ProductCategoryResponse.FromEntity(category));
    }

    private static async Task<IResult> ListAsync(IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var categories = await dbContext.ProductCategories
            .Where(c => c.OrganisationId == authContext.OrganisationId && c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        return Results.Ok(categories.Select(ProductCategoryResponse.FromEntity));
    }

    private static async Task<IResult> GetByIdAsync(Guid productCategoryId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var category = await dbContext.ProductCategories.SingleOrDefaultAsync(c => c.Id == productCategoryId);

        if (category is null || category.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        return Results.Ok(ProductCategoryResponse.FromEntity(category));
    }

    private static async Task<IResult> UpdateAsync(
        Guid productCategoryId,
        UpdateProductCategoryRequest request,
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
        var category = await dbContext.ProductCategories.SingleOrDefaultAsync(c => c.Id == productCategoryId);

        if (category is null || category.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var before = new { category.Name, category.DisplayOrder };

        category.Name = request.Name;
        category.DisplayOrder = request.DisplayOrder;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ProductCategoryLifecycleDomainEvent(
            category.TenantId, category.OrganisationId, category.Id, authContext.UserId,
            "Updated", JsonSerializer.Serialize(before), JsonSerializer.Serialize(new { category.Name, category.DisplayOrder }), DateTimeOffset.UtcNow));

        return Results.Ok(ProductCategoryResponse.FromEntity(category));
    }

    private static Task<IResult> DeactivateAsync(
        Guid productCategoryId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(productCategoryId, isActive: false, action: "Deactivated", authContextAccessor, dbContext, dispatcher);

    private static Task<IResult> ReactivateAsync(
        Guid productCategoryId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(productCategoryId, isActive: true, action: "Reactivated", authContextAccessor, dbContext, dispatcher);

    private static async Task<IResult> SetActiveAsync(
        Guid productCategoryId,
        bool isActive,
        string action,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;

        var category = await dbContext.ProductCategories.SingleOrDefaultAsync(c => c.Id == productCategoryId);

        if (category is null || category.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var beforeIsActive = category.IsActive;
        category.IsActive = isActive;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ProductCategoryLifecycleDomainEvent(
            category.TenantId,
            category.OrganisationId,
            category.Id,
            authContext.UserId,
            action,
            JsonSerializer.Serialize(new { IsActive = beforeIsActive }),
            JsonSerializer.Serialize(new { category.IsActive }),
            DateTimeOffset.UtcNow));

        return Results.Ok(ProductCategoryResponse.FromEntity(category));
    }
}
