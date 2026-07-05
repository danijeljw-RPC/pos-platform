using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Menus;

public sealed record CreateMenuRequest(string Name, Guid OrganisationId, Guid? LocationId, Guid? TenantId = null);

public sealed record UpdateMenuRequest(string Name, Guid? LocationId, Guid? TenantId = null);

public sealed record MenuResponse(Guid Id, Guid TenantId, Guid OrganisationId, Guid? LocationId, string Name, bool IsActive, DateTimeOffset CreatedAtUtc)
{
    public static MenuResponse FromEntity(Menu menu) =>
        new(menu.Id, menu.TenantId, menu.OrganisationId, menu.LocationId, menu.Name, menu.IsActive, menu.CreatedAtUtc);
}

/// <summary>
/// Create/read/update/deactivate/reactivate endpoints for <see cref="Menu"/> (PLAN-0004 Milestone
/// G). Gated <c>menus.manage</c> + <c>rejectStaffPin: true</c> — configuration, not the
/// sales-floor read (contrast with <see cref="ResolvedMenuEndpoints"/>, which needs neither).
/// <see cref="Menu.LocationId"/> null means organisation-wide; set means location-specific, and
/// must belong to the caller's organisation like every other referenced entity in this plan.
/// </summary>
public static class MenuEndpoints
{
    public static void MapMenuEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/menus").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.MenusManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.MenusManage, rejectStaffPin: true);
        group.MapGet("/{menuId:guid}", GetByIdAsync).RequirePermission(Permissions.MenusManage, rejectStaffPin: true);
        group.MapPatch("/{menuId:guid}", UpdateAsync).RequirePermission(Permissions.MenusManage, rejectStaffPin: true);
        group.MapPost("/{menuId:guid}/deactivate", DeactivateAsync).RequirePermission(Permissions.MenusManage, rejectStaffPin: true);
        group.MapPost("/{menuId:guid}/reactivate", ReactivateAsync).RequirePermission(Permissions.MenusManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateMenuRequest request,
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

        if (request.LocationId is not null)
        {
            var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == request.LocationId);
            if (location is null || location.OrganisationId != authContext.OrganisationId)
            {
                return Results.NotFound();
            }
        }

        var now = DateTimeOffset.UtcNow;
        var menu = new Menu
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            OrganisationId = request.OrganisationId,
            LocationId = request.LocationId,
            Name = request.Name,
            IsActive = true,
            CreatedAtUtc = now,
        };

        dbContext.Menus.Add(menu);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new MenuLifecycleDomainEvent(
            menu.TenantId, menu.OrganisationId, menu.Id, authContext.UserId,
            "Created", null, JsonSerializer.Serialize(new { menu.Name, menu.LocationId }), now));

        return Results.Created($"/api/v1/menus/{menu.Id}", MenuResponse.FromEntity(menu));
    }

    private static async Task<IResult> ListAsync(Guid? locationId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var query = dbContext.Menus.Where(m => m.OrganisationId == authContext.OrganisationId && m.IsActive);

        if (locationId is not null)
        {
            query = query.Where(m => m.LocationId == locationId);
        }

        var menus = await query.OrderBy(m => m.Name).ToListAsync();

        return Results.Ok(menus.Select(MenuResponse.FromEntity));
    }

    private static async Task<IResult> GetByIdAsync(Guid menuId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var menu = await dbContext.Menus.SingleOrDefaultAsync(m => m.Id == menuId);

        if (menu is null || menu.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        return Results.Ok(MenuResponse.FromEntity(menu));
    }

    private static async Task<IResult> UpdateAsync(
        Guid menuId,
        UpdateMenuRequest request,
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
        var menu = await dbContext.Menus.SingleOrDefaultAsync(m => m.Id == menuId);

        if (menu is null || menu.OrganisationId != authContext.OrganisationId)
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

        var before = new { menu.Name, menu.LocationId };

        menu.Name = request.Name;
        menu.LocationId = request.LocationId;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new MenuLifecycleDomainEvent(
            menu.TenantId, menu.OrganisationId, menu.Id, authContext.UserId,
            "Updated", JsonSerializer.Serialize(before), JsonSerializer.Serialize(new { menu.Name, menu.LocationId }), DateTimeOffset.UtcNow));

        return Results.Ok(MenuResponse.FromEntity(menu));
    }

    private static Task<IResult> DeactivateAsync(
        Guid menuId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(menuId, isActive: false, action: "Deactivated", authContextAccessor, dbContext, dispatcher);

    private static Task<IResult> ReactivateAsync(
        Guid menuId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher) =>
        SetActiveAsync(menuId, isActive: true, action: "Reactivated", authContextAccessor, dbContext, dispatcher);

    private static async Task<IResult> SetActiveAsync(
        Guid menuId,
        bool isActive,
        string action,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;

        var menu = await dbContext.Menus.SingleOrDefaultAsync(m => m.Id == menuId);

        if (menu is null || menu.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var beforeIsActive = menu.IsActive;
        menu.IsActive = isActive;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new MenuLifecycleDomainEvent(
            menu.TenantId,
            menu.OrganisationId,
            menu.Id,
            authContext.UserId,
            action,
            JsonSerializer.Serialize(new { IsActive = beforeIsActive }),
            JsonSerializer.Serialize(new { menu.IsActive }),
            DateTimeOffset.UtcNow));

        return Results.Ok(MenuResponse.FromEntity(menu));
    }
}
