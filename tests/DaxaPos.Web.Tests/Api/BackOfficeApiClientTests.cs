using System.Net;
using System.Net.Http.Json;
using DaxaPos.Web.Api;
using DaxaPos.Web.Tests.Fakes;

namespace DaxaPos.Web.Tests.Api;

public class BackOfficeApiClientTests
{
    private static (DaxaApiClient Client, StubHttpMessageHandler Stub) BuildClient()
    {
        var stub = new StubHttpMessageHandler();
        var httpClient = new HttpClient(stub) { BaseAddress = new Uri("http://test/") };
        return (new DaxaApiClient(httpClient), stub);
    }

    [Fact]
    public async Task LocalLoginAsync_OnSuccess_ReturnsSuccessWithValue()
    {
        var (client, stub) = BuildClient();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new LocalLoginResult(
                "admin-token", DateTimeOffset.UtcNow.AddHours(8), ["SystemAdmin"], ["devices.register"])),
        };

        var result = await client.LocalLoginAsync(new LocalLoginRequest("admin@example.com", "correct-horse"));

        Assert.Equal(ApiResultKind.Success, result.Kind);
        Assert.Equal("admin-token", result.Value!.SessionToken);
    }

    [Fact]
    public async Task LocalLoginAsync_OnUnauthorized_ReturnsUnauthorizedKind()
    {
        var (client, stub) = BuildClient();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized);

        var result = await client.LocalLoginAsync(new LocalLoginRequest("admin@example.com", "wrong"));

        Assert.Equal(ApiResultKind.Unauthorized, result.Kind);
    }

    [Fact]
    public async Task CreateDeviceRegistrationPinAsync_AttachesExplicitBearerTokenAndBody()
    {
        var (client, stub) = BuildClient();
        var locationId = Guid.NewGuid();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new DeviceRegistrationPinCreatedResult(
                Guid.NewGuid(), locationId, "123456", DateTimeOffset.UtcNow.AddMinutes(15), 1)),
        };

        var result = await client.CreateDeviceRegistrationPinAsync(
            "admin-token", new CreateDeviceRegistrationPinRequest(locationId, MaxUses: null));

        Assert.Equal(ApiResultKind.Success, result.Kind);
        Assert.Equal("123456", result.Value!.Pin);
        Assert.Equal("Bearer", stub.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("admin-token", stub.LastRequest.Headers.Authorization.Parameter);
        Assert.Equal(HttpMethod.Post, stub.LastRequest.Method);
    }

    [Fact]
    public async Task RevokeDeviceRegistrationPinAsync_SendsNoBodyWithExplicitBearer()
    {
        var (client, stub) = BuildClient();
        var pinId = Guid.NewGuid();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new DeviceRegistrationPinResult(
                pinId, Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(15), 1, 0, DateTimeOffset.UtcNow)),
        };

        var result = await client.RevokeDeviceRegistrationPinAsync("admin-token", pinId);

        Assert.Equal(ApiResultKind.Success, result.Kind);
        Assert.NotNull(result.Value!.RevokedAtUtc);
        Assert.Equal("admin-token", stub.LastRequest!.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task ListDevicesAsync_WithLocationId_AppendsQueryString()
    {
        var (client, stub) = BuildClient();
        var locationId = Guid.NewGuid();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<DeviceResult>()),
        };

        await client.ListDevicesAsync("admin-token", locationId);

        Assert.Contains($"locationId={locationId}", stub.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task ListLocationsAsync_OnForbidden_ReturnsForbiddenKind()
    {
        var (client, stub) = BuildClient();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.Forbidden);

        var result = await client.ListLocationsAsync("admin-token");

        Assert.Equal(ApiResultKind.Forbidden, result.Kind);
    }

    [Fact]
    public async Task ListProductsAsync_OnSuccess_ReturnsValues()
    {
        var (client, stub) = BuildClient();
        var categoryId = Guid.NewGuid();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new[] { new ProductResult(Guid.NewGuid(), categoryId, "Flat White", "SKU1", 5.5m, true, false) }),
        };

        var result = await client.ListProductsAsync("admin-token");

        Assert.Equal(ApiResultKind.Success, result.Kind);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task ListMenusAsync_OnSuccess_ReturnsValues()
    {
        var (client, stub) = BuildClient();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new[] { new MenuResult(Guid.NewGuid(), null, "Main Menu", true) }),
        };

        var result = await client.ListMenusAsync("admin-token");

        Assert.Equal(ApiResultKind.Success, result.Kind);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task LogoutBackOfficeAsync_OnSuccess_ReturnsSuccessKind()
    {
        var (client, stub) = BuildClient();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK);

        var result = await client.LogoutBackOfficeAsync("admin-token");

        Assert.Equal(ApiResultKind.Success, result.Kind);
        Assert.Equal("admin-token", stub.LastRequest!.Headers.Authorization!.Parameter);
    }
}
