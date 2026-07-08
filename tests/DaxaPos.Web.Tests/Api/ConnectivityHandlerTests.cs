using DaxaPos.Web.Api;
using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;

namespace DaxaPos.Web.Tests.Api;

public class ConnectivityHandlerTests
{
    private static (HttpClient Client, StubHttpMessageHandler Stub, ConnectivityTracker Tracker) BuildClient()
    {
        var stub = new StubHttpMessageHandler();
        var tracker = new ConnectivityTracker();
        var handler = new ConnectivityHandler(tracker) { InnerHandler = stub };
        return (new HttpClient(handler) { BaseAddress = new Uri("http://test/") }, stub, tracker);
    }

    [Fact]
    public async Task SuccessfulResponse_ReportsOnline()
    {
        var (client, _, tracker) = BuildClient();

        await client.GetAsync("api/v1/auth/me");

        Assert.Equal(ConnectivityStatus.Online, tracker.Status);
    }

    [Fact]
    public async Task ErrorStatusCodeResponse_StillReportsOnline()
    {
        // Reaching the server at all — even with a 404/500 — proves connectivity, distinct from a
        // transport-level failure (PLAN-0007 Milestone A).
        var (client, stub, tracker) = BuildClient();
        stub.Respond = _ => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);

        await client.GetAsync("api/v1/auth/me");

        Assert.Equal(ConnectivityStatus.Online, tracker.Status);
    }

    [Fact]
    public async Task NetworkFailure_ReportsToTracker_AndRethrows()
    {
        var (client, stub, tracker) = BuildClient();
        stub.ThrowNetworkFailure = true;

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("api/v1/auth/me"));

        Assert.Equal(ConnectivityStatus.Reconnecting, tracker.Status);
    }
}
