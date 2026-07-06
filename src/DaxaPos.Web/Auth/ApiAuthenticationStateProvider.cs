using System.Security.Claims;
using DaxaPos.Web.State;
using Microsoft.AspNetCore.Components.Authorization;

namespace DaxaPos.Web.Auth;

/// <summary>
/// Bridges the locally persisted <see cref="SessionState"/> (PLAN-0003 staff-PIN login) into
/// Blazor's <c>AuthorizeView</c>/<c>[Authorize]</c> components. Role/permission claims are a UX
/// hint only — permission checks are enforced server-side (CLAUDE.md), never re-derived here.
/// </summary>
public sealed class ApiAuthenticationStateProvider(IAuthSessionStore sessionStore) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = sessionStore.Current;

        if (session is null || session.IsExpired(DateTimeOffset.UtcNow))
        {
            return Task.FromResult(Anonymous);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.StaffMemberId.ToString()),
            new(ClaimTypes.Name, session.DisplayName),
        };
        claims.AddRange(session.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(session.Permissions.Select(permission => new Claim("daxa:permission", permission)));

        var identity = new ClaimsIdentity(claims, authenticationType: "DaxaStaffPin");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    /// <summary>Call after a store <c>Changed</c> event so <c>AuthorizeView</c> re-renders immediately.</summary>
    public void NotifyChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
