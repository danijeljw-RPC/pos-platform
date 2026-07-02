using System.Security.Claims;
using System.Text.Encodings.Web;
using DaxaPos.Application.Identity;
using DaxaPos.Infrastructure.Identity;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DaxaPos.Api.Authentication;

/// <summary>
/// Validates the opaque, server-hashed POS/local session bearer token (ADR-0015) — never a JWT.
/// Enforces the two-part session-expiry policy per session type (admin/local sessions: 12h
/// absolute cap, 8h idle; staff PIN sessions: 8h absolute cap, 30min idle) and, on success,
/// stashes the session's role/permission snapshot as an <see cref="AuthContext"/> for
/// <see cref="HttpContextAuthContextAccessor"/> to read.
/// </summary>
public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    DaxaDbContext dbContext,
    ISessionTokenService sessionTokenService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "Session";

    private const string BearerPrefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headerValue = Request.Headers.Authorization.ToString();

        if (!headerValue.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = headerValue[BearerPrefix.Length..].Trim();

        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.Fail("Missing bearer token.");
        }

        var tokenHash = sessionTokenService.Hash(token);

        // Bootstrap exception (ADR-0015 §1): no tenant context can exist yet — this lookup is
        // what establishes it. Narrow, documented, not a general bypass, and not fed by any
        // client-supplied tenant value (the client only ever supplies the opaque token).
        var session = await dbContext.AuthSessions
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(s => s.SessionTokenHash == tokenHash);

        var now = DateTimeOffset.UtcNow;

        if (session is null || session.RevokedAtUtc is not null || IsExpired(session, now))
        {
            return AuthenticateResult.Fail("Invalid, expired, or revoked session.");
        }

        session.LastActivityAtUtc = now;
        await dbContext.SaveChangesAsync();

        var authContext = new AuthContext(
            TenantId: session.TenantId,
            OrganisationId: session.OrganisationId,
            LocationId: session.LocationId,
            TerminalId: session.TerminalId,
            UserId: session.UserId,
            StaffMemberId: session.StaffMemberId,
            DeviceId: session.DeviceId,
            AuthMethod: session.AuthMethod,
            Roles: session.RoleSnapshot,
            Permissions: session.PermissionSnapshot);

        Context.Items[HttpContextAuthContextAccessor.AuthContextItemKey] = authContext;

        var identity = new ClaimsIdentity(SchemeName);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, (session.UserId ?? session.StaffMemberId ?? Guid.Empty).ToString()));

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return AuthenticateResult.Success(ticket);
    }

    /// <summary>
    /// Staff PIN sessions are operational POS sessions with deliberately shorter lifetimes
    /// (8h absolute / 30min idle — PLAN-0003 Decision 6) than back-office/admin sessions
    /// (12h / 8h).
    /// </summary>
    private static bool IsExpired(Domain.Entities.AuthSession session, DateTimeOffset now) =>
        session.AuthMethod == Domain.Enums.AuthMethod.LocalStaffPin
            ? StaffSessionExpiryPolicy.IsExpired(session.IssuedAtUtc, session.LastActivityAtUtc, now)
            : SessionExpiryPolicy.IsExpired(session.IssuedAtUtc, session.LastActivityAtUtc, now);
}
