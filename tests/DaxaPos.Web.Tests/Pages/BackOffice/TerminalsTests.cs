using System.Net;
using System.Net.Http.Json;
using Bunit;
using DaxaPos.Web.Api;
using DaxaPos.Web.Pages.BackOffice;
using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Web.Tests.Pages.BackOffice;

public class TerminalsTests : TestContext
{
    private static BackOfficeSessionState SampleSession() => new(
        "admin-token", DateTimeOffset.UtcNow.AddHours(1), "admin@example.com", ["SystemAdmin"], ["terminals.manage"]);

    private sealed class RoutedBackend
    {
        public Guid LocationId { get; } = Guid.NewGuid();

        public Guid DeviceId { get; } = Guid.NewGuid();

        public List<TerminalResult> Terminals { get; } = [];

        public HttpStatusCode AssignStatusCode { get; set; } = HttpStatusCode.OK;

        public HttpResponseMessage Respond(HttpRequestMessage request)
        {
            var path = request.RequestUri!.AbsolutePath;

            if (request.Method == HttpMethod.Get && path.EndsWith("/locations", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, new[] { new LocationResult(LocationId, Guid.NewGuid(), "Sydney CBD", true) });
            }

            if (request.Method == HttpMethod.Get && path.EndsWith("/devices", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, new[] { new DeviceResult(DeviceId, LocationId, "KioskBrowser", "Front Counter", true) });
            }

            if (request.Method == HttpMethod.Get && path.EndsWith("/terminals", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, Terminals);
            }

            if (request.Method == HttpMethod.Post && path.EndsWith("/terminals", StringComparison.Ordinal))
            {
                var body = request.Content!.ReadFromJsonAsync<CreateTerminalRequest>().GetAwaiter().GetResult()!;
                var created = new TerminalResult(Guid.NewGuid(), body.LocationId, null, body.Name, true);
                Terminals.Add(created);
                return Json(HttpStatusCode.Created, created);
            }

            if (request.Method == HttpMethod.Post && path.EndsWith("/assign-device", StringComparison.Ordinal))
            {
                if (AssignStatusCode != HttpStatusCode.OK)
                {
                    return new HttpResponseMessage(AssignStatusCode);
                }

                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var terminalId = Guid.Parse(segments[3]);
                var body = request.Content!.ReadFromJsonAsync<AssignTerminalDeviceRequest>().GetAwaiter().GetResult()!;
                var index = Terminals.FindIndex(t => t.Id == terminalId);
                var updated = Terminals[index] with { DeviceId = body.DeviceId };
                Terminals[index] = updated;
                return Json(HttpStatusCode.OK, updated);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json<T>(HttpStatusCode statusCode, T body) =>
            new(statusCode) { Content = JsonContent.Create(body) };
    }

    private async Task<RoutedBackend> RegisterAsync(RoutedBackend? backend = null)
    {
        backend ??= new RoutedBackend();
        var stub = new StubHttpMessageHandler { Respond = backend.Respond };
        Services.AddSingleton(new DaxaApiClient(new HttpClient(stub) { BaseAddress = new Uri("http://test/") }));
        var sessionStore = new BackOfficeSessionStore(new InMemoryBrowserStorage());
        await sessionStore.SaveAsync(SampleSession());
        Services.AddSingleton<IBackOfficeSessionStore>(sessionStore);
        return backend;
    }

    [Fact]
    public async Task NoTerminalsYet_ShowsEmptyState()
    {
        await RegisterAsync();

        var cut = RenderComponent<Terminals>();

        cut.WaitForAssertion(() => Assert.Contains("No terminals yet.", cut.Markup));
    }

    [Fact]
    public async Task CreateTerminal_AddsItToTheList_Unassigned()
    {
        var backend = await RegisterAsync();

        var cut = RenderComponent<Terminals>();
        cut.WaitForAssertion(() => Assert.Contains("Sydney CBD", cut.Markup));

        cut.Find("#terminal-name").Input("Front Counter 1");
        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains("Front Counter 1", cut.Markup));
        Assert.Contains("Unassigned", cut.Markup);
        Assert.Single(backend.Terminals);
    }

    [Fact]
    public async Task AssignDevice_Succeeds_AndShowsTheDeviceName()
    {
        var backend = await RegisterAsync();
        backend.Terminals.Add(new TerminalResult(Guid.NewGuid(), backend.LocationId, null, "Bar 1", true));

        var cut = RenderComponent<Terminals>();
        cut.WaitForAssertion(() => Assert.Contains("Bar 1", cut.Markup));
        Assert.Contains("Unassigned", cut.Markup);

        cut.Find("button.btn-outline-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains("Front Counter", cut.Markup));
        Assert.DoesNotContain("Unassigned", cut.Markup);
        Assert.Equal(backend.DeviceId, backend.Terminals[0].DeviceId);
    }

    [Fact]
    public async Task AssignDevice_Conflict_ShowsFriendlyError()
    {
        var backend = await RegisterAsync();
        backend.Terminals.Add(new TerminalResult(Guid.NewGuid(), backend.LocationId, null, "Bar 1", true));
        backend.AssignStatusCode = HttpStatusCode.Conflict;

        var cut = RenderComponent<Terminals>();
        cut.WaitForAssertion(() => Assert.Contains("Bar 1", cut.Markup));

        cut.Find("button.btn-outline-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains("already assigned to a different terminal", cut.Markup));
    }

    [Fact]
    public async Task UnassignDevice_ClearsTheAssignment()
    {
        var backend = await RegisterAsync();
        backend.Terminals.Add(new TerminalResult(Guid.NewGuid(), backend.LocationId, backend.DeviceId, "Bar 1", true));

        var cut = RenderComponent<Terminals>();
        cut.WaitForAssertion(() => Assert.Contains("Bar 1", cut.Markup));
        Assert.Contains("Front Counter", cut.Markup);

        cut.Find("button.btn-outline-secondary").Click();

        cut.WaitForAssertion(() => Assert.Contains("Unassigned", cut.Markup));
        Assert.Null(backend.Terminals[0].DeviceId);
    }
}
