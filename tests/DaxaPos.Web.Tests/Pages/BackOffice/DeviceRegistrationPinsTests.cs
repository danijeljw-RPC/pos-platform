using System.Net;
using System.Net.Http.Json;
using Bunit;
using DaxaPos.Web.Api;
using DaxaPos.Web.Pages.BackOffice;
using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Web.Tests.Pages.BackOffice;

public class DeviceRegistrationPinsTests : TestContext
{
    private static BackOfficeSessionState SampleSession() => new(
        "admin-token", DateTimeOffset.UtcNow.AddHours(1), "admin@example.com", ["SystemAdmin"], ["devices.register"]);

    private static (DaxaApiClient Client, StubHttpMessageHandler Stub) BuildRoutedClient(Guid locationId, Guid pinId)
    {
        var stub = new StubHttpMessageHandler
        {
            Respond = request =>
            {
                if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath.EndsWith("/locations"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(new[] { new LocationResult(locationId, Guid.NewGuid(), "Sydney CBD", true) }),
                    };
                }

                if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.EndsWith("/revoke"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(new DeviceRegistrationPinResult(
                            pinId, locationId, DateTimeOffset.UtcNow.AddMinutes(15), 1, 0, DateTimeOffset.UtcNow)),
                    };
                }

                if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.EndsWith("/device-registration-pins"))
                {
                    return new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = JsonContent.Create(new DeviceRegistrationPinCreatedResult(
                            pinId, locationId, "654321", DateTimeOffset.UtcNow.AddMinutes(15), 1)),
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            },
        };

        return (new DaxaApiClient(new HttpClient(stub) { BaseAddress = new Uri("http://test/") }), stub);
    }

    [Fact]
    public async Task GeneratePin_ShowsRawPinOnceAndOffersRevoke()
    {
        var locationId = Guid.NewGuid();
        var pinId = Guid.NewGuid();
        var (apiClient, _) = BuildRoutedClient(locationId, pinId);
        Services.AddSingleton(apiClient);
        var sessionStore = new BackOfficeSessionStore(new InMemoryBrowserStorage());
        await sessionStore.SaveAsync(SampleSession());
        Services.AddSingleton<IBackOfficeSessionStore>(sessionStore);

        var cut = RenderComponent<DeviceRegistrationPins>();
        cut.WaitForAssertion(() => Assert.Contains("Sydney CBD", cut.Markup));

        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains("654321", cut.Markup));
        Assert.Contains("Revoke this PIN", cut.Markup);
    }

    [Fact]
    public async Task RevokePin_ShowsRevokedState()
    {
        var locationId = Guid.NewGuid();
        var pinId = Guid.NewGuid();
        var (apiClient, _) = BuildRoutedClient(locationId, pinId);
        Services.AddSingleton(apiClient);
        var sessionStore = new BackOfficeSessionStore(new InMemoryBrowserStorage());
        await sessionStore.SaveAsync(SampleSession());
        Services.AddSingleton<IBackOfficeSessionStore>(sessionStore);

        var cut = RenderComponent<DeviceRegistrationPins>();
        cut.WaitForAssertion(() => Assert.Contains("Sydney CBD", cut.Markup));
        cut.Find("button.btn-primary").Click();
        cut.WaitForAssertion(() => Assert.Contains("654321", cut.Markup));

        cut.Find("button.btn-outline-danger").Click();

        cut.WaitForAssertion(() => Assert.Contains("Revoked at", cut.Markup));
    }
}
