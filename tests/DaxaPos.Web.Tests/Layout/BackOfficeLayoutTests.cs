using Bunit;
using DaxaPos.Web.Api;
using DaxaPos.Web.Layout;
using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Web.Tests.Layout;

public class BackOfficeLayoutTests : TestContext
{
    private static BackOfficeSessionState SampleSession(DateTimeOffset expiresAtUtc) => new(
        "admin-token", expiresAtUtc, "admin@example.com", ["SystemAdmin"], ["devices.register"]);

    private void RegisterCommonServices(IBackOfficeSessionStore sessionStore)
    {
        Services.AddSingleton(sessionStore);
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildFailure(System.Net.HttpStatusCode.OK, out _));
    }

    [Fact]
    public void NoSession_OnProtectedRoute_RedirectsToBackOfficeLogin()
    {
        var sessionStore = new BackOfficeSessionStore(new InMemoryBrowserStorage());
        RegisterCommonServices(sessionStore);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/back-office/devices");

        RenderComponent<BackOfficeLayout>(parameters => parameters.Add(p => p.Body, (RenderFragment)(builder => { })));

        Assert.EndsWith("/back-office/login", navigation.Uri);
    }

    [Fact]
    public async Task ExpiredSession_OnProtectedRoute_RedirectsToBackOfficeLogin()
    {
        var sessionStore = new BackOfficeSessionStore(new InMemoryBrowserStorage());
        await sessionStore.SaveAsync(SampleSession(DateTimeOffset.UtcNow.AddHours(-1)));
        RegisterCommonServices(sessionStore);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/back-office/devices");

        RenderComponent<BackOfficeLayout>(parameters => parameters.Add(p => p.Body, (RenderFragment)(builder => { })));

        Assert.EndsWith("/back-office/login", navigation.Uri);
    }

    [Fact]
    public async Task ValidSession_OnProtectedRoute_DoesNotRedirect()
    {
        var sessionStore = new BackOfficeSessionStore(new InMemoryBrowserStorage());
        await sessionStore.SaveAsync(SampleSession(DateTimeOffset.UtcNow.AddHours(1)));
        RegisterCommonServices(sessionStore);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/back-office/devices");
        var originalUri = navigation.Uri;

        RenderComponent<BackOfficeLayout>(parameters => parameters.Add(p => p.Body, (RenderFragment)(builder => { })));

        Assert.Equal(originalUri, navigation.Uri);
    }

    [Fact]
    public void NoSession_OnLoginRoute_DoesNotRedirect()
    {
        var sessionStore = new BackOfficeSessionStore(new InMemoryBrowserStorage());
        RegisterCommonServices(sessionStore);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/back-office/login");

        RenderComponent<BackOfficeLayout>(parameters => parameters.Add(p => p.Body, (RenderFragment)(builder => { })));

        Assert.EndsWith("/back-office/login", navigation.Uri);
    }
}
