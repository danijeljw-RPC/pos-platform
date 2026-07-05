using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Catalog;

public sealed record CreateModifierGroupRequest(string Name, Guid OrganisationId, int SelectionMin, int SelectionMax, bool IsRequired, Guid? TenantId = null);

public sealed record UpdateModifierGroupRequest(string Name, int SelectionMin, int SelectionMax, bool IsRequired, Guid? TenantId = null);

public sealed record ModifierGroupResponse(
    Guid Id,
    Guid TenantId,
    Guid OrganisationId,
    string Name,
    int SelectionMin,
    int SelectionMax,
    bool IsRequired,
    bool IsActive,
    DateTimeOffset CreatedAtUtc)
{
    public static ModifierGroupResponse FromEntity(ModifierGroup group) =>
        new(group.Id, group.TenantId, group.OrganisationId, group.Name, group.SelectionMin,
            group.SelectionMax, group.IsRequired, group.IsActive, group.CreatedAtUtc);
}

/// <summary>
/// Create/read/update/deactivate/reactivate endpoints for <see cref="ModifierGroup"/> (PLAN-0004
/// Milestone E) — a named group of <see cref="Modifier"/>s (e.g. "Milk type") attachable to
/// products via <see cref="ProductModifierGroup"/>. Organisation-owned directly (unlike
/// <see cref="Modifier"/>/<see cref="ProductVariant"/>), so the <c>AuthContext.OrganisationId</c>
/// cross-check is a direct comparison, same pattern as <see cref="Tax.TaxCategoryEndpoints"/>. No
/// hard delete.
/// </summary>
public static class ModifierGroupEndpoints
{
    public static void MapModifierGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/modifier-groups").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapGet("/{modifierGroupId:guid}", GetByIdAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPatch("/{modifierGroupId:guid}", UpdateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPost("/{modifierGroupId:guid}/deactivate", DeactivateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPost("/{modifierGroupId:guid}/reactivate", ReactivateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateModifierGroupRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        var validationError = ValidateFields(request.Name, request.SelectionMin, request.SelectionMax);
        if (validationError is not null)
        {
            return validationError;
        }

        var authContext = authContextAccessor.Current!;

        if (authContext.OrganisationId != request.OrganisationId)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var modifierGroup = new ModifierGroup
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            OrganisationId = request.OrganisationId,
            Name = request.Name,
            SelectionMin = request.SelectionMin,
            SelectionMax = request.SelectionMax,
            IsRequired = request.IsRequired,
            IsActive = true,
            CreatedAtUtc = now,
        };

        dbContext.ModifierGroups.Add(modifierGroup);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ModifierGroupLifecycleDomainEvent(
            modifierGroup.TenantId, modifierGroup.OrganisationId, modifierGroup.Id, authContext.UserId,
            "Created", null,
            JsonSerializer.Serialize(new { modifierGroup.Name, modifierGroup.SelectionMin, modifierGroup.SelectionMax, modifierGroup.IsRequired }), now));

        return Results.Created($"/api/v1/modifier-groups/{modifierGroup.Id}", ModifierGroupResponse.FromEntity(modifierGroup));
    }

    private static async Task<IResult> ListAsync(IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var modifierGroups = await dbContext.ModifierGroups
            .Where(g => g.OrganisationId == authContext.OrganisationId && g.IsActive)
            .OrderBy(g => g.Name)
            .ToListAsync();

        return Results.Ok(modifierGroups.Select(ModifierGroupResponse.FromEntity));
    }

    private static async Task<IResult> GetByIdAsync(Guid modifierGroupId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var modifierGroup = await dbContext.ModifierGroups.SingleOrDefaultAsync(g => g.Id == modifierGroupId);

        if (modifierGroup is null || modifierGroup.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        return Results.Ok(ModifierGroupResponse.FromEntity(modifierGroup));
    }

    private static async Task<IResult> UpdateAsync(
        Guid modifierGroupId,
        UpdateModifierGroupRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        var validationError = ValidateFields(request.Name, request.SelectionMin, request.SelectionMax);
        if (validationError is not null)
        {
            return validationError;
        }

        var authContext = authContextAccessor.Current!;
        var modifierGroup = await dbContext.ModifierGroups.SingleOrDefaultAsync(g => g.Id == modifierGroupId);

        if (modifierGroup is null || modifierGroup.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var before = new { modifierGroup.Name, modifierGroup.SelectionMin, modifierGroup.SelectionMax, modifierGroup.IsRequired };

        modifierGroup.Name = request.Name;
        modifierGroup.SelectionMin = request.SelectionMin;
        modifierGroup.SelectionMax = request.SelectionMax;
        modifierGroup.IsRequired = request.IsRequired;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ModifierGroupLifecycleDomainEvent(
            modifierGroup.TenantId, modifierGroup.OrganisationId, modifierGroup.Id, authContext.UserId,
            "Updated", JsonSerializer.Serialize(before),
            JsonSerializer.Serialize(new { modifierGroup.Name, modifierGroup.SelectionMin, modifierGroup.SelectionMax, modifierGroup.IsRequired }),
            DateTimeOffset.UtcNow));

        return Results.Ok(ModifierGroupResponse.FromEntity(modifierGroup));
    }

    private static Task<IResult> DeactivateAsync(
        Guid modifierGroupId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(modifierGroupId, isActive: false, action: "Deactivated", authContextAccessor, dbContext, dispatcher);

    private static Task<IResult> ReactivateAsync(
        Guid modifierGroupId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(modifierGroupId, isActive: true, action: "Reactivated", authContextAccessor, dbContext, dispatcher);

    private static async Task<IResult> SetActiveAsync(
        Guid modifierGroupId,
        bool isActive,
        string action,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;

        var modifierGroup = await dbContext.ModifierGroups.SingleOrDefaultAsync(g => g.Id == modifierGroupId);

        if (modifierGroup is null || modifierGroup.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var beforeIsActive = modifierGroup.IsActive;
        modifierGroup.IsActive = isActive;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ModifierGroupLifecycleDomainEvent(
            modifierGroup.TenantId,
            modifierGroup.OrganisationId,
            modifierGroup.Id,
            authContext.UserId,
            action,
            JsonSerializer.Serialize(new { IsActive = beforeIsActive }),
            JsonSerializer.Serialize(new { modifierGroup.IsActive }),
            DateTimeOffset.UtcNow));

        return Results.Ok(ModifierGroupResponse.FromEntity(modifierGroup));
    }

    private static IResult? ValidateFields(string name, int selectionMin, int selectionMax)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Results.BadRequest("Name is required.");
        }

        if (selectionMin < 0)
        {
            return Results.BadRequest("SelectionMin must not be negative.");
        }

        if (selectionMax < selectionMin)
        {
            return Results.BadRequest("SelectionMax must not be less than SelectionMin.");
        }

        return null;
    }
}
