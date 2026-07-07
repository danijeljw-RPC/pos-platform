using System.Net;
using System.Net.Http.Json;
using Bunit;
using DaxaPos.Web.Api;
using DaxaPos.Web.Auth;
using DaxaPos.Web.Pages;
using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Web.Tests.Pages;

public class LoginTests : TestContext
{
    private static DeviceContextStore RegisteredDeviceStore() =>
        new DeviceContextStore(new InMemoryBrowserStorage()).Also(store =>
            store.SaveAsync(new DeviceContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "KioskBrowser", "Front Counter", "device-token")).AsTask().Wait());

    [Fact]
    public void SubmittingValidCredentials_SavesSessionAndNavigatesHome()
    {
        var login = new StaffPinLoginResult("session-token", DateTimeOffset.UtcNow.AddHours(1), Guid.NewGuid(), "Jane Staff", ["StaffPin"], ["orders.manage"]);
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildSuccess(login, out _));
        Services.AddSingleton<IDeviceContextStore>(RegisteredDeviceStore());
        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        Services.AddSingleton<IAuthSessionStore>(sessionStore);
        Services.AddSingleton(new ApiAuthenticationStateProvider(sessionStore));

        var cut = RenderComponent<Login>();
        cut.Find("#staffCode").Change("S001");
        cut.Find("#pin").Change("1234");
        cut.Find("form").Submit();

        Assert.NotNull(sessionStore.Current);
        Assert.Equal("Jane Staff", sessionStore.Current!.DisplayName);
        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.Equal(navigation.BaseUri, navigation.Uri);
    }

    [Fact]
    public void SubmittingValidCredentials_CapturesTerminalId_FromAuthMe()
    {
        var login = new StaffPinLoginResult("session-token", DateTimeOffset.UtcNow.AddHours(1), Guid.NewGuid(), "Jane Staff", ["StaffPin"], ["orders.manage"]);
        var terminalId = Guid.NewGuid();
        var stub = new StubHttpMessageHandler
        {
            Respond = request => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = request.RequestUri!.AbsolutePath.EndsWith("/auth/me", StringComparison.Ordinal)
                    ? JsonContent.Create(new AuthContextResult(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), terminalId, null, Guid.NewGuid(), Guid.NewGuid(), "LocalStaffPin", ["StaffPin"], ["orders.manage"]))
                    : JsonContent.Create(login),
            },
        };
        Services.AddSingleton(new DaxaApiClient(new HttpClient(stub) { BaseAddress = new Uri("http://test/") }));
        Services.AddSingleton<IDeviceContextStore>(RegisteredDeviceStore());
        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        Services.AddSingleton<IAuthSessionStore>(sessionStore);
        Services.AddSingleton(new ApiAuthenticationStateProvider(sessionStore));

        var cut = RenderComponent<Login>();
        cut.Find("#staffCode").Change("S001");
        cut.Find("#pin").Change("1234");
        cut.Find("form").Submit();

        Assert.Equal(terminalId, sessionStore.Current!.TerminalId);
    }

    [Fact]
    public void SubmittingRejectedCredentials_ShowsGenericErrorAndDoesNotSaveSession()
    {
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildFailure(HttpStatusCode.Unauthorized, out _));
        Services.AddSingleton<IDeviceContextStore>(RegisteredDeviceStore());
        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        Services.AddSingleton<IAuthSessionStore>(sessionStore);
        Services.AddSingleton(new ApiAuthenticationStateProvider(sessionStore));

        var cut = RenderComponent<Login>();
        cut.Find("#staffCode").Change("S001");
        cut.Find("#pin").Change("wrong");
        cut.Find("form").Submit();

        Assert.Null(sessionStore.Current);
        Assert.Contains("not accepted", cut.Markup);
    }

    [Fact]
    public void WhenNoDeviceRegistered_PromptsForDeviceSetupInsteadOfShowingForm()
    {
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildFailure(HttpStatusCode.OK, out _));
        Services.AddSingleton<IDeviceContextStore>(new DeviceContextStore(new InMemoryBrowserStorage()));
        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        Services.AddSingleton<IAuthSessionStore>(sessionStore);
        Services.AddSingleton(new ApiAuthenticationStateProvider(sessionStore));

        var cut = RenderComponent<Login>();

        Assert.Contains("No device is registered", cut.Markup);
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find("#staffCode"));
    }
}

file static class TestExtensions
{
    public static T Also<T>(this T value, Action<T> action)
    {
        action(value);
        return value;
    }
}
