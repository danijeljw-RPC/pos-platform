using DaxaPos.Application.Identity;
using DaxaPos.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Api.Authorization;

/// <summary>
/// Minimal API endpoint filter enforcing a required permission against the request's
/// <see cref="AuthContext"/> (never against the raw authentication mechanism — ADR-0013).
/// Returns 401 if no <see cref="AuthContext"/> is present (should not normally be reachable if the
/// endpoint also requires authentication, but checked defensively), 403 if
/// <paramref name="rejectStaffPin"/> is set and the session's <see cref="AuthMethod"/> is
/// <see cref="AuthMethod.LocalStaffPin"/> (checked independently of the permission code, so a
/// misconfigured role can't bypass this by matching the permission string — ADR-0013's staff-PIN
/// restrictions), or 403 if authenticated but missing the permission.
/// </summary>
public sealed class RequirePermissionFilter(string permissionCode, bool rejectStaffPin = false) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var authContextAccessor = context.HttpContext.RequestServices.GetRequiredService<IAuthContextAccessor>();
        var authContext = authContextAccessor.Current;

        if (authContext is null)
        {
            return Results.Unauthorized();
        }

        if (rejectStaffPin && authContext.AuthMethod == AuthMethod.LocalStaffPin)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!authContext.Permissions.Contains(permissionCode))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }
}

public static class RequirePermissionEndpointExtensions
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permissionCode, bool rejectStaffPin = false)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(new RequirePermissionFilter(permissionCode, rejectStaffPin));
}
