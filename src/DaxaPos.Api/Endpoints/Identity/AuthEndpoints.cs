using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Identity;

public sealed record LocalLoginRequest(string Email, string Password);

public sealed record LocalLoginResponse(
    string SessionToken,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record StaffPinLoginRequest(Guid LocationId, string StaffCode, string Pin, Guid? TenantId = null);

public sealed record StaffPinLoginResponse(
    string SessionToken,
    DateTimeOffset ExpiresAtUtc,
    Guid StaffMemberId,
    string DisplayName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record AuthContextResponse(
    Guid TenantId,
    Guid? OrganisationId,
    Guid? LocationId,
    Guid? TerminalId,
    Guid? UserId,
    Guid? StaffMemberId,
    Guid? DeviceId,
    string AuthMethod,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public static class AuthEndpoints
{
    private const string BearerPrefix = "Bearer ";

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/local/login", LocalLoginAsync);
        group.MapPost("/staff-pin/login", StaffPinLoginAsync).RequireAuthorization();
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapGet("/me", Me).RequireAuthorization();
    }

    /// <summary>
    /// Daxa WebAPI-native staff PIN login (ADR-0013 — never Keycloak/OIDC). Requires a trusted
    /// <c>DeviceToken</c> context first: anonymous → 401 (via <c>RequireAuthorization</c>), a
    /// <c>Bearer</c> admin/session token → 403. Tenant/organisation/location/device scope comes
    /// exclusively from the device's <see cref="AuthContext"/>; the body's <c>LocationId</c> is a
    /// cross-check, never a scope. Every failure returns the same generic 401 — never disclosing
    /// whether the staff code or the PIN was wrong — but is audited with the specific reason
    /// (tenant context is known from the device, unlike the unknown-email/unknown-PIN precedents).
    /// </summary>
    private static async Task<IResult> StaffPinLoginAsync(
        StaffPinLoginRequest request,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IPinHasher pinHasher,
        ISessionTokenService sessionTokenService,
        IDomainEventDispatcher dispatcher,
        ILoggerFactory loggerFactory)
    {
        var deviceContext = authContextAccessor.Current;

        if (deviceContext is null)
        {
            return Results.Unauthorized();
        }

        if (deviceContext.AuthMethod != AuthMethod.DeviceToken)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (request.TenantId is not null)
        {
            return Results.BadRequest("TenantId must not be supplied; it is derived from the device context.");
        }

        // A DeviceToken AuthContext always carries these (resolved server-side in
        // DeviceTokenAuthenticationHandler from the credential's own rows).
        var organisationId = deviceContext.OrganisationId!.Value;
        var locationId = deviceContext.LocationId!.Value;
        var deviceId = deviceContext.DeviceId!.Value;
        var now = DateTimeOffset.UtcNow;

        async Task<IResult> FailAsync(Guid? staffMemberId, string reason)
        {
            await dispatcher.DispatchAsync(new StaffPinLoginFailedDomainEvent(
                deviceContext.TenantId, organisationId, locationId, deviceId, staffMemberId, reason, now));
            return Results.Unauthorized();
        }

        if (request.LocationId != locationId)
        {
            return await FailAsync(null, "LocationMismatch");
        }

        // The device context established the tenant, so this lookup runs under the normal
        // fail-closed tenant filter — no bootstrap IgnoreQueryFilters() needed or wanted.
        var staffCode = StaffCodePolicy.Normalize(request.StaffCode);
        var staffMember = await dbContext.StaffMembers
            .SingleOrDefaultAsync(s => s.OrganisationId == organisationId && s.StaffCode == staffCode);

        if (staffMember is null)
        {
            return await FailAsync(null, "UnknownStaffCode");
        }

        if (!staffMember.IsActive)
        {
            return await FailAsync(staffMember.Id, "StaffInactive");
        }

        if (staffMember.LockedOutUntilUtc is { } lockedOutUntil && lockedOutUntil > now)
        {
            return await FailAsync(staffMember.Id, "LockedOut");
        }

        if (staffMember.LocationId != locationId)
        {
            return await FailAsync(staffMember.Id, "HomeLocationMismatch");
        }

        if (!pinHasher.Verify(request.Pin, staffMember.PinHash))
        {
            staffMember.FailedPinAttempts++;

            if (LoginLockoutPolicy.ShouldLockOut(staffMember.FailedPinAttempts))
            {
                staffMember.LockedOutUntilUtc = now.Add(LoginLockoutPolicy.LockoutDuration);
                staffMember.FailedPinAttempts = 0;
            }

            await dbContext.SaveChangesAsync();
            return await FailAsync(staffMember.Id, "InvalidPin");
        }

        staffMember.FailedPinAttempts = 0;
        staffMember.LockedOutUntilUtc = null;

        var roleNames = await dbContext.StaffMemberRoles
            .Where(sr => sr.StaffMemberId == staffMember.Id)
            .Join(dbContext.Roles, sr => sr.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync();

        var permissionCodes = await dbContext.StaffMemberRoles
            .Where(sr => sr.StaffMemberId == staffMember.Id)
            .Join(dbContext.RolePermissions, sr => sr.RoleId, rp => rp.RoleId, (_, rp) => rp.PermissionId)
            .Join(dbContext.Permissions, permissionId => permissionId, p => p.Id, (_, p) => p.Code)
            .Distinct()
            .ToListAsync();

        // Defense-in-depth beneath the endpoint-level rejectStaffPin net (Decision 8): a staff
        // PIN session must never hold admin-sensitive permissions. This is a role-configuration
        // error, logged as such, but the client still sees only the generic failure.
        if (permissionCodes.Any(Application.Identity.Permissions.AdminSensitive.Contains))
        {
            loggerFactory.CreateLogger("DaxaPos.Api.StaffPinLogin").LogError(
                "Staff PIN login rejected for staff member {StaffMemberId}: assigned role grants admin-sensitive permissions. Fix the role assignment.",
                staffMember.Id);
            return await FailAsync(staffMember.Id, "RoleGrantsSensitivePermissions");
        }

        var rawToken = sessionTokenService.GenerateToken();
        var authSession = new AuthSession
        {
            Id = Guid.NewGuid(),
            TenantId = deviceContext.TenantId,
            OrganisationId = organisationId,
            LocationId = locationId,
            DeviceId = deviceId,
            StaffMemberId = staffMember.Id,
            AuthMethod = AuthMethod.LocalStaffPin,
            RoleSnapshot = roleNames,
            PermissionSnapshot = permissionCodes,
            SessionTokenHash = sessionTokenService.Hash(rawToken),
            IssuedAtUtc = now,
            ExpiresAtUtc = now.Add(StaffSessionExpiryPolicy.AbsoluteLifetime),
            LastActivityAtUtc = now,
        };

        dbContext.AuthSessions.Add(authSession);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new StaffPinLoginSucceededDomainEvent(
            deviceContext.TenantId, organisationId, locationId, deviceId, staffMember.Id, authSession.Id, now));

        return Results.Ok(new StaffPinLoginResponse(
            rawToken, authSession.ExpiresAtUtc, staffMember.Id, staffMember.DisplayName, roleNames, permissionCodes));
    }

    private static async Task<IResult> LocalLoginAsync(
        LocalLoginRequest request,
        DaxaDbContext dbContext,
        IPinHasher passwordHasher,
        ISessionTokenService sessionTokenService,
        IDomainEventDispatcher dispatcher)
    {
        // Bootstrap exception (ADR-0015 §1): tenant is not known until the user is resolved by
        // email — this is the one documented lookup that establishes it, not fed by a
        // client-supplied tenant value (the client only ever supplies email/password).
        var user = await dbContext.Users.IgnoreQueryFilters().SingleOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
        {
            // No real User matched: no tenant to audit against — see LocalUserLoginFailedDomainEvent remarks.
            return Results.Unauthorized();
        }

        var now = DateTimeOffset.UtcNow;

        if (!user.IsActive)
        {
            await dispatcher.DispatchAsync(new LocalUserLoginFailedDomainEvent(user.TenantId, user.OrganisationId, user.Id, "UserInactive", now));
            return Results.Unauthorized();
        }

        if (user.LockedOutUntilUtc is { } lockedOutUntil && lockedOutUntil > now)
        {
            await dispatcher.DispatchAsync(new LocalUserLoginFailedDomainEvent(user.TenantId, user.OrganisationId, user.Id, "LockedOut", now));
            return Results.Unauthorized();
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash ?? string.Empty))
        {
            user.FailedLoginCount++;

            if (LoginLockoutPolicy.ShouldLockOut(user.FailedLoginCount))
            {
                user.LockedOutUntilUtc = now.Add(LoginLockoutPolicy.LockoutDuration);
                user.FailedLoginCount = 0;
            }

            await dbContext.SaveChangesAsync();
            await dispatcher.DispatchAsync(new LocalUserLoginFailedDomainEvent(user.TenantId, user.OrganisationId, user.Id, "InvalidPassword", now));

            return Results.Unauthorized();
        }

        user.FailedLoginCount = 0;
        user.LockedOutUntilUtc = null;

        var roleNames = await dbContext.UserRoles
            .IgnoreQueryFilters()
            .Where(ur => ur.UserId == user.Id)
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync();

        var permissionCodes = await dbContext.UserRoles
            .IgnoreQueryFilters()
            .Where(ur => ur.UserId == user.Id)
            .Join(dbContext.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (_, rp) => rp.PermissionId)
            .Join(dbContext.Permissions, permissionId => permissionId, p => p.Id, (_, p) => p.Code)
            .Distinct()
            .ToListAsync();

        var rawToken = sessionTokenService.GenerateToken();
        var authSession = new AuthSession
        {
            Id = Guid.NewGuid(),
            TenantId = user.TenantId,
            OrganisationId = user.OrganisationId,
            UserId = user.Id,
            AuthMethod = AuthMethod.LocalUsernamePassword,
            RoleSnapshot = roleNames,
            PermissionSnapshot = permissionCodes,
            SessionTokenHash = sessionTokenService.Hash(rawToken),
            IssuedAtUtc = now,
            ExpiresAtUtc = now.Add(SessionExpiryPolicy.AbsoluteLifetime),
            LastActivityAtUtc = now,
        };

        dbContext.AuthSessions.Add(authSession);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new LocalUserLoginSucceededDomainEvent(user.TenantId, user.OrganisationId, user.Id, authSession.Id, now));

        return Results.Ok(new LocalLoginResponse(rawToken, authSession.ExpiresAtUtc, roleNames, permissionCodes));
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        DaxaDbContext dbContext,
        ISessionTokenService sessionTokenService,
        IDomainEventDispatcher dispatcher)
    {
        var headerValue = httpContext.Request.Headers.Authorization.ToString();

        if (!headerValue.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Unauthorized();
        }

        var token = headerValue[BearerPrefix.Length..].Trim();
        var tokenHash = sessionTokenService.Hash(token);

        var session = await dbContext.AuthSessions.IgnoreQueryFilters().SingleOrDefaultAsync(s => s.SessionTokenHash == tokenHash);

        if (session is null || session.RevokedAtUtc is not null)
        {
            return Results.Unauthorized();
        }

        var now = DateTimeOffset.UtcNow;
        session.RevokedAtUtc = now;
        session.RevokedReason = "UserLogout";
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchAsync(new AuthSessionRevokedDomainEvent(
            session.TenantId, session.OrganisationId, session.Id, session.UserId, session.StaffMemberId, "UserLogout", now));

        return Results.Ok();
    }

    private static IResult Me(IAuthContextAccessor authContextAccessor)
    {
        var authContext = authContextAccessor.Current;

        if (authContext is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new AuthContextResponse(
            authContext.TenantId,
            authContext.OrganisationId,
            authContext.LocationId,
            authContext.TerminalId,
            authContext.UserId,
            authContext.StaffMemberId,
            authContext.DeviceId,
            authContext.AuthMethod.ToString(),
            authContext.Roles,
            authContext.Permissions));
    }
}
