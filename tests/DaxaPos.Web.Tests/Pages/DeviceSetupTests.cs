using System.Net;
using Bunit;
using DaxaPos.Web.Api;
using DaxaPos.Web.Pages;
using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Web.Tests.Pages;

public class DeviceSetupTests : TestContext
{
    [Fact]
    public void SubmittingValidPin_SavesDeviceContextAndNavigatesToLogin()
    {
        var registration = new DeviceRegistrationResult(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "KioskBrowser", "Front Counter", "credential-id.secret");
        var apiClient = FakeDaxaApiClientHandler.BuildSuccess(registration, out _);
        Services.AddSingleton(apiClient);
        var deviceContextStore = new DeviceContextStore(new InMemoryBrowserStorage());
        Services.AddSingleton<IDeviceContextStore>(deviceContextStore);

        var cut = RenderComponent<DeviceSetup>();
        cut.Find("#pin").Change("123456");
        cut.Find("form").Submit();

        Assert.NotNull(deviceContextStore.Current);
        Assert.Equal("credential-id.secret", deviceContextStore.Current!.DeviceToken);
        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/login", navigation.Uri);
    }

    [Fact]
    public void SubmittingRejectedPin_ShowsGenericErrorAndDoesNotSaveDeviceContext()
    {
        var apiClient = FakeDaxaApiClientHandler.BuildFailure(HttpStatusCode.Unauthorized, out _);
        Services.AddSingleton(apiClient);
        var deviceContextStore = new DeviceContextStore(new InMemoryBrowserStorage());
        Services.AddSingleton<IDeviceContextStore>(deviceContextStore);

        var cut = RenderComponent<DeviceSetup>();
        cut.Find("#pin").Change("000000");
        cut.Find("form").Submit();

        Assert.Null(deviceContextStore.Current);
        Assert.Contains("not accepted", cut.Markup);
    }

    [Fact]
    public async Task WhenDeviceAlreadyRegistered_ShowsExistingDeviceInsteadOfForm()
    {
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildFailure(HttpStatusCode.OK, out _));
        var deviceContextStore = new DeviceContextStore(new InMemoryBrowserStorage());
        await deviceContextStore.SaveAsync(new DeviceContext(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "KioskBrowser", "Front Counter", "token"));
        Services.AddSingleton<IDeviceContextStore>(deviceContextStore);

        var cut = RenderComponent<DeviceSetup>();

        Assert.Contains("already registered", cut.Markup);
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find("#pin"));
    }
}
