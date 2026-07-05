using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Catalog;

public sealed record AssignProductModifierGroupRequest(Guid ProductId, Guid ModifierGroupId, int DisplayOrder, Guid? TenantId = null);

public sealed record ProductModifierGroupResponse(Guid Id, Guid TenantId, Guid ProductId, Guid ModifierGroupId, int DisplayOrder, DateTimeOffset CreatedAtUtc)
{
    public static ProductModifierGroupResponse FromEntity(ProductModifierGroup link) =>
        new(link.Id, link.TenantId, link.ProductId, link.ModifierGroupId, link.DisplayOrder, link.CreatedAtUtc);
}

/// <summary>
/// Assign/unassign endpoints for <see cref="ProductModifierGroup"/> (PLAN-0004 Milestone E) —
/// attaching a <see cref="ModifierGroup"/> to a <see cref="Product"/>. Unlike every other
/// catalogue entity in this plan, this join has only two operations, no list/read/update: changing
/// <see cref="ProductModifierGroup.DisplayOrder"/> means unassign then reassign. Both
/// <see cref="AssignProductModifierGroupRequest.ProductId"/> and
/// <see cref="AssignProductModifierGroupRequest.ModifierGroupId"/> must independently belong to the
/// caller's organisation — a mismatch on either is 404, matching
/// <see cref="Tax.TaxCategoryDefinitionEndpoints"/>'s multi-reference validation pattern.
/// </summary>
public static class ProductModifierGroupEndpoints
{
    public static void MapProductModifierGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/product-modifier-groups").RequireAuthorization();

        group.MapPost("", AssignAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
        group.MapDelete("/{productModifierGroupId:guid}", UnassignAsync).RequirePermission(Permissions.CatalogManage, rejectStaffPin: true);
    }

    private static async Task<IResult> AssignAsync(
        AssignProductModifierGroupRequest request,
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

        var product = await dbContext.Products.SingleOrDefaultAsync(p => p.Id == request.ProductId);
        if (product is null || product.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var modifierGroup = await dbContext.ModifierGroups.SingleOrDefaultAsync(g => g.Id == request.ModifierGroupId);
        if (modifierGroup is null || modifierGroup.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var link = new ProductModifierGroup
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            ProductId = request.ProductId,
            ModifierGroupId = request.ModifierGroupId,
            DisplayOrder = request.DisplayOrder,
            CreatedAtUtc = now,
        };

        dbContext.ProductModifierGroups.Add(link);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ProductModifierGroupChangedDomainEvent(
            link.TenantId, authContext.OrganisationId!.Value, link.Id, link.ProductId, link.ModifierGroupId, authContext.UserId,
            "Assigned", null, JsonSerializer.Serialize(new { link.ProductId, link.ModifierGroupId, link.DisplayOrder }), now));

        return Results.Created($"/api/v1/product-modifier-groups/{link.Id}", ProductModifierGroupResponse.FromEntity(link));
    }

    private static async Task<IResult> UnassignAsync(
        Guid productModifierGroupId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;

        var link = await FindAuthorizedAsync(productModifierGroupId, authContext.OrganisationId, dbContext);

        if (link is null)
        {
            return Results.NotFound();
        }

        var before = new { link.ProductId, link.ModifierGroupId, link.DisplayOrder };

        dbContext.ProductModifierGroups.Remove(link);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ProductModifierGroupChangedDomainEvent(
            link.TenantId, authContext.OrganisationId!.Value, link.Id, link.ProductId, link.ModifierGroupId, authContext.UserId,
            "Unassigned", JsonSerializer.Serialize(before), null, DateTimeOffset.UtcNow));

        return Results.NoContent();
    }

    /// <summary>
    /// Fetches a link (tenant filter applies) and confirms it belongs to
    /// <paramref name="callerOrganisationId"/> via its <see cref="Product"/>, since the link has no
    /// organisation column of its own. Returns <c>null</c> for either a missing link or an
    /// organisation mismatch (404 either way).
    /// </summary>
    private static async Task<ProductModifierGroup?> FindAuthorizedAsync(Guid productModifierGroupId, Guid? callerOrganisationId, DaxaDbContext dbContext)
    {
        var link = await dbContext.ProductModifierGroups.SingleOrDefaultAsync(l => l.Id == productModifierGroupId);

        if (link is null)
        {
            return null;
        }

        var product = await dbContext.Products.SingleOrDefaultAsync(p => p.Id == link.ProductId);

        return product is null || product.OrganisationId != callerOrganisationId ? null : link;
    }
}
