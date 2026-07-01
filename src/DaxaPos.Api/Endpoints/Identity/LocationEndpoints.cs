using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Identity;

public sealed record CreateLocationRequest(string Name, Guid OrganisationId, Guid? TenantId = null);

public sealed record UpdateLocationRequest(string Name, Guid? TenantId = null);

public sealed record LocationResponse(Guid Id, Guid TenantId, Guid OrganisationId, string Name, bool IsActive, DateTimeOffset CreatedAtUtc)
{
    public static LocationResponse FromEntity(Location location) =>
        new(location.Id, location.TenantId, location.OrganisationId, location.Name, location.IsActive, location.CreatedAtUtc);
}

/// <summary>
/// Create/read/rename/deactivate/reactivate endpoints for <see cref="Location"/> (PLAN-0003
/// Milestone D). Unlike <see cref="OrganisationEndpoints"/>, every operation here is also checked
/// against <c>AuthContext.OrganisationId</c> — the literal ADR-0015 Context Provenance example —
/// because <c>locations.manage</c> is also granted to organisation-scoped roles
/// (<c>OrganisationOwner</c>, per the Initial Permission Catalogue), not just <c>SystemAdmin</c>. A
/// mismatch returns 404, never 403, so a caller cannot learn that a location exists under a
/// different organisation. No hard delete.
/// </summary>
public static class LocationEndpoints
{
    public static void MapLocationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/locations").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.LocationsManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.LocationsManage, rejectStaffPin: true);
        group.MapGet("/{locationId:guid}", GetByIdAsync).RequirePermission(Permissions.LocationsManage, rejectStaffPin: true);
        group.MapPatch("/{locationId:guid}", UpdateAsync).RequirePermission(Permissions.LocationsManage, rejectStaffPin: true);
        group.MapPost("/{locationId:guid}/deactivate", DeactivateAsync).RequirePermission(Permissions.LocationsManage, rejectStaffPin: true);
        group.MapPost("/{locationId:guid}/reactivate", ReactivateAsync).RequirePermission(Permissions.LocationsManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateLocationRequest request,
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

        // Context provenance (ADR-0015): the caller-supplied OrganisationId identifies which
        // resource is being acted on, never a scope the caller can widen. A mismatch — including a
        // legitimate organisation the caller's tenant owns but isn't the caller's own — is a 404,
        // not a validation error, so existence under another organisation is never confirmed.
        if (authContext.OrganisationId != request.OrganisationId)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var location = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            OrganisationId = request.OrganisationId,
            Name = request.Name,
            IsActive = true,
            CreatedAtUtc = now,
        };

        dbContext.Locations.Add(location);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new LocationLifecycleDomainEvent(
            location.TenantId, location.OrganisationId, location.Id, authContext.UserId, "Created", null, JsonSerializer.Serialize(new { location.Name }), now));

        return Results.Created($"/api/v1/locations/{location.Id}", LocationResponse.FromEntity(location));
    }

    private static async Task<IResult> ListAsync(IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var locations = await dbContext.Locations
            .Where(l => l.OrganisationId == authContext.OrganisationId && l.IsActive)
            .OrderBy(l => l.Name)
            .ToListAsync();

        return Results.Ok(locations.Select(LocationResponse.FromEntity));
    }

    private static async Task<IResult> GetByIdAsync(Guid locationId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        // No IsActive filter — a manage-permission caller may look up an inactive location directly.
        var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == locationId);

        if (location is null || location.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        return Results.Ok(LocationResponse.FromEntity(location));
    }

    private static async Task<IResult> UpdateAsync(
        Guid locationId,
        UpdateLocationRequest request,
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
        var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == locationId);

        if (location is null || location.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var beforeName = location.Name;
        location.Name = request.Name;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new LocationLifecycleDomainEvent(
            location.TenantId,
            location.OrganisationId,
            location.Id,
            authContext.UserId,
            "Updated",
            JsonSerializer.Serialize(new { Name = beforeName }),
            JsonSerializer.Serialize(new { location.Name }),
            DateTimeOffset.UtcNow));

        return Results.Ok(LocationResponse.FromEntity(location));
    }

    private static Task<IResult> DeactivateAsync(
        Guid locationId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(locationId, isActive: false, action: "Deactivated", authContextAccessor, dbContext, dispatcher);

    private static Task<IResult> ReactivateAsync(
        Guid locationId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(locationId, isActive: true, action: "Reactivated", authContextAccessor, dbContext, dispatcher);

    private static async Task<IResult> SetActiveAsync(
        Guid locationId,
        bool isActive,
        string action,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;

        // No IsActive filter on the lookup — deactivate/reactivate must find the target row
        // regardless of its current state.
        var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == locationId);

        if (location is null || location.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var beforeIsActive = location.IsActive;
        location.IsActive = isActive;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new LocationLifecycleDomainEvent(
            location.TenantId,
            location.OrganisationId,
            location.Id,
            authContext.UserId,
            action,
            JsonSerializer.Serialize(new { IsActive = beforeIsActive }),
            JsonSerializer.Serialize(new { location.IsActive }),
            DateTimeOffset.UtcNow));

        return Results.Ok(LocationResponse.FromEntity(location));
    }
}
