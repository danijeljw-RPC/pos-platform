using DaxaPos.Web.Auth;
using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;

namespace DaxaPos.Web.Tests.Auth;

public class ApiAuthenticationStateProviderTests
{
    [Fact]
    public async Task GetAuthenticationStateAsync_WithNoSession_IsAnonymous()
    {
        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        var provider = new ApiAuthenticationStateProvider(sessionStore);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WithValidSession_IsAuthenticatedWithClaims()
    {
        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        await sessionStore.SaveAsync(new SessionState(
            "session-token", DateTimeOffset.UtcNow.AddHours(1), Guid.NewGuid(), "Jane Staff", ["StaffPin"], ["orders.manage"]));
        var provider = new ApiAuthenticationStateProvider(sessionStore);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity!.IsAuthenticated);
        Assert.Equal("Jane Staff", state.User.Identity.Name);
        Assert.Contains(state.User.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "StaffPin");
        Assert.Contains(state.User.Claims, c => c.Type == "daxa:permission" && c.Value == "orders.manage");
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WithExpiredSession_IsAnonymous()
    {
        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        await sessionStore.SaveAsync(new SessionState(
            "session-token", DateTimeOffset.UtcNow.AddHours(-1), Guid.NewGuid(), "Jane Staff", ["StaffPin"], ["orders.manage"]));
        var provider = new ApiAuthenticationStateProvider(sessionStore);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
    }
}
