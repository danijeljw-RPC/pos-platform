using System.Security.Cryptography;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Identity;

public sealed record RegisterDeviceRequest(string Pin, string DeviceType, string? Name = null, Guid? TenantId = null);

/// <summary>
/// Returned once at registration — the only time the raw device token ever leaves the server.
/// Carries the fields ADR-0008 says an installed/PWA device stores locally.
/// </summary>
public sealed record RegisterDeviceResponse(
    Guid DeviceId,
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    string DeviceType,
    string Name,
    string DeviceToken);

/// <summary>
/// Pre-auth, PIN-gated device registration (ADR-0008, PLAN-0003 Milestone E). Not authenticated in
/// the <see cref="AuthContext"/> sense — the registration PIN is the human-controlled approval
/// step — but rate-limited per remote IP and audited where a tenant can be resolved. Every PIN
/// failure returns the same generic 401 so an attacker learns nothing about which PINs exist.
/// </summary>
public static class DeviceRegistrationEndpoints
{
    public const string RateLimitPolicyName = "device-registration";

    /// <summary>
    /// Bounds the pre-auth candidate scan. PINs live 15 minutes, so anything older than this can
    /// never validate and only matters for (already best-effort) failure auditing.
    /// </summary>
    private static readonly TimeSpan CandidateScanWindow = TimeSpan.FromHours(24);

    public static void MapDeviceRegistrationEndpoints(this IEndpointRouteBuilder app) =>
        app.MapPost("/api/v1/device-registration", RegisterAsync).RequireRateLimiting(RateLimitPolicyName);

    private static async Task<IResult> RegisterAsync(
        RegisterDeviceRequest request,
        DaxaDbContext dbContext,
        IDeviceCredentialHasher credentialHasher,
        IDomainEventDispatcher dispatcher)
    {
        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the registration PIN.");
        }

        if (string.IsNullOrWhiteSpace(request.Pin))
        {
            return Results.BadRequest("Pin is required.");
        }

        if (!Enum.TryParse<DeviceType>(request.DeviceType, ignoreCase: true, out var deviceType) || !Enum.IsDefined(deviceType))
        {
            return Results.BadRequest("DeviceType is not recognised.");
        }

        var now = DateTimeOffset.UtcNow;

        // Bootstrap exception (ADR-0015 §1): no tenant context exists yet — the matched PIN row is
        // what establishes it. PINs are stored as salted hashes, so this is a verify-scan over a
        // small, time-bounded candidate set, not a hash lookup (approved Milestone E decision 3).
        // The scan includes recently dead rows so matched-but-expired/revoked/exhausted attempts
        // can be audited against their tenant (approved decision 4).
        var scanCutoff = now.Subtract(CandidateScanWindow);
        var candidates = await dbContext.DeviceRegistrationPins
            .IgnoreQueryFilters()
            .Where(p => p.CreatedAtUtc >= scanCutoff)
            .ToListAsync();

        var matched = candidates.Where(p => credentialHasher.Verify(request.Pin, p.PinHash)).ToList();

        if (matched.Count == 0)
        {
            // Unknown PIN: generic failure, no AuditEvent — there is no tenant to attach the row
            // to (AuditEvent.TenantId is non-nullable), matching the Milestone C unknown-email
            // precedent. Rate limiting covers the brute-force path.
            return Results.Unauthorized();
        }

        var live = matched
            .Where(p => DeviceRegistrationPinPolicy.IsUsable(p.ExpiresAtUtc, p.RevokedAtUtc, p.UsedCount, p.MaxUses, now))
            .ToList();

        if (live.Count > 1)
        {
            // Ambiguous (a cross-PIN digit collision): fail closed per ADR-0015 — never pick one.
            // Audited only if every match resolves to a single tenant (approved decision 4).
            await AuditFailureIfSingleTenantAsync(dispatcher, matched, "AmbiguousPinMatch", now);
            return Results.Unauthorized();
        }

        if (live.Count == 0)
        {
            if (matched.Count == 1)
            {
                var pin = matched[0];
                var reason = pin.RevokedAtUtc is not null ? "PinRevoked"
                    : pin.ExpiresAtUtc <= now ? "PinExpired"
                    : "PinExhausted";

                await dispatcher.DispatchAsync(new DeviceRegistrationFailedDomainEvent(
                    pin.TenantId, pin.OrganisationId, pin.LocationId, pin.Id, reason, now));
            }
            else
            {
                await AuditFailureIfSingleTenantAsync(dispatcher, matched, "AmbiguousPinMatch", now);
            }

            return Results.Unauthorized();
        }

        var matchedPin = live[0];

        var device = new Device
        {
            Id = Guid.NewGuid(),
            TenantId = matchedPin.TenantId,
            LocationId = matchedPin.LocationId,
            DeviceType = deviceType,
            Name = request.Name?.Trim() ?? string.Empty,
            CreatedAtUtc = now,
        };

        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var credential = new DeviceCredential
        {
            Id = Guid.NewGuid(),
            TenantId = matchedPin.TenantId,
            DeviceId = device.Id,
            CredentialHash = credentialHasher.Hash(secret),
            Status = DeviceCredentialStatus.Active,
            IssuedAtUtc = now,
        };

        // UsedCount is incremented only after successful validation, in the same SaveChanges as
        // the device/credential rows (approved decision 3).
        matchedPin.UsedCount++;

        dbContext.Devices.Add(device);
        dbContext.DeviceCredentials.Add(credential);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new DeviceRegisteredDomainEvent(
            matchedPin.TenantId, matchedPin.OrganisationId, matchedPin.LocationId,
            device.Id, credential.Id, matchedPin.Id, deviceType.ToString(), now));

        return Results.Created(
            $"/api/v1/devices/{device.Id}",
            new RegisterDeviceResponse(
                device.Id,
                matchedPin.TenantId,
                matchedPin.OrganisationId,
                matchedPin.LocationId,
                deviceType.ToString(),
                device.Name,
                $"{credential.Id}.{secret}"));
    }

    private static async Task AuditFailureIfSingleTenantAsync(
        IDomainEventDispatcher dispatcher,
        IReadOnlyList<DeviceRegistrationPin> matched,
        string reason,
        DateTimeOffset now)
    {
        if (matched.Select(p => p.TenantId).Distinct().Count() != 1)
        {
            return;
        }

        var first = matched[0];
        var sharedOrganisationId = matched.All(p => p.OrganisationId == first.OrganisationId) ? first.OrganisationId : (Guid?)null;
        var sharedLocationId = matched.All(p => p.LocationId == first.LocationId) ? first.LocationId : (Guid?)null;

        await dispatcher.DispatchAsync(new DeviceRegistrationFailedDomainEvent(
            first.TenantId, sharedOrganisationId, sharedLocationId, PinId: null, reason, now));
    }
}
