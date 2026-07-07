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
}
