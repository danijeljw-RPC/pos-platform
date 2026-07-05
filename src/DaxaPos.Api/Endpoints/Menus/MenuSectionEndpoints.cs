using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Menus;

public sealed record CreateMenuSectionRequest(string Name, Guid MenuId, int DisplayOrder, Guid? TenantId = null);

public sealed record UpdateMenuSectionRequest(string Name, int DisplayOrder, bool IsActive, Guid? TenantId = null);

public sealed record MenuSectionResponse(Guid Id, Guid TenantId, Guid MenuId, string Name, int DisplayOrder, bool IsActive, DateTimeOffset CreatedAtUtc)
{
    public static MenuSectionResponse FromEntity(MenuSection section) =>
        new(section.Id, section.TenantId, section.MenuId, section.Name, section.DisplayOrder, section.IsActive, section.CreatedAtUtc);
}

/// <summary>
/// Create/read/update endpoints for <see cref="MenuSection"/> (PLAN-0004 Milestone G). No
/// <c>OrganisationId</c> column of its own — every organisation check walks
/// <c>MenuSection -&gt; Menu -&gt; OrganisationId</c>, matching <see cref="ProductVariant"/>'s
/// <c>ProductVariant -&gt; Product</c> pattern. No separate deactivate/reactivate routes —
/// <c>IsActive</c> is one of the fields the single <c>PATCH</c> updates, matching the 4-endpoint
/// count the plan gives this entity (not the 6-endpoint sextet used elsewhere).
/// </summary>
public static class MenuSectionEndpoints
{
    public static void MapMenuSectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/menu-sections").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.MenusManage, rejectStaffPin: true);
        group.MapGet("", ListAsync).RequirePermission(Permissions.MenusManage, rejectStaffPin: true);
        group.MapGet("/{menuSectionId:guid}", GetByIdAsync).RequirePermission(Permissions.MenusManage, rejectStaffPin: true);
        group.MapPatch("/{menuSectionId:guid}", UpdateAsync).RequirePermission(Permissions.MenusManage, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateMenuSectionRequest request,
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

        var menu = await dbContext.Menus.SingleOrDefaultAsync(m => m.Id == request.MenuId);
        if (menu is null || menu.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var section = new MenuSection
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            MenuId = request.MenuId,
            Name = request.Name,
            DisplayOrder = request.DisplayOrder,
            IsActive = true,
            CreatedAtUtc = now,
        };

        dbContext.MenuSections.Add(section);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new MenuSectionLifecycleDomainEvent(
            section.TenantId, menu.OrganisationId, section.Id, authContext.UserId,
            "Created", null, JsonSerializer.Serialize(new { section.Name, section.DisplayOrder }), now));

        return Results.Created($"/api/v1/menu-sections/{section.Id}", MenuSectionResponse.FromEntity(section));
    }

    private static async Task<IResult> ListAsync(Guid? menuId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var query =
            from s in dbContext.MenuSections
            join m in dbContext.Menus on s.MenuId equals m.Id
            where m.OrganisationId == authContext.OrganisationId && s.IsActive
            select s;

        if (menuId is not null)
        {
            query = query.Where(s => s.MenuId == menuId);
        }

        var sections = await query.OrderBy(s => s.DisplayOrder).ToListAsync();

        return Results.Ok(sections.Select(MenuSectionResponse.FromEntity));
    }

    private static async Task<IResult> GetByIdAsync(Guid menuSectionId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;
        var section = await FindAuthorizedAsync(menuSectionId, authContext.OrganisationId, dbContext);

        return section is null ? Results.NotFound() : Results.Ok(MenuSectionResponse.FromEntity(section));
    }

    private static async Task<IResult> UpdateAsync(
        Guid menuSectionId,
        UpdateMenuSectionRequest request,
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
        var section = await FindAuthorizedAsync(menuSectionId, authContext.OrganisationId, dbContext);

        if (section is null)
        {
            return Results.NotFound();
        }

        var before = new { section.Name, section.DisplayOrder, section.IsActive };

        section.Name = request.Name;
        section.DisplayOrder = request.DisplayOrder;
        section.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync();

        var organisationId = await ResolveOrganisationIdAsync(section.MenuId, dbContext);

        await dispatcher.DispatchAsync(new MenuSectionLifecycleDomainEvent(
            section.TenantId, organisationId, section.Id, authContext.UserId,
            "Updated", JsonSerializer.Serialize(before), JsonSerializer.Serialize(new { section.Name, section.DisplayOrder, section.IsActive }), DateTimeOffset.UtcNow));

        return Results.Ok(MenuSectionResponse.FromEntity(section));
    }

    private static async Task<MenuSection?> FindAuthorizedAsync(Guid menuSectionId, Guid? callerOrganisationId, DaxaDbContext dbContext)
    {
        var section = await dbContext.MenuSections.SingleOrDefaultAsync(s => s.Id == menuSectionId);

        if (section is null)
        {
            return null;
        }

        var menu = await dbContext.Menus.SingleOrDefaultAsync(m => m.Id == section.MenuId);

        return menu is null || menu.OrganisationId != callerOrganisationId ? null : section;
    }

    private static async Task<Guid> ResolveOrganisationIdAsync(Guid menuId, DaxaDbContext dbContext) =>
        (await dbContext.Menus.SingleAsync(m => m.Id == menuId)).OrganisationId;
}
