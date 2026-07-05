using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Tax;

public sealed record CreateTaxCategoryRequest(string Code, string Name, Guid OrganisationId, TaxTreatment TaxTreatment, Guid? TenantId = null);

public sealed record UpdateTaxCategoryRequest(string Name, TaxTreatment TaxTreatment, Guid? TenantId = null);

public sealed record TaxCategoryResponse(
    Guid Id,
    Guid TenantId,
    Guid OrganisationId,
    string Code,
    string Name,
    TaxTreatment TaxTreatment,
    bool IsActive,
    DateTimeOffset CreatedAtUtc)
{
    public static TaxCategoryResponse FromEntity(TaxCategory category) =>
        new(category.Id, category.TenantId, category.OrganisationId, category.Code, category.Name,
            category.TaxTreatment, category.IsActive, category.CreatedAtUtc);
}

/// <summary>
/// Create/read/update/deactivate/reactivate endpoints for <see cref="TaxCategory"/> (PLAN-0004
/// Milestone C, OI-0007) — the product-facing semantic label (<c>Taxable</c>, <c>GSTFree</c>, ...)
/// that resolves to one or more <see cref="TaxDefinition"/> rows via
/// <see cref="TaxCategoryDefinition"/>. Same <c>AuthContext.OrganisationId</c> cross-check pattern
/// as <see cref="TaxDefinitionEndpoints"/>/<see cref="Identity.LocationEndpoints"/> — a mismatch is
/// 404, never 403. No hard delete: category assignment is financially meaningful (ADR-0010).
/// </summary>
public static class TaxCategoryEndpoints
{
    public static void MapTaxCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tax-categories").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapGet("/{taxCategoryId:guid}", GetByIdAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPatch("/{taxCategoryId:guid}", UpdateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPost("/{taxCategoryId:guid}/deactivate", DeactivateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPost("/{taxCategoryId:guid}/reactivate", ReactivateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateTaxCategoryRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest("Code is required.");
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

        if (await dbContext.TaxCategories.AnyAsync(t => t.TenantId == authContext.TenantId && t.Code == request.Code))
        {
            return Results.Conflict("Code is already in use within this tenant.");
        }

        var now = DateTimeOffset.UtcNow;
        var category = new TaxCategory
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            OrganisationId = request.OrganisationId,
            Code = request.Code,
            Name = request.Name,
            TaxTreatment = request.TaxTreatment,
            IsActive = true,
            CreatedAtUtc = now,
        };

        dbContext.TaxCategories.Add(category);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new TaxCategoryLifecycleDomainEvent(
            category.TenantId, category.OrganisationId, category.Id, authContext.UserId,
            "Created", null, JsonSerializer.Serialize(new { category.Code, category.Name, category.TaxTreatment }), now));

        return Results.Created($"/api/v1/tax-categories/{category.Id}", TaxCategoryResponse.FromEntity(category));
    }

    private static async Task<IResult> ListAsync(IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var categories = await dbContext.TaxCategories
            .Where(t => t.OrganisationId == authContext.OrganisationId && t.IsActive)
            .OrderBy(t => t.Code)
            .ToListAsync();

        return Results.Ok(categories.Select(TaxCategoryResponse.FromEntity));
    }

    private static async Task<IResult> GetByIdAsync(Guid taxCategoryId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var category = await dbContext.TaxCategories.SingleOrDefaultAsync(t => t.Id == taxCategoryId);

        if (category is null || category.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        return Results.Ok(TaxCategoryResponse.FromEntity(category));
    }

    private static async Task<IResult> UpdateAsync(
        Guid taxCategoryId,
        UpdateTaxCategoryRequest request,
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
        var category = await dbContext.TaxCategories.SingleOrDefaultAsync(t => t.Id == taxCategoryId);

        if (category is null || category.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var before = new { category.Name, category.TaxTreatment };

        category.Name = request.Name;
        category.TaxTreatment = request.TaxTreatment;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new TaxCategoryLifecycleDomainEvent(
            category.TenantId, category.OrganisationId, category.Id, authContext.UserId,
            "Updated", JsonSerializer.Serialize(before), JsonSerializer.Serialize(new { category.Name, category.TaxTreatment }), DateTimeOffset.UtcNow));

        return Results.Ok(TaxCategoryResponse.FromEntity(category));
    }

    private static Task<IResult> DeactivateAsync(
        Guid taxCategoryId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(taxCategoryId, isActive: false, action: "Deactivated", authContextAccessor, dbContext, dispatcher);

    private static Task<IResult> ReactivateAsync(
        Guid taxCategoryId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(taxCategoryId, isActive: true, action: "Reactivated", authContextAccessor, dbContext, dispatcher);

    private static async Task<IResult> SetActiveAsync(
        Guid taxCategoryId,
        bool isActive,
        string action,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;

        var category = await dbContext.TaxCategories.SingleOrDefaultAsync(t => t.Id == taxCategoryId);

        if (category is null || category.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var beforeIsActive = category.IsActive;
        category.IsActive = isActive;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new TaxCategoryLifecycleDomainEvent(
            category.TenantId,
            category.OrganisationId,
            category.Id,
            authContext.UserId,
            action,
            JsonSerializer.Serialize(new { IsActive = beforeIsActive }),
            JsonSerializer.Serialize(new { category.IsActive }),
            DateTimeOffset.UtcNow));

        return Results.Ok(TaxCategoryResponse.FromEntity(category));
    }
}
