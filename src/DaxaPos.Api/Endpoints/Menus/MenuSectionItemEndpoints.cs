using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Menus;

public sealed record AssignMenuSectionItemRequest(Guid MenuSectionId, Guid ProductId, int DisplayOrder, Guid? TenantId = null);

public sealed record MenuSectionItemResponse(Guid Id, Guid TenantId, Guid MenuSectionId, Guid ProductId, int DisplayOrder, DateTimeOffset CreatedAtUtc)
{
    public static MenuSectionItemResponse FromEntity(MenuSectionItem item) =>
        new(item.Id, item.TenantId, item.MenuSectionId, item.ProductId, item.DisplayOrder, item.CreatedAtUtc);
}

/// <summary>
/// Assign/unassign endpoints for <see cref="MenuSectionItem"/> (PLAN-0004 Milestone G) — attaching
/// a <see cref="Product"/> to a <see cref="MenuSection"/>. Same two-operation, no-lifecycle shape
/// as <see cref="Catalog.ProductModifierGroupEndpoints"/>: changing <c>DisplayOrder</c> means
/// unassign then reassign. Assignment rejects an archived or inactive product — the plan's
/// "excludes archived/unavailable products" requirement enforced at configuration time, in
/// addition to the resolved-menu endpoint's own defensive re-check at read time (a product can be
/// archived-and-replaced after assignment, which this check alone cannot prevent).
/// </summary>
public static class MenuSectionItemEndpoints
{
    public static void MapMenuSectionItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/menu-section-items").RequireAuthorization();

        group.MapPost("", AssignAsync).RequirePermission(Permissions.MenusManage, rejectStaffPin: true);
        group.MapDelete("/{menuSectionItemId:guid}", UnassignAsync).RequirePermission(Permissions.MenusManage, rejectStaffPin: true);
    }

    private static async Task<IResult> AssignAsync(
        AssignMenuSectionItemRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        if (request.DisplayOrder < 0)
        {
            return Results.BadRequest("DisplayOrder must not be negative.");
        }

        var authContext = authContextAccessor.Current!;

        var section = await dbContext.MenuSections.SingleOrDefaultAsync(s => s.Id == request.MenuSectionId);
        if (section is null)
        {
            return Results.NotFound();
        }

        var menu = await dbContext.Menus.SingleOrDefaultAsync(m => m.Id == section.MenuId);
        if (menu is null || menu.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var product = await dbContext.Products.SingleOrDefaultAsync(p => p.Id == request.ProductId);
        if (product is null || product.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        if (!product.IsActive || product.IsArchived)
        {
            return Results.BadRequest("Cannot assign an inactive or archived product to a menu.");
        }

        var now = DateTimeOffset.UtcNow;
        var item = new MenuSectionItem
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            MenuSectionId = request.MenuSectionId,
            ProductId = request.ProductId,
            DisplayOrder = request.DisplayOrder,
            CreatedAtUtc = now,
        };

        dbContext.MenuSectionItems.Add(item);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new MenuSectionItemChangedDomainEvent(
            item.TenantId, menu.OrganisationId, item.Id, item.MenuSectionId, item.ProductId, authContext.UserId,
            "Assigned", null, JsonSerializer.Serialize(new { item.MenuSectionId, item.ProductId, item.DisplayOrder }), now));

        return Results.Created($"/api/v1/menu-section-items/{item.Id}", MenuSectionItemResponse.FromEntity(item));
    }

    private static async Task<IResult> UnassignAsync(
        Guid menuSectionItemId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;

        var item = await FindAuthorizedAsync(menuSectionItemId, authContext.OrganisationId, dbContext);

        if (item is null)
        {
            return Results.NotFound();
        }

        var before = new { item.MenuSectionId, item.ProductId, item.DisplayOrder };
        var organisationId = await ResolveOrganisationIdAsync(item.MenuSectionId, dbContext);

        dbContext.MenuSectionItems.Remove(item);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new MenuSectionItemChangedDomainEvent(
            item.TenantId, organisationId, item.Id, item.MenuSectionId, item.ProductId, authContext.UserId,
            "Unassigned", JsonSerializer.Serialize(before), null, DateTimeOffset.UtcNow));

        return Results.NoContent();
    }

    /// <summary>
    /// Fetches an item (tenant filter applies) and confirms it belongs to
    /// <paramref name="callerOrganisationId"/> via <c>MenuSectionItem -&gt; MenuSection -&gt; Menu</c>,
    /// since neither intermediate entity carries an organisation column (404 either way).
    /// </summary>
    private static async Task<MenuSectionItem?> FindAuthorizedAsync(Guid menuSectionItemId, Guid? callerOrganisationId, DaxaDbContext dbContext)
    {
        var item = await dbContext.MenuSectionItems.SingleOrDefaultAsync(i => i.Id == menuSectionItemId);

        if (item is null)
        {
            return null;
        }

        var section = await dbContext.MenuSections.SingleOrDefaultAsync(s => s.Id == item.MenuSectionId);
        if (section is null)
        {
            return null;
        }

        var menu = await dbContext.Menus.SingleOrDefaultAsync(m => m.Id == section.MenuId);

        return menu is null || menu.OrganisationId != callerOrganisationId ? null : item;
    }

    private static async Task<Guid> ResolveOrganisationIdAsync(Guid menuSectionId, DaxaDbContext dbContext)
    {
        var section = await dbContext.MenuSections.SingleAsync(s => s.Id == menuSectionId);
        return (await dbContext.Menus.SingleAsync(m => m.Id == section.MenuId)).OrganisationId;
    }
}
