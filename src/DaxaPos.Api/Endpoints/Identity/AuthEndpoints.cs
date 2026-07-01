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
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapGet("/me", Me).RequireAuthorization();
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
