using System.Security.Claims;
using System.Text.Encodings.Web;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;
using DaxaPos.Infrastructure.Identity;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DaxaPos.Api.Authentication;

/// <summary>
/// Validates a device credential token (ADR-0008, PLAN-0003 Milestone E), presented as
/// <c>Authorization: Device {credentialId}.{secret}</c> — the credential id is the lookup key,
/// the secret is verified against the stored salted hash in constant time. On success, stashes a
/// <b>partial</b> <see cref="AuthContext"/>: tenant/organisation/location/device populated from
/// server-side rows, no user/staff identity, and <b>empty</b> roles/permissions — a device token
/// is trusted device context only and must never grant user permissions by itself (ADR-0013).
/// Milestone F's staff PIN login combines this context with staff authentication.
/// </summary>
public sealed class DeviceTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    DaxaDbContext dbContext,
    IDeviceCredentialHasher deviceCredentialHasher)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "DeviceToken";

    public const string HeaderPrefix = "Device ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headerValue = Request.Headers.Authorization.ToString();

        if (!headerValue.StartsWith(HeaderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = headerValue[HeaderPrefix.Length..].Trim();
        var parts = token.Split('.', 2);

        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var credentialId) || parts[1].Length == 0)
        {
            return AuthenticateResult.Fail("Malformed device token.");
        }

        // Bootstrap exception (ADR-0015 §1): no tenant context can exist yet — this lookup is what
        // establishes it. The client supplies only the credential id (a lookup key, proven by the
        // secret) — never a tenant/location value.
        var credential = await dbContext.DeviceCredentials
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(c => c.Id == credentialId);

        if (credential is null ||
            credential.Status != DeviceCredentialStatus.Active ||
            !deviceCredentialHasher.Verify(parts[1], credential.CredentialHash))
        {
            return AuthenticateResult.Fail("Invalid, rotated, or revoked device credential.");
        }

        // Same bootstrap window — the tenant provider isn't populated until the AuthContext is
        // stashed below, so these resolve with the filter bypassed but explicitly pinned to the
        // credential's own TenantId. Fails closed if either row is missing or cross-tenant.
        var device = await dbContext.Devices
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(d => d.Id == credential.DeviceId && d.TenantId == credential.TenantId);

        var location = device is null
            ? null
            : await dbContext.Locations
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(l => l.Id == device.LocationId && l.TenantId == credential.TenantId);

        if (device is null || location is null)
        {
            return AuthenticateResult.Fail("Device context could not be resolved.");
        }

        var authContext = new AuthContext(
            TenantId: credential.TenantId,
            OrganisationId: location.OrganisationId,
            LocationId: device.LocationId,
            TerminalId: null,
            UserId: null,
            StaffMemberId: null,
            DeviceId: device.Id,
            AuthMethod: AuthMethod.DeviceToken,
            Roles: [],
            Permissions: []);

        Context.Items[HttpContextAuthContextAccessor.AuthContextItemKey] = authContext;

        var identity = new ClaimsIdentity(SchemeName);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, device.Id.ToString()));

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
