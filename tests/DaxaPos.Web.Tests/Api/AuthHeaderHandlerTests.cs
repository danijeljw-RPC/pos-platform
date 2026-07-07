using System.Net.Http.Headers;
using DaxaPos.Web.Api;
using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;

namespace DaxaPos.Web.Tests.Api;

public class AuthHeaderHandlerTests
{
    private static DeviceContext SampleDevice() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "KioskBrowser", "Front Counter", "device-token");

    private static SessionState SampleSession(DateTimeOffset expiresAtUtc) => new(
        "session-token", expiresAtUtc, Guid.NewGuid(), "Jane Staff", ["StaffPin"], ["orders.manage"]);

    private static (HttpClient Client, StubHttpMessageHandler Stub) BuildClient(IAuthSessionStore sessionStore, IDeviceContextStore deviceContextStore)
    {
        var stub = new StubHttpMessageHandler();
        var handler = new AuthHeaderHandler(sessionStore, deviceContextStore) { InnerHandler = stub };
        return (new HttpClient(handler) { BaseAddress = new Uri("http://test/") }, stub);
    }

    [Fact]
    public async Task SendAsync_WithValidSession_AttachesBearerToken()
    {
        var storage = new InMemoryBrowserStorage();
        var sessionStore = new AuthSessionStore(storage);
        await sessionStore.SaveAsync(SampleSession(DateTimeOffset.UtcNow.AddHours(1)));
        var deviceStore = new DeviceContextStore(storage);
        var (client, stub) = BuildClient(sessionStore, deviceStore);

        await client.GetAsync("api/v1/auth/me");

        Assert.Equal("Bearer", stub.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("session-token", stub.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_WithExpiredSessionAndDeviceContext_FallsBackToDeviceToken()
    {
        var storage = new InMemoryBrowserStorage();
        var sessionStore = new AuthSessionStore(storage);
        await sessionStore.SaveAsync(SampleSession(DateTimeOffset.UtcNow.AddHours(-1)));
        var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
        await deviceStore.SaveAsync(SampleDevice());
        var (client, stub) = BuildClient(sessionStore, deviceStore);

        await client.GetAsync("api/v1/auth/staff-pin/login");

        Assert.Equal("Device", stub.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("device-token", stub.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_WithNoSessionOrDevice_AttachesNoAuthorizationHeader()
    {
        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
        var (client, stub) = BuildClient(sessionStore, deviceStore);

        await client.GetAsync("api/v1/device-registration");

        Assert.Null(stub.LastRequest!.Headers.Authorization);
    }

    /// <summary>
    /// PLAN-0006 Milestone B regression guard: a Back Office call sets its own explicit bearer
    /// token (see <c>DaxaApiClient.PostAuthorizedAsync</c>/<c>GetAuthorizedAsync</c>). Even when a
    /// live Terminal staff session also exists in the same browser, this handler must not overwrite
    /// the explicit Back Office token with the Terminal's staff session token — the two sessions
    /// (ADR-0015 §2) must never collide on the shared HttpClient.
    /// </summary>
    [Fact]
    public async Task SendAsync_WithExplicitAuthorizationHeaderAndLiveStaffSession_DoesNotOverwriteExplicitHeader()
    {
        var storage = new InMemoryBrowserStorage();
        var sessionStore = new AuthSessionStore(storage);
        await sessionStore.SaveAsync(SampleSession(DateTimeOffset.UtcNow.AddHours(1)));
        var deviceStore = new DeviceContextStore(storage);
        var (client, stub) = BuildClient(sessionStore, deviceStore);

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/locations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "backoffice-token");

        await client.SendAsync(request);

        Assert.Equal("Bearer", stub.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("backoffice-token", stub.LastRequest.Headers.Authorization.Parameter);
    }
}
