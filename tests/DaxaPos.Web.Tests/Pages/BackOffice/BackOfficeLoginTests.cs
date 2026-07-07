using System.Net;
using Bunit;
using DaxaPos.Web.Api;
using DaxaPos.Web.Pages.BackOffice;
using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Web.Tests.Pages.BackOffice;

public class BackOfficeLoginTests : TestContext
{
    [Fact]
    public void SubmittingValidCredentials_SavesSessionAndNavigatesToBackOfficeHome()
    {
        var login = new LocalLoginResult("admin-token", DateTimeOffset.UtcNow.AddHours(8), ["SystemAdmin"], ["devices.register"]);
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildSuccess(login, out _));
        var sessionStore = new BackOfficeSessionStore(new InMemoryBrowserStorage());
        Services.AddSingleton<IBackOfficeSessionStore>(sessionStore);

        var cut = RenderComponent<BackOfficeLogin>();
        cut.Find("#email").Change("admin@example.com");
        cut.Find("#password").Change("correct-horse");
        cut.Find("form").Submit();

        Assert.NotNull(sessionStore.Current);
        Assert.Equal("admin@example.com", sessionStore.Current!.Email);
        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/back-office", navigation.Uri);
    }

    [Fact]
    public void SubmittingRejectedCredentials_ShowsGenericErrorAndDoesNotSaveSession()
    {
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildFailure(HttpStatusCode.Unauthorized, out _));
        var sessionStore = new BackOfficeSessionStore(new InMemoryBrowserStorage());
        Services.AddSingleton<IBackOfficeSessionStore>(sessionStore);

        var cut = RenderComponent<BackOfficeLogin>();
        cut.Find("#email").Change("admin@example.com");
        cut.Find("#password").Change("wrong");
        cut.Find("form").Submit();

        Assert.Null(sessionStore.Current);
        Assert.Contains("not accepted", cut.Markup);
    }
}
