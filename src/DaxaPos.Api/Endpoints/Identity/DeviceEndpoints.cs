using System.Security.Cryptography;
using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Identity;

public sealed record DeviceResponse(Guid Id, Guid LocationId, string DeviceType, string Name, bool HasActiveCredential, DateTimeOffset CreatedAtUtc);

/// <summary>Returned once at rotation — the only time the new raw device token leaves the server.</summary>
public sealed record RotateDeviceCredentialResponse(Guid DeviceId, string DeviceToken);

/// <summary>
/// Device credential lifecycle management (ADR-0008 rotation model, PLAN-0003 Milestone E).
/// Every operation resolves <c>Device → Location → OrganisationId</c> (a <see cref="Device"/> has
/// no <c>OrganisationId</c> column, like <see cref="Terminal"/>) and returns 404 for cross-tenant
/// or cross-organisation access. Revocation is terminal for the device identity — a revoked
/// device re-registers as a new <see cref="Device"/>, so rotation of a device with no active
/// credential is refused rather than resurrecting it.
/// </summary>
public static class DeviceEndpoints
{
    public static void MapDeviceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/devices").RequireAuthorization();

        group.MapGet("", ListAsync).RequirePermission(Permissions.DevicesManage, rejectStaffPin: true);
        group.MapPost("/{deviceId:guid}/rotate-credential", RotateCredentialAsync).RequirePermission(Permissions.DevicesManage, rejectStaffPin: true);
        group.MapPost("/{deviceId:guid}/revoke", RevokeAsync).RequirePermission(Permissions.DevicesManage, rejectStaffPin: true);
    }

    private static async Task<IResult> ListAsync(
        Guid? locationId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;

        var query = dbContext.Devices
            .Join(dbContext.Locations, d => d.LocationId, l => l.Id, (d, l) => new { Device = d, l.OrganisationId })
            .Where(x => x.OrganisationId == authContext.OrganisationId)
            .Select(x => x.Device);

        if (locationId is not null)
        {
            query = query.Where(d => d.LocationId == locationId);
        }

        var devices = await query.OrderBy(d => d.Name).ThenBy(d => d.CreatedAtUtc).ToListAsync();

        var deviceIds = devices.Select(d => d.Id).ToList();
        var activeDeviceIds = await dbContext.DeviceCredentials
            .Where(c => deviceIds.Contains(c.DeviceId) && c.Status == DeviceCredentialStatus.Active)
            .Select(c => c.DeviceId)
            .Distinct()
            .ToListAsync();

        return Results.Ok(devices.Select(d => ToResponse(d, activeDeviceIds.Contains(d.Id))));
    }

    private static async Task<IResult> RotateCredentialAsync(
        Guid deviceId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDeviceCredentialHasher credentialHasher,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;
        var resolved = await ResolveDeviceAsync(deviceId, authContext, dbContext);

        if (resolved is null)
        {
            return Results.NotFound();
        }

        var (device, location) = resolved.Value;

        var activeCredentials = await dbContext.DeviceCredentials
            .Where(c => c.DeviceId == device.Id && c.Status == DeviceCredentialStatus.Active)
            .OrderByDescending(c => c.IssuedAtUtc)
            .ToListAsync();

        if (activeCredentials.Count == 0)
        {
            return Results.Conflict("Device has no active credential to rotate; a revoked device must re-register.");
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var credential in activeCredentials)
        {
            credential.Status = DeviceCredentialStatus.Retired;
            credential.RotatedAtUtc = now;
        }

        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var newCredential = new DeviceCredential
        {
            Id = Guid.NewGuid(),
            TenantId = device.TenantId,
            DeviceId = device.Id,
            CredentialHash = credentialHasher.Hash(secret),
            Status = DeviceCredentialStatus.Active,
            IssuedAtUtc = now,
        };

        dbContext.DeviceCredentials.Add(newCredential);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new DeviceCredentialRotatedDomainEvent(
            device.TenantId, location.OrganisationId, device.LocationId,
            device.Id, activeCredentials[0].Id, newCredential.Id, authContext.UserId, now));

        return Results.Ok(new RotateDeviceCredentialResponse(device.Id, $"{newCredential.Id}.{secret}"));
    }

    private static async Task<IResult> RevokeAsync(
        Guid deviceId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;
        var resolved = await ResolveDeviceAsync(deviceId, authContext, dbContext);

        if (resolved is null)
        {
            return Results.NotFound();
        }

        var (device, location) = resolved.Value;

        var credentials = await dbContext.DeviceCredentials
            .Where(c => c.DeviceId == device.Id && c.Status != DeviceCredentialStatus.Revoked)
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;

        foreach (var credential in credentials)
        {
            credential.Status = DeviceCredentialStatus.Revoked;
            credential.RevokedAtUtc = now;
        }

        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new DeviceRevokedDomainEvent(
            device.TenantId, location.OrganisationId, device.LocationId, device.Id, authContext.UserId, now));

        return Results.Ok(ToResponse(device, hasActiveCredential: false));
    }

    /// <summary>
    /// Resolves a device under the caller's tenant (query filter) and organisation
    /// (<c>Device → Location → OrganisationId</c> walk); null means 404 — existence under another
    /// tenant/organisation is never confirmed.
    /// </summary>
    private static async Task<(Device Device, Location Location)?> ResolveDeviceAsync(
        Guid deviceId,
        AuthContext authContext,
        DaxaDbContext dbContext)
    {
        var device = await dbContext.Devices.SingleOrDefaultAsync(d => d.Id == deviceId);

        if (device is null)
        {
            return null;
        }

        var location = await dbContext.Locations.SingleOrDefaultAsync(l => l.Id == device.LocationId);

        if (location is null || location.OrganisationId != authContext.OrganisationId)
        {
            return null;
        }

        return (device, location);
    }

    private static DeviceResponse ToResponse(Device device, bool hasActiveCredential) =>
        new(device.Id, device.LocationId, device.DeviceType.ToString(), device.Name, hasActiveCredential, device.CreatedAtUtc);
}
