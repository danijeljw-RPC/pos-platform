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
    public async Task RegisterDeviceAsync_OnNetworkFailure_ReturnsNetworkFailureKind()
    {
        var (client, stub) = BuildClient();
        stub.ThrowNetworkFailure = true;

        var result = await client.RegisterDeviceAsync(new DeviceRegistrationRequest("000000", "KioskBrowser", null));

        Assert.Equal(ApiResultKind.NetworkFailure, result.Kind);
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

    [Fact]
    public async Task ListOrdersAsync_OnSuccess_ReturnsOrdersAndAppendsLocationIdQuery()
    {
        var (client, stub) = BuildClient();
        var locationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create<IReadOnlyList<OrderResult>>(
            [
                new OrderResult(orderId, terminalId, OrderStatusResult.Open, 10m, 1m, 11m, [], 42, DateTimeOffset.UtcNow),
            ]),
        };

        var result = await client.ListOrdersAsync(locationId);

        Assert.Equal(ApiResultKind.Success, result.Kind);
        Assert.Single(result.Value!);
        Assert.Equal(orderId, result.Value![0].Id);
        Assert.Contains($"locationId={locationId}", stub.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task ListOrdersAsync_OnForbidden_ReturnsForbiddenKind()
    {
        var (client, stub) = BuildClient();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.Forbidden);

        var result = await client.ListOrdersAsync(Guid.NewGuid());

        Assert.Equal(ApiResultKind.Forbidden, result.Kind);
    }

    [Fact]
    public async Task RecordPaymentAsync_OnSuccess_PostsToOrderPaymentsAndReturnsPayment()
    {
        var (client, stub) = BuildClient();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new PaymentResult(
                paymentId, orderId, Guid.NewGuid(), PaymentMethodResult.Cash, PaymentStatusResult.Recorded,
                11.00m, 11.00m, idempotencyKey, null, Guid.NewGuid(), DateTimeOffset.UtcNow, null)),
        };

        var result = await client.RecordPaymentAsync(orderId, new RecordPaymentRequest(PaymentMethodResult.Cash, 11.00m, idempotencyKey));

        Assert.Equal(ApiResultKind.Success, result.Kind);
        Assert.Equal(paymentId, result.Value!.Id);
        Assert.Equal(PaymentStatusResult.Recorded, result.Value.Status);
        Assert.Equal($"http://test/api/v1/orders/{orderId}/payments", stub.LastRequest!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, stub.LastRequest.Method);
    }

    [Fact]
    public async Task RecordPaymentAsync_OnOverpaymentRejection_ReturnsFailedKind()
    {
        var (client, stub) = BuildClient();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.BadRequest);

        var result = await client.RecordPaymentAsync(Guid.NewGuid(), new RecordPaymentRequest(PaymentMethodResult.Cash, 999m, Guid.NewGuid()));

        Assert.Equal(ApiResultKind.Failed, result.Kind);
    }

    [Fact]
    public async Task GetPaymentsAsync_OnSuccess_ReturnsPaymentList()
    {
        var (client, stub) = BuildClient();
        var orderId = Guid.NewGuid();
        var payment = new PaymentResult(
            Guid.NewGuid(), orderId, Guid.NewGuid(), PaymentMethodResult.ManualEftpos, PaymentStatusResult.Recorded,
            8.00m, 8.00m, Guid.NewGuid(), null, Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create<IReadOnlyList<PaymentResult>>([payment]),
        };

        var result = await client.GetPaymentsAsync(orderId);

        Assert.Equal(ApiResultKind.Success, result.Kind);
        Assert.Single(result.Value!);
        Assert.Equal($"http://test/api/v1/orders/{orderId}/payments", stub.LastRequest!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Get, stub.LastRequest.Method);
    }

    [Fact]
    public async Task GetReceiptAsync_OnSuccess_ReturnsReceipt()
    {
        var (client, stub) = BuildClient();
        var orderId = Guid.NewGuid();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ReceiptResult(
                orderId, 42, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                [new ReceiptLineResult("Flat White", 1, 5.50m, null)],
                5.50m, "Total", 5.50m, [], "Includes GST", 0.50m, [], [], [])),
        };

        var result = await client.GetReceiptAsync(orderId);

        Assert.Equal(ApiResultKind.Success, result.Kind);
        Assert.Equal(orderId, result.Value!.OrderId);
        Assert.Single(result.Value.Lines);
        Assert.Equal($"http://test/api/v1/orders/{orderId}/receipt", stub.LastRequest!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Get, stub.LastRequest.Method);
    }

    [Fact]
    public async Task GetReceiptAsync_OnNotFound_ReturnsFailedKind()
    {
        var (client, stub) = BuildClient();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        var result = await client.GetReceiptAsync(Guid.NewGuid());

        Assert.Equal(ApiResultKind.Failed, result.Kind);
    }

    [Fact]
    public async Task ReprintReceiptAsync_OnSuccess_PostsWithNoBodyAndReturnsReceipt()
    {
        var (client, stub) = BuildClient();
        var orderId = Guid.NewGuid();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ReceiptResult(
                orderId, 42, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                [], 0m, "Total", 0m, [], "Includes GST", 0m, [], [], [])),
        };

        var result = await client.ReprintReceiptAsync(orderId);

        Assert.Equal(ApiResultKind.Success, result.Kind);
        Assert.Equal($"http://test/api/v1/orders/{orderId}/receipt/reprint", stub.LastRequest!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, stub.LastRequest.Method);
        Assert.Null(stub.LastRequest.Content);
    }

    [Fact]
    public async Task ReprintReceiptAsync_OnForbidden_ReturnsForbiddenKind()
    {
        var (client, stub) = BuildClient();
        stub.Respond = _ => new HttpResponseMessage(HttpStatusCode.Forbidden);

        var result = await client.ReprintReceiptAsync(Guid.NewGuid());

        Assert.Equal(ApiResultKind.Forbidden, result.Kind);
    }
}
