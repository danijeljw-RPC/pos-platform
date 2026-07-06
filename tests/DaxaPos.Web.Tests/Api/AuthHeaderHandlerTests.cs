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
}
