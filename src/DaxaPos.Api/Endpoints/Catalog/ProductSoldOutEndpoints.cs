using System.Text.Json;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Catalog;

public sealed record SetSoldOutRequest(bool IsSoldOut, Guid? TenantId = null);

/// <summary>
/// The plan's first genuinely staff-accessible write endpoint (PLAN-0004 Milestone F) —
/// <c>POST /api/v1/products/{productId}/locations/{locationId}/sold-out</c>, gated
/// <c>catalog.sold-out-toggle</c> (Milestone A's first <c>Operational</c>-category permission,
/// granted to <c>Staff</c>) with <c>rejectStaffPin: false</c>, deliberately the opposite of every
/// other Milestone F/C/D/E endpoint. May only ever touch
/// <see cref="ProductLocationOverride.IsSoldOut"/> — never <see cref="ProductLocationOverride.PriceOverride"/>
/// or <see cref="ProductLocationOverride.IsAvailable"/>, which remain <c>pricing.manage</c>-only via
/// <see cref="ProductLocationOverrideEndpoints"/>. Upserts the override row if none exists yet for
/// the (product, location) pair, defaulting <see cref="ProductLocationOverride.IsAvailable"/> to
/// <c>true</c> and <see cref="ProductLocationOverride.PriceOverride"/> to <c>null</c>. A staff-PIN
/// session's own <c>AuthContext.LocationId</c> (from its registered device) must match the target
/// <paramref name="locationId"/> — a POS terminal at one location must not toggle another
/// location's stock, even within the same organisation; an organisation-scoped admin session
/// (<c>LocationId</c> null) has no such restriction.
/// </summary>
public static class ProductSoldOutEndpoints
{
    public static void MapProductSoldOutEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/v1/products")
            .RequireAuthorization()
            .MapPost("/{productId:guid}/locations/{locationId:guid}/sold-out", SetSoldOutAsync)
            .RequirePermission(Permissions.CatalogSoldOutToggle, rejectStaffPin: false);
    }

    private static async Task<IResult> SetSoldOutAsync(
        Guid productId,
        Guid locationId,
        SetSoldOutRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        var authContext = authContextAccessor.Current!;

        // A location-bound session (staff PIN, from its device's registered location) may only
        // toggle sold-out at its own location — checked independently of the organisation match
        // below, same as ADR-0013's staff-PIN restrictions being checked independently of the
        // permission code itself.
        if (authContext.LocationId is not null && authContext.LocationId != locationId)
        {
            return Results.NotFound();
        }

        var product = await dbContext.Products.SingleOrDefaultAsync(p => p.Id == productId);
        if (product is null || product.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == locationId);
        if (location is null || location.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var over = await dbContext.ProductLocationOverrides.SingleOrDefaultAsync(o => o.ProductId == productId && o.LocationId == locationId);
        var before = over is null ? null : new { over.IsSoldOut };

        if (over is null)
        {
            over = new ProductLocationOverride
            {
                Id = Guid.NewGuid(),
                TenantId = authContext.TenantId,
                LocationId = locationId,
                ProductId = productId,
                IsAvailable = true,
                IsSoldOut = request.IsSoldOut,
                PriceOverride = null,
                CreatedAtUtc = now,
            };
            dbContext.ProductLocationOverrides.Add(over);
        }
        else
        {
            over.IsSoldOut = request.IsSoldOut;
        }

        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new ProductLocationOverrideChangedDomainEvent(
            over.TenantId, location.OrganisationId, over.Id, over.ProductId, over.LocationId,
            authContext.UserId, authContext.StaffMemberId,
            "SoldOutToggled",
            before is null ? null : JsonSerializer.Serialize(before),
            JsonSerializer.Serialize(new { over.IsSoldOut }),
            now));

        return Results.Ok(ProductLocationOverrideResponse.FromEntity(over));
    }
}
