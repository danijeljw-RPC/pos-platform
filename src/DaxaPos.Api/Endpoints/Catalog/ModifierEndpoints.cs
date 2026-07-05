using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Catalog;

public sealed record CreateModifierRequest(string Name, Guid ModifierGroupId, decimal PriceDelta, Guid? TenantId = null);

public sealed record UpdateModifierRequest(string Name, decimal PriceDelta, Guid? TenantId = null);

public sealed record ModifierResponse(
    Guid Id,
    Guid TenantId,
    Guid ModifierGroupId,
    string Name,
    decimal PriceDelta,
    bool IsActive,
    DateTimeOffset CreatedAtUtc)
{
    public static ModifierResponse FromEntity(Modifier modifier) =>
        new(modifier.Id, modifier.TenantId, modifier.ModifierGroupId, modifier.Name, modifier.PriceDelta,
            modifier.IsActive, modifier.CreatedAtUtc);
}

/// <summary>
/// Create/read/update/deactivate/reactivate endpoints for <see cref="Modifier"/> (PLAN-0004
/// Milestone E). <see cref="Modifier"/> carries no <c>OrganisationId</c> column of its own, so
/// every organisation check here walks <c>Modifier -&gt; ModifierGroup -&gt; OrganisationId</c>,
/// matching <see cref="ProductVariantEndpoints"/>'s <c>ProductVariant -&gt; Product</c> pattern.
/// <see cref="PriceDelta"/> may be positive, zero, or negative — deliberately not validated with
/// <see cref="Product"/>'s <c>&gt;= 0</c> rule. No hard delete.
/// </summary>
public static class ModifierEndpoints
{
    public static void MapModifierEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/modifiers").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapGet("/{modifierId:guid}", GetByIdAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPatch("/{modifierId:guid}", UpdateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPost("/{modifierId:guid}/deactivate", DeactivateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapPost("/{modifierId:guid}/reactivate", ReactivateAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateModifierRequest request,
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

        var modifierGroup = await dbContext.ModifierGroups.SingleOrDefaultAsync(g => g.Id == request.ModifierGroupId);
        if (modifierGroup is null || modifierGroup.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var modifier = new Modifier
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            ModifierGroupId = request.ModifierGroupId,
            Name = request.Name,
            PriceDelta = request.PriceDelta,
            IsActive = true,
            CreatedAtUtc = now,
        };

        dbContext.Modifiers.Add(modifier);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ModifierLifecycleDomainEvent(
            modifier.TenantId, modifierGroup.OrganisationId, modifier.Id, authContext.UserId,
            "Created", null, JsonSerializer.Serialize(new { modifier.Name, modifier.PriceDelta }), now));

        return Results.Created($"/api/v1/modifiers/{modifier.Id}", ModifierResponse.FromEntity(modifier));
    }

    private static async Task<IResult> ListAsync(
        Guid? modifierGroupId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var query =
            from m in dbContext.Modifiers
            join g in dbContext.ModifierGroups on m.ModifierGroupId equals g.Id
            where g.OrganisationId == authContext.OrganisationId && m.IsActive
            select m;

        if (modifierGroupId is not null)
        {
            query = query.Where(m => m.ModifierGroupId == modifierGroupId);
        }

        var modifiers = await query.OrderBy(m => m.Name).ToListAsync();

        return Results.Ok(modifiers.Select(ModifierResponse.FromEntity));
    }

    private static async Task<IResult> GetByIdAsync(Guid modifierId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;
        var modifier = await FindAuthorizedModifierAsync(modifierId, authContext.OrganisationId, dbContext);

        return modifier is null ? Results.NotFound() : Results.Ok(ModifierResponse.FromEntity(modifier));
    }

    private static async Task<IResult> UpdateAsync(
        Guid modifierId,
        UpdateModifierRequest request,
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
        var modifier = await FindAuthorizedModifierAsync(modifierId, authContext.OrganisationId, dbContext);

        if (modifier is null)
        {
            return Results.NotFound();
        }

        var before = new { modifier.Name, modifier.PriceDelta };

        modifier.Name = request.Name;
        modifier.PriceDelta = request.PriceDelta;
        await dbContext.SaveChangesAsync();

        var organisationId = await ResolveOrganisationIdAsync(modifier.ModifierGroupId, dbContext);

        await dispatcher.DispatchAsync(new ModifierLifecycleDomainEvent(
            modifier.TenantId, organisationId, modifier.Id, authContext.UserId,
            "Updated", JsonSerializer.Serialize(before), JsonSerializer.Serialize(new { modifier.Name, modifier.PriceDelta }), DateTimeOffset.UtcNow));

        return Results.Ok(ModifierResponse.FromEntity(modifier));
    }

    private static Task<IResult> DeactivateAsync(
        Guid modifierId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(modifierId, isActive: false, action: "Deactivated", authContextAccessor, dbContext, dispatcher);

    private static Task<IResult> ReactivateAsync(
        Guid modifierId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(modifierId, isActive: true, action: "Reactivated", authContextAccessor, dbContext, dispatcher);

    private static async Task<IResult> SetActiveAsync(
        Guid modifierId,
        bool isActive,
        string action,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;
        var modifier = await FindAuthorizedModifierAsync(modifierId, authContext.OrganisationId, dbContext);

        if (modifier is null)
        {
            return Results.NotFound();
        }

        var beforeIsActive = modifier.IsActive;
        modifier.IsActive = isActive;
        await dbContext.SaveChangesAsync();

        var organisationId = await ResolveOrganisationIdAsync(modifier.ModifierGroupId, dbContext);

        await dispatcher.DispatchAsync(new ModifierLifecycleDomainEvent(
            modifier.TenantId,
            organisationId,
            modifier.Id,
            authContext.UserId,
            action,
            JsonSerializer.Serialize(new { IsActive = beforeIsActive }),
            JsonSerializer.Serialize(new { modifier.IsActive }),
            DateTimeOffset.UtcNow));

        return Results.Ok(ModifierResponse.FromEntity(modifier));
    }

    /// <summary>
    /// Fetches a modifier (tenant filter applies) and confirms it belongs to
    /// <paramref name="callerOrganisationId"/> via its <see cref="ModifierGroup"/>. Returns
    /// <c>null</c> for either a missing modifier or an organisation mismatch (404 either way).
    /// </summary>
    private static async Task<Modifier?> FindAuthorizedModifierAsync(Guid modifierId, Guid? callerOrganisationId, DaxaDbContext dbContext)
    {
        var modifier = await dbContext.Modifiers.SingleOrDefaultAsync(m => m.Id == modifierId);

        if (modifier is null)
        {
            return null;
        }

        var modifierGroup = await dbContext.ModifierGroups.SingleOrDefaultAsync(g => g.Id == modifier.ModifierGroupId);

        return modifierGroup is null || modifierGroup.OrganisationId != callerOrganisationId ? null : modifier;
    }

    private static async Task<Guid> ResolveOrganisationIdAsync(Guid modifierGroupId, DaxaDbContext dbContext) =>
        (await dbContext.ModifierGroups.SingleAsync(g => g.Id == modifierGroupId)).OrganisationId;
}
