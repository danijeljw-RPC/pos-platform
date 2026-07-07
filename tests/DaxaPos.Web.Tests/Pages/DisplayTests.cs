using Bunit;
using DaxaPos.Web.Api;
using DaxaPos.Web.Pages;
using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Web.Tests.Pages;

/// <summary>
/// Milestone E: <see cref="Display"/> is the customer-facing display page, polling the same
/// terminal-scoped order/payment/receipt endpoints <see cref="Sales"/>/<see cref="Pay"/> already use.
/// Drives <see cref="FakeOrderBackend"/> (Milestone D's fake) rather than a live backend.
/// </summary>
public class DisplayTests : TestContext
{
    private static readonly Guid DeviceId = Guid.NewGuid();
    private static readonly Guid LocationId = Guid.NewGuid();
    private static readonly Guid TerminalId = Guid.NewGuid();

    private static readonly TimeSpan FastPoll = TimeSpan.FromMilliseconds(20);

    private static DeviceContext SampleDevice() => new(
        DeviceId, Guid.NewGuid(), Guid.NewGuid(), LocationId, "KioskBrowser", "Front Counter", "device-token");

    private static OrderLineResult SampleLine(string name, decimal amount) =>
        new(Guid.NewGuid(), Guid.NewGuid(), 1, name, amount, amount, null, OrderLineStatusResult.Active, []);

    private DraftOrderStore RegisterDraftStore() => new(new InMemoryBrowserStorage());

    private DraftOrderStore RegisterServices(FakeOrderBackend backend, bool withDevice = true)
    {
        if (withDevice)
        {
            var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
            deviceStore.SaveAsync(SampleDevice()).AsTask().Wait();
            Services.AddSingleton<IDeviceContextStore>(deviceStore);
        }
        else
        {
            Services.AddSingleton<IDeviceContextStore>(new DeviceContextStore(new InMemoryBrowserStorage()));
        }

        var draftStore = RegisterDraftStore();
        Services.AddSingleton<IDraftOrderStore>(draftStore);

        var stub = new StubHttpMessageHandler { Respond = backend.Respond };
        Services.AddSingleton(new DaxaApiClient(new HttpClient(stub) { BaseAddress = new Uri("http://test/") }));
        return draftStore;
    }

    private IRenderedComponent<Display> RenderDisplay() =>
        RenderComponent<Display>(parameters => parameters.Add(p => p.PollInterval, FastPoll));

    [Fact]
    public void NoDeviceRegistered_ShowsIdleBranding()
    {
        var backend = new FakeOrderBackend();
        RegisterServices(backend, withDevice: false);

        var cut = RenderDisplay();

        Assert.Contains("Daxa POS", cut.Markup);
        Assert.DoesNotContain("Balance due", cut.Markup);
    }

    [Fact]
    public void DeviceRegisteredButNoTrackedOrder_ShowsIdleBranding()
    {
        var backend = new FakeOrderBackend();
        RegisterServices(backend);

        var cut = RenderDisplay();

        cut.WaitForAssertion(() => Assert.Contains("Daxa POS", cut.Markup));
        Assert.DoesNotContain("Balance due", cut.Markup);
    }

    [Fact]
    public async Task ActiveOpenOrder_ShowsLineItemsAndServerTotal()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 10.00m, 1.00m, 11.00m,
            [SampleLine("Flat White", 11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var draftStore = RegisterServices(backend);
        await draftStore.SaveOrderIdAsync(DeviceId, order.Id);

        var cut = RenderDisplay();

        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("$11.00", cut.Markup));
    }

    [Fact]
    public async Task PartialPayment_ShowsRemainingBalanceDue()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 20.00m, 0m, 20.00m,
            [SampleLine("Burger", 20.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var draftStore = RegisterServices(backend);
        await draftStore.SaveOrderIdAsync(DeviceId, order.Id);
        backend.Payments.Add(new PaymentResult(
            Guid.NewGuid(), order.Id, LocationId, PaymentMethodResult.Cash, PaymentStatusResult.Recorded,
            12.00m, 12.00m, Guid.NewGuid(), null, Guid.NewGuid(), DateTimeOffset.UtcNow, null));

        var cut = RenderDisplay();

        // Needs two poll round-trips (order, then payments) before the balance settles, so give
        // it more headroom than the 1s bUnit default.
        cut.WaitForAssertion(() => Assert.Contains("$8.00", cut.Markup), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task CompletedOrder_ShowsReceipt()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Completed, 11.00m, 0m, 11.00m,
            [SampleLine("Flat White", 11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var draftStore = RegisterServices(backend);
        await draftStore.SaveOrderIdAsync(DeviceId, order.Id);

        var cut = RenderDisplay();

        cut.WaitForAssertion(() => Assert.Contains("Thank you", cut.Markup));
        Assert.Contains("Flat White", cut.Markup);
    }

    [Fact]
    public async Task DraftClearedRightAfterCompletion_KeepsShowingTheReceipt()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 11.00m, 0m, 11.00m,
            [SampleLine("Flat White", 11.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var draftStore = RegisterServices(backend);
        await draftStore.SaveOrderIdAsync(DeviceId, order.Id);

        var cut = RenderDisplay();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));

        // Simulate Pay.razor: the order settles and the draft pointer is cleared, in that order.
        backend.Order = order with { Status = OrderStatusResult.Completed };
        await draftStore.ClearAsync(DeviceId);

        cut.WaitForAssertion(() => Assert.Contains("Thank you", cut.Markup));
        Assert.Contains("Flat White", cut.Markup);
    }

    [Fact]
    public async Task OrderVoidedAndDraftCleared_ResetsToIdle()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 5.00m, 0m, 5.00m,
            [SampleLine("Muffin", 5.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var draftStore = RegisterServices(backend);
        await draftStore.SaveOrderIdAsync(DeviceId, order.Id);

        var cut = RenderDisplay();
        cut.WaitForAssertion(() => Assert.Contains("Muffin", cut.Markup));

        // Simulate Sales.razor's ClearOrder: void server-side, clear the local pointer.
        backend.Order = order with { Status = OrderStatusResult.Voided };
        await draftStore.ClearAsync(DeviceId);

        cut.WaitForAssertion(() => Assert.Contains("Daxa POS", cut.Markup));
        Assert.DoesNotContain("Muffin", cut.Markup);
    }

    [Fact]
    public async Task NewOrderStartingWhileOldReceiptShowing_SwitchesToTheNewOrder()
    {
        var firstOrder = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Completed, 11.00m, 0m, 11.00m,
            [SampleLine("Flat White", 11.00m)]);
        var backend = new FakeOrderBackend { Order = firstOrder };
        var draftStore = RegisterServices(backend);
        await draftStore.SaveOrderIdAsync(DeviceId, firstOrder.Id);

        var cut = RenderDisplay();
        cut.WaitForAssertion(() => Assert.Contains("Thank you", cut.Markup));

        var secondOrder = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 4.00m, 0m, 4.00m,
            [SampleLine("Cookie", 4.00m)]);
        backend.Order = secondOrder;
        await draftStore.SaveOrderIdAsync(DeviceId, secondOrder.Id);

        cut.WaitForAssertion(() => Assert.Contains("Cookie", cut.Markup));
        Assert.DoesNotContain("Thank you", cut.Markup);
    }

    [Fact]
    public async Task OrderBelongingToADifferentTerminal_DegradesToIdle_NotAnError()
    {
        var order = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 5.00m, 0m, 5.00m,
            [SampleLine("Muffin", 5.00m)]);
        var backend = new FakeOrderBackend { Order = order };
        var draftStore = RegisterServices(backend);

        // The stored pointer refers to an order the backend won't return for this session
        // (mirrors C.2's terminal-scoped 404 for a different terminal's order).
        await draftStore.SaveOrderIdAsync(DeviceId, Guid.NewGuid());

        var cut = RenderDisplay();

        cut.WaitForAssertion(() => Assert.Contains("Daxa POS", cut.Markup));
        Assert.DoesNotContain("Muffin", cut.Markup);
        Assert.DoesNotContain("error", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}
