using DaxaPos.Application.Identity;
using DaxaPos.Domain.Enums;
using DaxaPos.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;

namespace DaxaPos.UnitTests.Identity;

public class AuthContextAccessorTests
{
    private static readonly AuthContext SampleContext = new(
        TenantId: Guid.NewGuid(),
        OrganisationId: Guid.NewGuid(),
        LocationId: Guid.NewGuid(),
        TerminalId: null,
        UserId: Guid.NewGuid(),
        StaffMemberId: null,
        DeviceId: null,
        AuthMethod: AuthMethod.LocalUsernamePassword,
        Roles: ["OrganisationOwner"],
        Permissions: [Permissions.OrganisationsManage]);

    [Fact]
    public void Current_IsNull_WhenNoHttpContext()
    {
        var httpContextAccessor = new HttpContextAccessor { HttpContext = null };
        var authContextAccessor = new HttpContextAuthContextAccessor(httpContextAccessor);

        Assert.Null(authContextAccessor.Current);
    }

    [Fact]
    public void Current_IsNull_WhenHttpContextHasNoStashedAuthContext()
    {
        var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var authContextAccessor = new HttpContextAuthContextAccessor(httpContextAccessor);

        Assert.Null(authContextAccessor.Current);
    }

    [Fact]
    public void Current_ReturnsStashedAuthContext_WhenPresent()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextAuthContextAccessor.AuthContextItemKey] = SampleContext;
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var authContextAccessor = new HttpContextAuthContextAccessor(httpContextAccessor);

        Assert.Equal(SampleContext, authContextAccessor.Current);
    }
}

public class CurrentTenantProviderTests
{
    [Fact]
    public void TenantId_IsNull_WhenNoAuthContext()
    {
        var authContextAccessor = new FakeAuthContextAccessor(null);
        var tenantProvider = new CurrentTenantProvider(authContextAccessor);

        Assert.Null(tenantProvider.TenantId);
    }

    [Fact]
    public void TenantId_MatchesAuthContext_WhenPresent()
    {
        var context = SampleContext with { TenantId = Guid.NewGuid() };
        var authContextAccessor = new FakeAuthContextAccessor(context);
        var tenantProvider = new CurrentTenantProvider(authContextAccessor);

        Assert.Equal(context.TenantId, tenantProvider.TenantId);
    }

    private static readonly AuthContext SampleContext = new(
        TenantId: Guid.NewGuid(),
        OrganisationId: null,
        LocationId: null,
        TerminalId: null,
        UserId: null,
        StaffMemberId: null,
        DeviceId: null,
        AuthMethod: AuthMethod.LocalStaffPin,
        Roles: [],
        Permissions: []);

    private sealed class FakeAuthContextAccessor(AuthContext? current) : IAuthContextAccessor
    {
        public AuthContext? Current { get; } = current;
    }
}
