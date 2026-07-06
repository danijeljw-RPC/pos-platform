using Bunit;
using DaxaPos.Web.Api;
using DaxaPos.Web.Auth;
using DaxaPos.Web.Layout;
using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Web.Tests.Layout;

public class MainLayoutTests : TestContext
{
    private void RegisterCommonServices(IDeviceContextStore deviceStore, IAuthSessionStore sessionStore)
    {
        Services.AddSingleton(deviceStore);
        Services.AddSingleton(sessionStore);
        Services.AddSingleton(new ApiAuthenticationStateProvider(sessionStore));
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildFailure(System.Net.HttpStatusCode.OK, out _));
    }

    [Fact]
    public void NoDeviceContext_OnProtectedRoute_RedirectsToDeviceSetup()
    {
        var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        RegisterCommonServices(deviceStore, sessionStore);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/some-protected-page");

        RenderComponent<MainLayout>(parameters => parameters.Add(p => p.Body, (RenderFragment)(builder => { })));

        Assert.EndsWith("/device-setup", navigation.Uri);
    }

    [Fact]
    public async Task DeviceContextButNoSession_OnProtectedRoute_RedirectsToLogin()
    {
        var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
        await deviceStore.SaveAsync(new DeviceContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "KioskBrowser", "Front Counter", "token"));
        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        RegisterCommonServices(deviceStore, sessionStore);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/some-protected-page");

        RenderComponent<MainLayout>(parameters => parameters.Add(p => p.Body, (RenderFragment)(builder => { })));

        Assert.EndsWith("/login", navigation.Uri);
    }

    [Fact]
    public async Task DeviceContextAndValidSession_OnProtectedRoute_DoesNotRedirect()
    {
        var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
        await deviceStore.SaveAsync(new DeviceContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "KioskBrowser", "Front Counter", "token"));
        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        await sessionStore.SaveAsync(new SessionState("session-token", DateTimeOffset.UtcNow.AddHours(1), Guid.NewGuid(), "Jane Staff", ["StaffPin"], ["orders.manage"]));
        RegisterCommonServices(deviceStore, sessionStore);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/some-protected-page");
        var originalUri = navigation.Uri;

        RenderComponent<MainLayout>(parameters => parameters.Add(p => p.Body, (RenderFragment)(builder => { })));

        Assert.Equal(originalUri, navigation.Uri);
    }

    [Fact]
    public void NoDeviceContext_OnDeviceSetupRoute_DoesNotRedirect()
    {
        var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        RegisterCommonServices(deviceStore, sessionStore);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/device-setup");

        RenderComponent<MainLayout>(parameters => parameters.Add(p => p.Body, (RenderFragment)(builder => { })));

        Assert.EndsWith("/device-setup", navigation.Uri);
    }
}
