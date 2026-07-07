using System.Net;
using System.Net.Http.Json;
using DaxaPos.Web.Api;
using DaxaPos.Web.Tests.Fakes;

namespace DaxaPos.Web.Tests.Api;

public class DaxaApiClientTests
{
    private static (DaxaApiClient Client, StubHttpMessageHandler Stub) BuildClient()
    {
        var stub = new StubHttpMessageHandler();
        var httpClient = new HttpClient(stub) { BaseAddress = new Uri("http://test/") };
        return (new DaxaApiClient(httpClient), stub);
    }

    [Fact]
    public async Task StaffPinLoginAsync_OnSuccess_ReturnsSuccessWithValue()
    {
        var (client, stub) = BuildClient();
        var staffMemberId = Guid.NewGuid();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new StaffPinLoginResult(
                "session-token", DateTimeOffset.UtcNow.AddHours(1), staffMemberId, "Jane Staff", ["StaffPin"], ["orders.manage"])),
        };

        var result = await client.StaffPinLoginAsync(new StaffPinLoginRequest(Guid.NewGuid(), "S001", "1234"));

        Assert.Equal(ApiResultKind.Success, result.Kind);
        Assert.Equal("session-token", result.Value!.SessionToken);
        Assert.Equal(staffMemberId, result.Value.StaffMemberId);
    }

    [Fact]
    public async Task StaffPinLoginAsync_OnUnauthorized_ReturnsUnauthorizedKind()
    {
        var (client, stub) = BuildClient();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized);

        var result = await client.StaffPinLoginAsync(new StaffPinLoginRequest(Guid.NewGuid(), "S001", "wrong-pin"));

        Assert.Equal(ApiResultKind.Unauthorized, result.Kind);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task RegisterDeviceAsync_OnForbidden_ReturnsForbiddenKind()
    {
        var (client, stub) = BuildClient();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.Forbidden);

        var result = await client.RegisterDeviceAsync(new DeviceRegistrationRequest("000000", "KioskBrowser", null));

        Assert.Equal(ApiResultKind.Forbidden, result.Kind);
    }

    [Fact]
    public async Task RegisterDeviceAsync_OnNetworkFailure_ReturnsFailedKind()
    {
        var (client, stub) = BuildClient();
        stub.ThrowNetworkFailure = true;

        var result = await client.RegisterDeviceAsync(new DeviceRegistrationRequest("000000", "KioskBrowser", null));

        Assert.Equal(ApiResultKind.Failed, result.Kind);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task LogoutAsync_OnSuccess_ReturnsSuccessKind()
    {
        var (client, stub) = BuildClient();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK);

        var result = await client.LogoutAsync();

        Assert.Equal(ApiResultKind.Success, result.Kind);
    }

    [Fact]
    public async Task GetResolvedMenuAsync_OnSuccess_ReturnsSectionsAndAppendsLocationIdQuery()
    {
        var (client, stub) = BuildClient();
        var locationId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ResolvedMenuResult(locationId,
            [
                new ResolvedMenuSectionResult(Guid.NewGuid(), Guid.NewGuid(), "Coffee", 0,
                [
                    new ResolvedMenuItemResult(productId, "Flat White", 0, 5.5m, true, "AU_GST_10", []),
                ]),
            ])),
        };

        var result = await client.GetResolvedMenuAsync(locationId);

        Assert.Equal(ApiResultKind.Success, result.Kind);
        Assert.Equal(locationId, result.Value!.LocationId);
        Assert.Single(result.Value.Sections);
        Assert.Equal("Flat White", result.Value.Sections[0].Items[0].ProductName);
        Assert.Contains($"locationId={locationId}", stub.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task GetResolvedMenuAsync_OnNotFound_ReturnsFailedKind()
    {
        var (client, stub) = BuildClient();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        var result = await client.GetResolvedMenuAsync(Guid.NewGuid());

        Assert.Equal(ApiResultKind.Failed, result.Kind);
    }
}
