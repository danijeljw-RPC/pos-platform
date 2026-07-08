using Bunit;
using DaxaPos.Web.Api;
using DaxaPos.Web.Pages;
using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Web.Tests.Pages;

/// <summary>
/// Milestone F: <see cref="Kds"/> is a minimal, read-only kitchen board — no station routing, no
/// mark-ready/complete, no real kitchen-ticket lifecycle. It reuses <c>GET /api/v1/orders</c>
/// (already staff-PIN-eligible, already location-scoped server-side) rather than a new endpoint.
/// Drives <see cref="FakeOrderBackend"/>'s <c>Orders</c> list, independent of the single-order
/// <c>Order</c> property Sales/Pay/Display tests use.
/// </summary>
public class KdsTests : TestContext
{
    private static readonly Guid DeviceId = Guid.NewGuid();
    private static readonly Guid LocationId = Guid.NewGuid();
    private static readonly Guid TerminalId = Guid.NewGuid();

    private static readonly TimeSpan FastPoll = TimeSpan.FromMilliseconds(20);

    private static DeviceContext SampleDevice() => new(
        DeviceId, Guid.NewGuid(), Guid.NewGuid(), LocationId, "KioskBrowser", "Kitchen Screen", "device-token");

    private static OrderLineResult SampleLine(string name, int quantity = 1, string? notes = null, IReadOnlyList<OrderLineModifierResult>? modifiers = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), quantity, name, 5.5m, 5.5m * quantity, notes, OrderLineStatusResult.Active, modifiers ?? []);

    private static OrderResult SampleOrder(
        OrderStatusResult status, long orderNumber, DateTimeOffset openedAtUtc, IReadOnlyList<OrderLineResult>? lines = null) =>
        new(Guid.NewGuid(), TerminalId, status, 5.5m, 0.5m, 6.0m, lines ?? [SampleLine("Flat White")], orderNumber, openedAtUtc);

    private IDeviceContextStore RegisterDeviceStore(bool withDevice)
    {
        var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
        if (withDevice)
        {
            deviceStore.SaveAsync(SampleDevice()).AsTask().Wait();
        }

        Services.AddSingleton<IDeviceContextStore>(deviceStore);
        return deviceStore;
    }

    private void RegisterServices(FakeOrderBackend backend, bool withDevice = true)
    {
        RegisterDeviceStore(withDevice);

        var stub = new StubHttpMessageHandler { Respond = backend.Respond };
        Services.AddSingleton(new DaxaApiClient(new HttpClient(stub) { BaseAddress = new Uri("http://test/") }));
    }

    private IRenderedComponent<Kds> RenderKds() =>
        RenderComponent<Kds>(parameters => parameters.Add(p => p.PollInterval, FastPoll));

    /// <summary>
    /// PLAN-0007 Milestone A. Separate from <see cref="RegisterServices"/> so the existing tests
    /// above keep constructing their <see cref="DaxaApiClient"/> exactly as before — only the new
    /// connectivity-specific tests below opt into <see cref="ConnectivityHandler"/>/
    /// <see cref="IConnectivityTracker"/> wiring.
    /// </summary>
    private StubHttpMessageHandler RegisterServicesWithConnectivity(FakeOrderBackend backend, IConnectivityTracker connectivity)
    {
        RegisterDeviceStore(withDevice: true);
        Services.AddSingleton(connectivity);

        var stub = new StubHttpMessageHandler { Respond = backend.Respond };
        var handler = new ConnectivityHandler(connectivity) { InnerHandler = stub };
        Services.AddSingleton(new DaxaApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://test/") }));
        return stub;
    }

    [Fact]
    public void NoDeviceRegistered_ShowsSetupPrompt()
    {
        var backend = new FakeOrderBackend();
        RegisterServices(backend, withDevice: false);

        var cut = RenderKds();

        Assert.Contains("No device is registered", cut.Markup);
    }

    [Fact]
    public void NoOpenOrders_ShowsEmptyState()
    {
        var backend = new FakeOrderBackend();
        RegisterServices(backend);

        var cut = RenderKds();

        cut.WaitForAssertion(() => Assert.Contains("No open orders", cut.Markup));
    }

    [Fact]
    public void OpenOrder_ShowsOrderNumberLinesModifiersAndNotes()
    {
        var modifiers = new[] { new OrderLineModifierResult(Guid.NewGuid(), Guid.NewGuid(), "Oat milk", 0.5m) };
        var line = SampleLine("Flat White", quantity: 2, notes: "Extra hot", modifiers: modifiers);
        var order = SampleOrder(OrderStatusResult.Open, 1042, DateTimeOffset.UtcNow, [line]);
        var backend = new FakeOrderBackend();
        backend.Orders.Add(order);
        RegisterServices(backend);

        var cut = RenderKds();

        cut.WaitForAssertion(() => Assert.Contains("1042", cut.Markup));
        Assert.Contains("Flat White", cut.Markup);
        Assert.Contains("Oat milk", cut.Markup);
        Assert.Contains("Extra hot", cut.Markup);
        Assert.Contains("2", cut.Markup);
    }

    [Fact]
    public void HeldOrder_IsShown()
    {
        var order = SampleOrder(OrderStatusResult.Held, 7, DateTimeOffset.UtcNow);
        var backend = new FakeOrderBackend();
        backend.Orders.Add(order);
        RegisterServices(backend);

        var cut = RenderKds();

        cut.WaitForAssertion(() => Assert.Contains("Held", cut.Markup));
    }

    [Fact]
    public void CompletedVoidedAndCancelledOrders_AreExcluded()
    {
        var backend = new FakeOrderBackend();
        backend.Orders.Add(SampleOrder(OrderStatusResult.Completed, 1, DateTimeOffset.UtcNow));
        backend.Orders.Add(SampleOrder(OrderStatusResult.Voided, 2, DateTimeOffset.UtcNow));
        backend.Orders.Add(SampleOrder(OrderStatusResult.Cancelled, 3, DateTimeOffset.UtcNow));
        RegisterServices(backend);

        var cut = RenderKds();

        cut.WaitForAssertion(() => Assert.Contains("No open orders", cut.Markup));
    }

    [Fact]
    public void MultipleOpenOrders_AreSortedOldestFirst()
    {
        var now = DateTimeOffset.UtcNow;
        var older = SampleOrder(OrderStatusResult.Open, 100, now.AddMinutes(-10));
        var newer = SampleOrder(OrderStatusResult.Open, 200, now);
        var backend = new FakeOrderBackend();
        backend.Orders.Add(newer);
        backend.Orders.Add(older);
        RegisterServices(backend);

        var cut = RenderKds();

        cut.WaitForAssertion(() => Assert.Contains("100", cut.Markup));
        var indexOfOlder = cut.Markup.IndexOf("100", StringComparison.Ordinal);
        var indexOfNewer = cut.Markup.IndexOf("200", StringComparison.Ordinal);
        Assert.True(indexOfOlder < indexOfNewer);
    }

    [Fact]
    public void LoadFailure_Forbidden_ShowsPermissionMessage_NotACrash()
    {
        RegisterDeviceStore(withDevice: true);

        var stub = new StubHttpMessageHandler { Respond = _ => new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden) };
        Services.AddSingleton(new DaxaApiClient(new HttpClient(stub) { BaseAddress = new Uri("http://test/") }));

        var cut = RenderKds();

        cut.WaitForAssertion(() => Assert.Contains("You don't have permission", cut.Markup));
    }

    [Fact]
    public void LoadFailure_Unauthorized_ShowsSessionExpiredMessage_NotACrash()
    {
        RegisterDeviceStore(withDevice: true);

        var stub = new StubHttpMessageHandler { Respond = _ => new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized) };
        Services.AddSingleton(new DaxaApiClient(new HttpClient(stub) { BaseAddress = new Uri("http://test/") }));

        var cut = RenderKds();

        cut.WaitForAssertion(() => Assert.Contains("Your session has expired", cut.Markup));
    }

    [Fact]
    public void NewOrderAppearingOnPoll_IsPickedUpWithoutManualRefresh()
    {
        var backend = new FakeOrderBackend();
        RegisterServices(backend);

        var cut = RenderKds();
        cut.WaitForAssertion(() => Assert.Contains("No open orders", cut.Markup));

        backend.Orders.Add(SampleOrder(OrderStatusResult.Open, 55, DateTimeOffset.UtcNow));

        cut.WaitForAssertion(() => Assert.Contains("55", cut.Markup));
    }

    [Fact]
    public void NetworkFailure_ShowsConnectionLostMessage_NotGenericFailure()
    {
        var connectivity = new ConnectivityTracker();
        var backend = new FakeOrderBackend();
        var stub = RegisterServicesWithConnectivity(backend, connectivity);
        stub.ThrowNetworkFailure = true;

        var cut = RenderKds();

        cut.WaitForAssertion(() => Assert.Contains(ApiErrorMessages.ConnectionLost, cut.Markup));
    }

    [Fact]
    public void NetworkFailureAfterOrdersLoaded_KeepsShowingTheLastBoard()
    {
        var connectivity = new ConnectivityTracker();
        var backend = new FakeOrderBackend();
        var order = SampleOrder(OrderStatusResult.Open, 99, DateTimeOffset.UtcNow);
        backend.Orders.Add(order);
        var stub = RegisterServicesWithConnectivity(backend, connectivity);

        var cut = RenderKds();
        cut.WaitForAssertion(() => Assert.Contains("99", cut.Markup));

        stub.ThrowNetworkFailure = true;

        cut.WaitForAssertion(() => Assert.Contains(ApiErrorMessages.ConnectionLost, cut.Markup));
        Assert.Contains("99", cut.Markup);
    }

    [Fact]
    public void NetworkFailure_ShowsOfflineBanner()
    {
        var connectivity = new ConnectivityTracker();
        var backend = new FakeOrderBackend();
        var stub = RegisterServicesWithConnectivity(backend, connectivity);
        stub.ThrowNetworkFailure = true;

        var cut = RenderKds();

        cut.WaitForAssertion(() => Assert.Contains("Reconnecting", cut.Markup));
    }
}
