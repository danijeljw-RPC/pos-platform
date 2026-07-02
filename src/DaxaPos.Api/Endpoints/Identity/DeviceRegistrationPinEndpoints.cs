using System.Security.Cryptography;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Identity;

public sealed record CreateDeviceRegistrationPinRequest(Guid LocationId, int? MaxUses = null, Guid? TenantId = null);

/// <summary>Returned once at creation — the only time the raw PIN ever leaves the server.</summary>
public sealed record DeviceRegistrationPinCreatedResponse(Guid Id, Guid LocationId, string Pin, DateTimeOffset ExpiresAtUtc, int MaxUses);

public sealed record DeviceRegistrationPinResponse(Guid Id, Guid LocationId, DateTimeOffset ExpiresAtUtc, int MaxUses, int UsedCount, DateTimeOffset? RevokedAtUtc);

/// <summary>
/// Admin issuance and revocation of device registration PINs (ADR-0008, PLAN-0003 Milestone E).
/// A PIN is a short-lived enrolment secret scoped to the caller's organisation's location; it is
/// stored hashed and the raw value is returned exactly once. Flat routes with the parent id in
/// the body, cross-checked against <c>AuthContext.OrganisationId</c> — the Milestone D convention.
/// </summary>
public static class DeviceRegistrationPinEndpoints
{
    public static void MapDeviceRegistrationPinEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/device-registration-pins").RequireAuthorization();

        group.MapPost("", CreateAsync).RequirePermission(Permissions.DevicesRegister, rejectStaffPin: true);
        group.MapPost("/{pinId:guid}/revoke", RevokeAsync).RequirePermission(Permissions.DevicesRegister, rejectStaffPin: true);
    }

    private static async Task<IResult> CreateAsync(
        CreateDeviceRegistrationPinRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDeviceCredentialHasher pinHasher,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the authenticated session.");
        }

        var maxUses = request.MaxUses ?? DeviceRegistrationPinPolicy.DefaultMaxUses;

        if (!DeviceRegistrationPinPolicy.IsValidMaxUses(maxUses))
        {
            return Results.BadRequest(
                $"MaxUses must be between {DeviceRegistrationPinPolicy.MinMaxUses} and {DeviceRegistrationPinPolicy.MaxMaxUses}.");
        }

        var authContext = authContextAccessor.Current!;

        // Context provenance (ADR-0015): the caller-supplied LocationId identifies which resource
        // is acted on, never a scope the caller can widen — a location outside the caller's tenant
        // (invisible via the query filter) or organisation is a 404, not a validation error.
        var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == request.LocationId);

        if (location is null || location.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        var rawPin = RandomNumberGenerator
            .GetInt32(0, (int)Math.Pow(10, DeviceRegistrationPinPolicy.PinLength))
            .ToString($"D{DeviceRegistrationPinPolicy.PinLength}");

        var pin = new DeviceRegistrationPin
        {
            Id = Guid.NewGuid(),
            TenantId = authContext.TenantId,
            OrganisationId = location.OrganisationId,
            LocationId = location.Id,
            PinHash = pinHasher.Hash(rawPin),
            ExpiresAtUtc = now.Add(DeviceRegistrationPinPolicy.Lifetime),
            MaxUses = maxUses,
            UsedCount = 0,
            CreatedByUserId = authContext.UserId!.Value,
            CreatedAtUtc = now,
        };

        dbContext.DeviceRegistrationPins.Add(pin);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new DeviceRegistrationPinCreatedDomainEvent(
            pin.TenantId, pin.OrganisationId, pin.LocationId, pin.Id, authContext.UserId, pin.ExpiresAtUtc, pin.MaxUses, now));

        return Results.Created(
            $"/api/v1/device-registration-pins/{pin.Id}",
            new DeviceRegistrationPinCreatedResponse(pin.Id, pin.LocationId, rawPin, pin.ExpiresAtUtc, pin.MaxUses));
    }

    private static async Task<IResult> RevokeAsync(
        Guid pinId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;
        var pin = await dbContext.DeviceRegistrationPins.SingleOrDefaultAsync(p => p.Id == pinId);

        if (pin is null || pin.OrganisationId != authContext.OrganisationId)
        {
            return Results.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        // Idempotent, matching the Milestone D deactivate/reactivate convention — a repeat revoke
        // keeps the original revocation timestamp.
        pin.RevokedAtUtc ??= now;
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new DeviceRegistrationPinRevokedDomainEvent(
            pin.TenantId, pin.OrganisationId, pin.LocationId, pin.Id, authContext.UserId, now));

        return Results.Ok(new DeviceRegistrationPinResponse(
            pin.Id, pin.LocationId, pin.ExpiresAtUtc, pin.MaxUses, pin.UsedCount, pin.RevokedAtUtc));
    }
}
