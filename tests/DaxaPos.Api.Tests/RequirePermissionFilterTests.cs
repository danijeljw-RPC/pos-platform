using DaxaPos.Api.Authorization;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Api.Tests;

public class RequirePermissionFilterTests
{
    [Fact]
    public async Task InvokeAsync_ReturnsUnauthorized_WhenNoAuthContext()
    {
        var filter = new RequirePermissionFilter(Permissions.OrganisationsManage);
        var context = CreateInvocationContext(authContext: null);

        var result = await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>("next-called"));

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsForbidden_WhenAuthContextLacksPermission()
    {
        var filter = new RequirePermissionFilter(Permissions.OrganisationsManage);
        var authContext = CreateAuthContext(permissions: [Permissions.DevicesManage]);
        var context = CreateInvocationContext(authContext);

        var result = await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>("next-called"));

        var statusCodeResult = Assert.IsType<StatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_CallsNext_WhenAuthContextHasPermission()
    {
        var filter = new RequirePermissionFilter(Permissions.OrganisationsManage);
        var authContext = CreateAuthContext(permissions: [Permissions.OrganisationsManage]);
        var context = CreateInvocationContext(authContext);

        var result = await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>("next-called"));

        Assert.Equal("next-called", result);
    }

    private static AuthContext CreateAuthContext(IReadOnlyCollection<string> permissions) => new(
        TenantId: Guid.NewGuid(),
        OrganisationId: Guid.NewGuid(),
        LocationId: null,
        TerminalId: null,
        UserId: Guid.NewGuid(),
        StaffMemberId: null,
        DeviceId: null,
        AuthMethod: AuthMethod.LocalUsernamePassword,
        Roles: ["OrganisationOwner"],
        Permissions: permissions);

    private static EndpointFilterInvocationContext CreateInvocationContext(AuthContext? authContext)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAuthContextAccessor>(new FakeAuthContextAccessor(authContext));

        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        return EndpointFilterInvocationContext.Create(httpContext);
    }

    private sealed class FakeAuthContextAccessor(AuthContext? current) : IAuthContextAccessor
    {
        public AuthContext? Current { get; } = current;
    }
}
