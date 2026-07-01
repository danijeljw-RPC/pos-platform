using DaxaPos.Application.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Api.Authorization;

/// <summary>
/// Minimal API endpoint filter enforcing a required permission against the request's
/// <see cref="AuthContext"/> (never against the raw authentication mechanism — ADR-0013).
/// Returns 401 if no <see cref="AuthContext"/> is present (should not normally be reachable if the
/// endpoint also requires authentication, but checked defensively), or 403 if authenticated but
/// missing the permission.
/// </summary>
public sealed class RequirePermissionFilter(string permissionCode) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var authContextAccessor = context.HttpContext.RequestServices.GetRequiredService<IAuthContextAccessor>();
        var authContext = authContextAccessor.Current;

        if (authContext is null)
        {
            return Results.Unauthorized();
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
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permissionCode)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(new RequirePermissionFilter(permissionCode));
}
