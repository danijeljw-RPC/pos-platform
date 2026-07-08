using System.Net;
using System.Net.Http.Json;
using Bunit;
using DaxaPos.Web.Api;
using DaxaPos.Web.Pages;
using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Web.Tests.Pages;

/// <summary>
/// Milestone D: <see cref="Pay"/> is the payment-entry/receipt page reached from
/// <see cref="Sales"/>'s "Pay" button. Drives <see cref="FakeOrderBackend"/>'s payment/receipt
/// simulation (added this milestone) rather than a live backend.
/// </summary>
public class PayTests : TestContext
{
    private static readonly Guid DeviceId = Guid.NewGuid();
    private static readonly Guid LocationId = Guid.NewGuid();
    private static readonly Guid TerminalId = Guid.NewGuid();

    private static DeviceContext SampleDevice() => new(
        DeviceId, Guid.NewGuid(), Guid.NewGuid(), LocationId, "KioskBrowser", "Front Counter", "device-token");

    private static OrderLineResult SampleLine(decimal amount) =>
        new(Guid.NewGuid(), Guid.NewGuid(), 1, "Flat White", amount, amount, null, OrderLineStatusResult.Active, []);

    private DraftOrderStore RegisterDraftStore() => new(new InMemoryBrowserStorage());

    private StubHttpMessageHandler RegisterServices(FakeOrderBackend backend, DraftOrderStore? draftStore = null)
    {
        var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
        deviceStore.SaveAsync(SampleDevice()).AsTask().Wait();
        Services.AddSingleton<IDeviceContextStore>(deviceStore);
        Services.AddSingleton<IDraftOrderStore>(draftStore ?? RegisterDraftStore());

        var stub = new StubHttpMessageHandler { Respond = backend.Respond };
        Services.AddSingleton(new DaxaApiClient(new HttpClient(stub) { BaseAddress = new Uri("http://test/") }));
        return stub;
    }

    private IRenderedComponent<Pay> RenderPay(Guid orderId) =>
        RenderComponent<Pay>(parameters => parameters.Add(p => p.OrderId, orderId));

    [Fact]
    public void OpenOrderWithNoPayments_ShowsFullBalanceDue_AndPrefillsCashAmount()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 10.00m, 1.00m, 11.00m, [SampleLine(11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        RegisterServices(backend);

        var cut = RenderPay(order.Id);

        cut.WaitForAssertion(() => Assert.Contains("$11.00", cut.Markup));
        Assert.Equal("11.00", cut.Find("#cash-amount").GetAttribute("value"));
    }

    [Fact]
    public async Task RecordingCashPaymentForFullAmount_ReachesReceiptState_AndClearsTheDraft()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 10.00m, 1.00m, 11.00m, [SampleLine(11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var draftStore = RegisterDraftStore();
        await draftStore.SaveOrderIdAsync(DeviceId, order.Id);
        RegisterServices(backend, draftStore);

        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.Contains("$11.00", cut.Markup));

        cut.Find("#record-cash").Click();

        cut.WaitForAssertion(() => Assert.Contains("Receipt", cut.Markup));
        Assert.Equal(OrderStatusResult.Completed, backend.Order!.Status);
        Assert.Null(await draftStore.GetOrderIdAsync(DeviceId));
    }

    [Fact]
    public void RecordingPartialCashPayment_StaysInPayingState_WithUpdatedBalanceAndHistoryRow()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 20.00m, 0m, 20.00m, [SampleLine(20.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        RegisterServices(backend);

        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.Contains("$20.00", cut.Markup));

        cut.Find("#cash-amount").Change("12.00");
        cut.Find("#record-cash").Click();

        cut.WaitForAssertion(() => Assert.Contains("$8.00", cut.Markup));
        Assert.Equal(OrderStatusResult.Open, backend.Order!.Status);
        Assert.Contains("$12.00", cut.Markup);
    }

    [Fact]
    public void ManualEftposPayment_RecordsAgainstTheOrder()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 8.00m, 0m, 8.00m, [SampleLine(8.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        RegisterServices(backend);

        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.Contains("$8.00", cut.Markup));

        cut.Find("#record-eftpos").Click();

        cut.WaitForAssertion(() => Assert.Contains("Receipt", cut.Markup));
        Assert.Single(backend.Payments);
        Assert.Equal(PaymentMethodResult.ManualEftpos, backend.Payments[0].Method);
    }

    [Fact]
    public void OverpaymentAttempt_ShowsServerRejectionMessage_AndOrderStaysOpen()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 10.00m, 0m, 10.00m, [SampleLine(10.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        RegisterServices(backend);

        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.Contains("$10.00", cut.Markup));

        cut.Find("#cash-amount").Change("15.00");
        cut.Find("#record-cash").Click();

        cut.WaitForAssertion(() => Assert.Contains("could not be recorded", cut.Markup));
        Assert.Equal(OrderStatusResult.Open, backend.Order!.Status);
        Assert.Empty(backend.Payments);
        Assert.Empty(cut.FindAll("#retry-payment"));
        Assert.Empty(cut.FindAll("#check-payment-status"));
    }

    /// <summary>
    /// PLAN-0007 Milestone D: <see cref="SubmitPaymentAsync"/> now revalidates immediately before
    /// the POST (not only after a rejection, per Milestone C) — narrows the two-tabs-same-order
    /// double-payment race by catching a since-completed order proactively. The order was completed
    /// by another terminal between this page's load and the payment attempt; the pre-submit
    /// revalidation discovers this via a plain GET and shows the receipt without ever sending the
    /// payment POST at all — an improvement over Milestone C's rejection-triggered revalidation,
    /// which only recovered after a doomed submit attempt.
    /// </summary>
    [Fact]
    public void OrderCompletedElsewhere_PreSubmitRevalidationCatchesIt_BeforeAnyPost()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 10.00m, 1.00m, 11.00m, [SampleLine(11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var stub = RegisterServices(backend);
        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.Contains("$11.00", cut.Markup));

        backend.Payments.Add(new PaymentResult(
            Guid.NewGuid(), order.Id, Guid.NewGuid(), PaymentMethodResult.Cash, PaymentStatusResult.Recorded,
            11.00m, 11.00m, Guid.NewGuid(), null, null, DateTimeOffset.UtcNow, null));
        backend.Order = order with { Status = OrderStatusResult.Completed };

        var paymentPostAttempts = 0;
        var originalRespond = stub.Respond;
        stub.Respond = req =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath.EndsWith("/payments", StringComparison.Ordinal))
            {
                paymentPostAttempts++;
            }

            return originalRespond(req);
        };

        cut.Find("#record-cash").Click();

        cut.WaitForAssertion(() => Assert.Contains("Receipt", cut.Markup));
        Assert.Empty(cut.FindAll("#record-cash"));
        Assert.Equal(0, paymentPostAttempts);
    }

    /// <summary>
    /// PLAN-0007 Milestone D: the same pre-submit revalidation for a non-Completed "no longer
    /// payable" outcome (voided elsewhere) — shows <see cref="ApiErrorMessages.OrderChangedElsewhere"/>
    /// and never sends the payment POST, mirroring Milestone C's own
    /// <c>RefreshOrderAndPaymentsAsync</c>/<c>ApplyCompletionStateAsync</c> composition rather than a
    /// new state machine.
    /// </summary>
    [Fact]
    public void OrderVoidedElsewhere_PreSubmitRevalidation_ShowsMessage_AndNeverSubmits()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 10.00m, 1.00m, 11.00m, [SampleLine(11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var stub = RegisterServices(backend);
        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.Contains("$11.00", cut.Markup));

        backend.Order = order with { Status = OrderStatusResult.Voided };

        var paymentPostAttempts = 0;
        var originalRespond = stub.Respond;
        stub.Respond = req =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath.EndsWith("/payments", StringComparison.Ordinal))
            {
                paymentPostAttempts++;
            }

            return originalRespond(req);
        };

        cut.Find("#record-cash").Click();

        cut.WaitForAssertion(() => Assert.Contains(ApiErrorMessages.OrderChangedElsewhere, cut.Markup));
        Assert.Equal(0, paymentPostAttempts);
        Assert.Empty(backend.Payments);
    }

    /// <summary>
    /// PLAN-0007 Milestone D: if the pre-submit revalidation GET itself fails (network blip), it
    /// must not block the real payment attempt — <see cref="RefreshOrderAndPaymentsAsync"/> already
    /// leaves <c>_order</c> unchanged on a failed read, so the still-Open in-memory order lets
    /// submission proceed exactly as it did before Milestone D. Without this, a network blip on the
    /// read would silently swallow a payment attempt that Milestone C's own NetworkFailure handling
    /// is supposed to catch.
    /// </summary>
    [Fact]
    public void PreSubmitRevalidationReadFailure_StillAttemptsThePayment()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 10.00m, 1.00m, 11.00m, [SampleLine(11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var stub = RegisterServices(backend);
        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.Contains("$11.00", cut.Markup));

        // Fails only the order GET (path ends with the order id), leaving the payment POST (path
        // ends with "/payments") reachable. Asserted server-side, not via the "Receipt" markup —
        // the existing post-success refresh (Milestone C) also calls GetOrderAsync and would itself
        // be blocked by the same FailingPathSuffix for the rest of this test, which is a separate,
        // pre-existing limitation unrelated to what this test verifies.
        stub.FailingPathSuffix = order.Id.ToString();

        cut.Find("#record-cash").Click();

        cut.WaitForAssertion(() => Assert.Single(backend.Payments));
    }

    /// <summary>
    /// PLAN-0007 Milestone C: 401/403 must align with the existing <see cref="ApiErrorMessages.ForLoadFailure"/>
    /// wording used elsewhere on this page (<see cref="LoadAsync"/>), and must never be offered a
    /// payment Retry/Check-status affordance — only <see cref="ApiResultKind.NetworkFailure"/> is
    /// uncertain; a real 401/403 is not.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "session has expired")]
    [InlineData(HttpStatusCode.Forbidden, "permission")]
    public void AuthFailureRecordingPayment_ShowsAlignedMessage_WithNoRetryOrCheckStatus(HttpStatusCode statusCode, string expectedFragment)
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 10.00m, 1.00m, 11.00m, [SampleLine(11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var stub = RegisterServices(backend);
        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.Contains("$11.00", cut.Markup));

        var originalRespond = stub.Respond;
        stub.Respond = req => req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath.EndsWith("/payments", StringComparison.Ordinal)
            ? new HttpResponseMessage(statusCode)
            : originalRespond(req);

        cut.Find("#record-cash").Click();

        cut.WaitForAssertion(() => Assert.Contains(expectedFragment, cut.Markup, StringComparison.OrdinalIgnoreCase));
        Assert.Empty(cut.FindAll("#retry-payment"));
        Assert.Empty(cut.FindAll("#check-payment-status"));
    }

    [Fact]
    public void ReprintButton_OnSuccess_ShowsConfirmation_AndCallsTheReprintEndpoint()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Completed, 11.00m, 0m, 11.00m, [SampleLine(11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        RegisterServices(backend);

        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.Contains("Receipt", cut.Markup));

        cut.Find("#reprint").Click();

        cut.WaitForAssertion(() => Assert.Contains("reprinted", cut.Markup));
        Assert.Equal(1, backend.ReprintCount);
    }

    [Fact]
    public void LoadingAnAlreadyCompletedOrder_ShowsReceiptDirectly_WithNoPaymentForm()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Completed, 11.00m, 0m, 11.00m, [SampleLine(11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        RegisterServices(backend);

        var cut = RenderPay(order.Id);

        cut.WaitForAssertion(() => Assert.Contains("Receipt", cut.Markup));
        Assert.Empty(cut.FindAll("#cash-amount"));
    }

    [Fact]
    public void LoadingAVoidedOrder_ShowsNotPayableMessage_WithNoPaymentForm()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Voided, 0m, 0m, 0m, []);
        var backend = new FakeOrderBackend { Order = order };
        RegisterServices(backend);

        var cut = RenderPay(order.Id);

        cut.WaitForAssertion(() => Assert.Contains("no longer payable", cut.Markup));
        Assert.Empty(cut.FindAll("#cash-amount"));
    }

    [Fact]
    public void LoadingAnOrderThatDoesNotExist_ShowsAnErrorMessage()
    {
        var backend = new FakeOrderBackend();
        RegisterServices(backend);

        var cut = RenderPay(Guid.NewGuid());

        cut.WaitForAssertion(() => Assert.Contains("could not be found", cut.Markup));
    }

    /// <summary>
    /// PLAN-0007 Milestone A. Separate from <see cref="RegisterServices"/> so the existing tests
    /// above keep constructing their <see cref="DaxaApiClient"/> exactly as before — only the new
    /// connectivity-specific tests below opt into <see cref="ConnectivityHandler"/>/
    /// <see cref="IConnectivityTracker"/> wiring.
    /// </summary>
    private StubHttpMessageHandler RegisterServicesWithConnectivity(FakeOrderBackend backend, IConnectivityTracker connectivity)
    {
        var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
        deviceStore.SaveAsync(SampleDevice()).AsTask().Wait();
        Services.AddSingleton<IDeviceContextStore>(deviceStore);
        Services.AddSingleton<IDraftOrderStore>(RegisterDraftStore());
        Services.AddSingleton(connectivity);

        var stub = new StubHttpMessageHandler { Respond = backend.Respond };
        var handler = new ConnectivityHandler(connectivity) { InnerHandler = stub };
        Services.AddSingleton(new DaxaApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://test/") }));
        return stub;
    }

    [Fact]
    public void NetworkFailureLoadingOrder_ShowsConnectionLostMessage_AndRetryButton()
    {
        var backend = new FakeOrderBackend();
        var stub = RegisterServicesWithConnectivity(backend, new ConnectivityTracker());
        stub.ThrowNetworkFailure = true;

        var cut = RenderPay(Guid.NewGuid());

        cut.WaitForAssertion(() => Assert.Contains(ApiErrorMessages.ConnectionLost, cut.Markup));
        Assert.NotEmpty(cut.FindAll("#retry-load"));
    }

    [Fact]
    public void RetryButton_OnceConnectivityRestored_LoadsTheOrder()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 10.00m, 1.00m, 11.00m, [SampleLine(11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var stub = RegisterServicesWithConnectivity(backend, new ConnectivityTracker());
        stub.ThrowNetworkFailure = true;
        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#retry-load")));

        stub.ThrowNetworkFailure = false;
        cut.Find("#retry-load").Click();

        cut.WaitForAssertion(() => Assert.Contains("$11.00", cut.Markup));
    }

    [Fact]
    public void NetworkFailureLoadingOrder_ShowsOfflineBanner()
    {
        var backend = new FakeOrderBackend();
        var stub = RegisterServicesWithConnectivity(backend, new ConnectivityTracker());
        stub.ThrowNetworkFailure = true;

        var cut = RenderPay(Guid.NewGuid());

        cut.WaitForAssertion(() => Assert.Contains("Reconnecting", cut.Markup));
    }

    /// <summary>
    /// Before PLAN-0007 Milestone C, any non-success <c>RecordPaymentAsync</c> result other than
    /// 401/403 showed "may exceed the amount owing" — actively misleading when the real cause was a
    /// dropped connection and no payment was confirmed at all. Milestone C's dedicated
    /// <see cref="ApiErrorMessages.PaymentNotConfirmed"/> replaces the read-path
    /// <see cref="ApiErrorMessages.ConnectionLost"/> wording (which implies automatic retry — payments
    /// never auto-retry) and offers explicit Retry/Check-status actions instead.
    /// </summary>
    [Fact]
    public void NetworkFailureRecordingPayment_ShowsPendingPaymentState_WithRetryAndCheckStatusActions()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 10.00m, 1.00m, 11.00m, [SampleLine(11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var connectivity = new ConnectivityTracker();
        var stub = RegisterServicesWithConnectivity(backend, connectivity);
        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.Contains("$11.00", cut.Markup));

        stub.ThrowNetworkFailure = true;
        cut.Find("#record-cash").Click();

        cut.WaitForAssertion(() => Assert.Contains(ApiErrorMessages.PaymentNotConfirmed, cut.Markup));
        Assert.DoesNotContain("may exceed", cut.Markup);
        Assert.NotEmpty(cut.FindAll("#retry-payment"));
        Assert.NotEmpty(cut.FindAll("#check-payment-status"));
        Assert.Empty(backend.Payments);
    }

    /// <summary>
    /// PLAN-0007 Milestone C: the linchpin safety property — a Retry after a network failure must
    /// reuse the exact same <see cref="RecordPaymentRequest.IdempotencyKey"/> as the failed attempt,
    /// not generate a new one, so a request that actually reached the server the first time resolves
    /// to the same <c>Payment</c> row (<c>PaymentEndpoints</c>' existing idempotency check) rather
    /// than creating a duplicate.
    /// </summary>
    [Fact]
    public async Task Retry_AfterNetworkFailure_ReusesTheSameIdempotencyKey_AndSucceeds()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 10.00m, 1.00m, 11.00m, [SampleLine(11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var connectivity = new ConnectivityTracker();
        var stub = RegisterServicesWithConnectivity(backend, connectivity);
        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.Contains("$11.00", cut.Markup));

        stub.ThrowNetworkFailure = true;
        cut.Find("#record-cash").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#retry-payment")));

        var failedAttemptKey = (await stub.LastRequest!.Content!.ReadFromJsonAsync<RecordPaymentRequest>())!.IdempotencyKey;

        stub.ThrowNetworkFailure = false;
        cut.Find("#retry-payment").Click();

        cut.WaitForAssertion(() => Assert.Contains("Receipt", cut.Markup));
        Assert.Single(backend.Payments);
        Assert.Equal(failedAttemptKey, backend.Payments[0].IdempotencyKey);
    }

    /// <summary>
    /// PLAN-0007 Milestone C, mirroring Milestone B's equivalent Sales.razor guarantee: connectivity
    /// alone recovering must never auto-submit a payment. Only an explicit staff tap on Retry does.
    /// </summary>
    [Fact]
    public void ConnectivityRestoring_WithoutRetryClick_DoesNotAutoSubmitPayment()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 10.00m, 1.00m, 11.00m, [SampleLine(11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var connectivity = new ConnectivityTracker();
        var stub = RegisterServicesWithConnectivity(backend, connectivity);
        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.Contains("$11.00", cut.Markup));

        stub.ThrowNetworkFailure = true;
        cut.Find("#record-cash").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#retry-payment")));

        stub.ThrowNetworkFailure = false;
        cut.InvokeAsync(connectivity.ReportOnline);

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#retry-payment")));
        Assert.Empty(backend.Payments);
    }

    /// <summary>
    /// PLAN-0007 Milestone C: the "did it land" recovery path for an ack-loss network failure — the
    /// original request actually reached and was processed by the server (simulated here by seeding
    /// the backend with a payment under the same idempotency key the failed attempt used), but the
    /// client never saw the response. "Check status" only reads (<c>GetOrderAsync</c>/
    /// <c>GetPaymentsAsync</c>/<c>GetReceiptAsync</c>) — it must never re-POST — and must resolve the
    /// pending state once it finds the matching payment, without assuming the payment failed.
    /// </summary>
    [Fact]
    public async Task CheckStatus_WhenPaymentAlreadyLandedServerSide_ResolvesPendingState_WithoutASecondPost()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 10.00m, 1.00m, 11.00m, [SampleLine(11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var connectivity = new ConnectivityTracker();
        var stub = RegisterServicesWithConnectivity(backend, connectivity);
        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.Contains("$11.00", cut.Markup));

        stub.ThrowNetworkFailure = true;
        cut.Find("#record-cash").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#check-payment-status")));

        var uncertainAttemptKey = (await stub.LastRequest!.Content!.ReadFromJsonAsync<RecordPaymentRequest>())!.IdempotencyKey;

        // Simulate the request actually reaching the server despite the client seeing a
        // NetworkFailure: the backend now has the payment, recorded via an out-of-band path that
        // bypasses this test's stub (which would otherwise still throw for a real POST retry).
        backend.Payments.Add(new PaymentResult(
            Guid.NewGuid(), order.Id, Guid.NewGuid(), PaymentMethodResult.Cash, PaymentStatusResult.Recorded,
            11.00m, 11.00m, uncertainAttemptKey, null, null, DateTimeOffset.UtcNow, null));
        backend.Order = order with { Status = OrderStatusResult.Completed };

        // Connectivity is back by the time staff taps "Check status" — only the original POST's
        // response was lost, not the connection itself.
        stub.ThrowNetworkFailure = false;
        cut.Find("#check-payment-status").Click();

        cut.WaitForAssertion(() => Assert.Contains("Receipt", cut.Markup));
        Assert.Single(backend.Payments);
        Assert.Empty(cut.FindAll("#retry-payment"));
    }

    /// <summary>
    /// PLAN-0007 Milestone C: fixes <c>EnterReceiptStateAsync</c> silently swallowing a receipt-fetch
    /// failure. Before this fix, a network blip right after a payment completed the order fell through
    /// to the normal Cash/Manual-EFTPOS payment-entry UI for an order that was already fully paid —
    /// inviting a second payment attempt against a closed order.
    /// </summary>
    [Fact]
    public void ReceiptFetchFailure_AfterPaymentCompletesTheOrder_ShowsRecoverableState_NotPaymentButtons()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 10.00m, 1.00m, 11.00m, [SampleLine(11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var connectivity = new ConnectivityTracker();
        var stub = RegisterServicesWithConnectivity(backend, connectivity);
        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.Contains("$11.00", cut.Markup));

        stub.FailingPathSuffix = "/receipt";
        cut.Find("#record-cash").Click();

        cut.WaitForAssertion(() => Assert.Contains(ApiErrorMessages.ReceiptUnavailable, cut.Markup));
        Assert.Equal(OrderStatusResult.Completed, backend.Order!.Status);
        Assert.Empty(cut.FindAll("#record-cash"));
        Assert.NotEmpty(cut.FindAll("#retry-receipt"));
    }

    /// <summary>PLAN-0007 Milestone C: the receipt-unavailable state must actually recover once the
    /// receipt endpoint becomes reachable again.</summary>
    [Fact]
    public void RetryReceipt_OnceTheEndpointRecovers_ShowsTheReceipt()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 10.00m, 1.00m, 11.00m, [SampleLine(11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var connectivity = new ConnectivityTracker();
        var stub = RegisterServicesWithConnectivity(backend, connectivity);
        var cut = RenderPay(order.Id);
        cut.WaitForAssertion(() => Assert.Contains("$11.00", cut.Markup));

        stub.FailingPathSuffix = "/receipt";
        cut.Find("#record-cash").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#retry-receipt")));

        stub.FailingPathSuffix = null;
        cut.Find("#retry-receipt").Click();

        cut.WaitForAssertion(() => Assert.Contains("Receipt", cut.Markup));
        Assert.Contains("$11.00", cut.Markup);
    }
}
