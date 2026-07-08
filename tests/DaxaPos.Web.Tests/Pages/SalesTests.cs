using System.Net;
using Bunit;
using DaxaPos.Web.Api;
using DaxaPos.Web.Pages;
using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Web.Tests.Pages;

/// <summary>
/// Milestone C.1: <see cref="Sales"/> now wires a real <c>Order</c>/<c>OrderLine</c> (via
/// <see cref="FakeOrderBackend"/>) instead of a local-only draft — these tests drive that flow end
/// to end (open/add-line/void-line/void-order) rather than asserting on in-memory list state.
/// </summary>
public class SalesTests : TestContext
{
    private static readonly Guid DeviceId = Guid.NewGuid();
    private static readonly Guid LocationId = Guid.NewGuid();
    private static readonly Guid TerminalId = Guid.NewGuid();

    private static DeviceContext SampleDevice() => new(
        DeviceId, Guid.NewGuid(), Guid.NewGuid(), LocationId, "KioskBrowser", "Front Counter", "device-token");

    private static ResolvedMenuResult SimpleMenu(Guid productId, string productName, decimal price) => new(
        LocationId,
        [
            new ResolvedMenuSectionResult(Guid.NewGuid(), Guid.NewGuid(), "Coffee", 0,
            [
                new ResolvedMenuItemResult(productId, productName, 0, price, true, "AU_GST_10", []),
            ]),
        ]);

    private static ResolvedMenuResult MenuWithRequiredModifier(Guid productId, string productName, decimal price, Guid groupId, Guid modifierId, string modifierName) => new(
        LocationId,
        [
            new ResolvedMenuSectionResult(Guid.NewGuid(), Guid.NewGuid(), "Mains", 0,
            [
                new ResolvedMenuItemResult(productId, productName, 0, price, true, "AU_GST_10",
                [
                    new ResolvedModifierGroupResult(groupId, "Doneness", 1, 1, true, 0,
                    [
                        new ResolvedModifierResult(modifierId, modifierName, 0m),
                    ]),
                ]),
            ]),
        ]);

    private DraftOrderStore RegisterDraftStore() => Also(new DraftOrderStore(new InMemoryBrowserStorage()));

    private StubHttpMessageHandler RegisterCommonServices(FakeOrderBackend backend, DraftOrderStore? draftStore = null, bool withTerminal = true, IDraftPointerWatcher? draftWatcher = null)
    {
        var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
        deviceStore.SaveAsync(SampleDevice()).AsTask().Wait();
        Services.AddSingleton<IDeviceContextStore>(deviceStore);

        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        sessionStore.SaveAsync(new SessionState(
            "token", DateTimeOffset.UtcNow.AddHours(1), Guid.NewGuid(), "Jane Staff", ["StaffPin"], ["orders.manage"],
            withTerminal ? TerminalId : null)).AsTask().Wait();
        Services.AddSingleton<IAuthSessionStore>(sessionStore);

        Services.AddSingleton<IDraftOrderStore>(draftStore ?? RegisterDraftStore());

        // PLAN-0007 Milestone D: optional, mirroring ConnectivityBanner's pattern — a test that
        // doesn't care about cross-tab detection doesn't register a watcher, and Sales resolves it
        // defensively (IServiceProvider.GetService), so none of the ~15 pre-existing tests above
        // needed to change.
        if (draftWatcher is not null)
        {
            Services.AddSingleton(draftWatcher);
        }

        var stub = new StubHttpMessageHandler { Respond = backend.Respond };
        Services.AddSingleton(new DaxaApiClient(new HttpClient(stub) { BaseAddress = new Uri("http://test/") }));
        return stub;
    }

    [Fact]
    public void WhenNoDeviceRegistered_ShowsPromptInsteadOfMenu()
    {
        Services.AddSingleton<IDeviceContextStore>(new DeviceContextStore(new InMemoryBrowserStorage()));
        Services.AddSingleton<IAuthSessionStore>(new AuthSessionStore(new InMemoryBrowserStorage()));
        Services.AddSingleton<IDraftOrderStore>(RegisterDraftStore());
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildFailure(HttpStatusCode.OK, out _));

        var cut = RenderComponent<Sales>();

        Assert.Contains("No device is registered", cut.Markup);
    }

    [Fact]
    public void WhenDeviceHasNoTerminalLinked_ShowsBlockingMessage_InsteadOfLoadingMenu()
    {
        var backend = new FakeOrderBackend();
        RegisterCommonServices(backend, withTerminal: false);

        var cut = RenderComponent<Sales>();

        Assert.Contains("isn't linked to a POS terminal", cut.Markup);
    }

    [Fact]
    public async Task WhenMenuLoadFails_ShowsErrorMessageAsync()
    {
        var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
        await deviceStore.SaveAsync(SampleDevice());
        Services.AddSingleton<IDeviceContextStore>(deviceStore);

        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        await sessionStore.SaveAsync(new SessionState(
            "token", DateTimeOffset.UtcNow.AddHours(1), Guid.NewGuid(), "Jane Staff", ["StaffPin"], ["orders.manage"], TerminalId));
        Services.AddSingleton<IAuthSessionStore>(sessionStore);
        Services.AddSingleton<IDraftOrderStore>(RegisterDraftStore());
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildFailure(HttpStatusCode.NotFound, out _));

        var cut = RenderComponent<Sales>();

        cut.WaitForAssertion(() => Assert.Contains("Could not load the menu", cut.Markup));
    }

    [Fact]
    public async Task WhenMenuLoadFailsWithUnauthorized_ShowsSessionExpiredMessageAsync()
    {
        var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
        await deviceStore.SaveAsync(SampleDevice());
        Services.AddSingleton<IDeviceContextStore>(deviceStore);

        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        await sessionStore.SaveAsync(new SessionState(
            "token", DateTimeOffset.UtcNow.AddHours(1), Guid.NewGuid(), "Jane Staff", ["StaffPin"], ["orders.manage"], TerminalId));
        Services.AddSingleton<IAuthSessionStore>(sessionStore);
        Services.AddSingleton<IDraftOrderStore>(RegisterDraftStore());
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildFailure(HttpStatusCode.Unauthorized, out _));

        var cut = RenderComponent<Sales>();

        cut.WaitForAssertion(() => Assert.Contains("Your session has expired", cut.Markup));
    }

    [Fact]
    public async Task WhenMenuLoadFailsWithForbidden_ShowsPermissionMessageAsync()
    {
        var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
        await deviceStore.SaveAsync(SampleDevice());
        Services.AddSingleton<IDeviceContextStore>(deviceStore);

        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        await sessionStore.SaveAsync(new SessionState(
            "token", DateTimeOffset.UtcNow.AddHours(1), Guid.NewGuid(), "Jane Staff", ["StaffPin"], ["orders.manage"], TerminalId));
        Services.AddSingleton<IAuthSessionStore>(sessionStore);
        Services.AddSingleton<IDraftOrderStore>(RegisterDraftStore());
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildFailure(HttpStatusCode.Forbidden, out _));

        var cut = RenderComponent<Sales>();

        cut.WaitForAssertion(() => Assert.Contains("You don't have permission", cut.Markup));
    }

    [Fact]
    public void WhenMenuHasNoItems_ShowsEmptyState()
    {
        var backend = new FakeOrderBackend { Menu = new ResolvedMenuResult(LocationId, []) };
        RegisterCommonServices(backend);

        var cut = RenderComponent<Sales>();

        cut.WaitForAssertion(() => Assert.Contains("No menu items are available", cut.Markup));
    }

    [Fact]
    public void TappingASimpleTile_OpensARealOrder_AndAddsALine()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        var stub = RegisterCommonServices(backend);

        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));
        Assert.Contains($"locationId={LocationId}", stub.LastRequest!.RequestUri!.Query);

        cut.Find("button.btn-outline-primary").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("No items selected yet.", cut.Markup));
        Assert.Equal(TerminalId, backend.LastOpenedTerminalId);
        cut.WaitForAssertion(() => Assert.Contains("$5.50", cut.Markup));
    }

    [Fact]
    public void TappingASimpleTileTwice_DisplaysAsOneGroupWithQuantityTwo()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        RegisterCommonServices(backend);

        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));

        var tile = cut.Find("button.btn-outline-primary");
        tile.Click();
        cut.WaitForAssertion(() => Assert.Contains("$5.50", cut.Markup));
        tile.Click();

        cut.WaitForAssertion(() => Assert.Contains("$11.00", cut.Markup));
        Assert.Equal(2, backend.Order!.Lines.Count);
    }

    [Fact]
    public void TileWithARequiredModifierGroup_OpensAModal_AndBlocksAddUntilSelected()
    {
        var productId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var modifierId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = MenuWithRequiredModifier(productId, "Steak", 20m, groupId, modifierId, "Medium Rare") };
        backend.RegisterProduct(productId, "Steak", 20m);
        backend.RegisterModifier(modifierId, "Medium Rare", 0m);
        RegisterCommonServices(backend);

        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Steak", cut.Markup));
        cut.Find("button.btn-outline-primary").Click();

        Assert.Contains("Doneness", cut.Markup);
        var addButton = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Add");
        Assert.True(addButton.HasAttribute("disabled"));

        cut.Find("input[type=checkbox]").Change(true);
        cut.WaitForAssertion(() => Assert.False(cut.FindAll("button").Single(b => b.TextContent.Trim() == "Add").HasAttribute("disabled")));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Add").Click();

        cut.WaitForAssertion(() => Assert.Contains("Medium Rare", cut.Markup));
        Assert.Single(backend.Order!.Lines);
        Assert.Single(backend.Order!.Lines[0].Modifiers);
    }

    [Fact]
    public void DecrementingToZero_VoidsTheLine_AndRemovesTheGroup()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        RegisterCommonServices(backend);

        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));
        cut.Find("button.btn-outline-primary").Click();
        cut.WaitForAssertion(() => Assert.Contains("$5.50", cut.Markup));

        var decrementButton = cut.FindAll("button").Single(b => b.TextContent.Trim() == "-");
        decrementButton.Click();

        cut.WaitForAssertion(() => Assert.Contains("No items selected yet.", cut.Markup));
        Assert.All(backend.Order!.Lines, l => Assert.Equal(OrderLineStatusResult.Voided, l.Status));
    }

    [Fact]
    public async Task ClearOrder_VoidsTheOrderServerSide_AndClearsTheStoredPointer()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        var draftStore = RegisterDraftStore();
        RegisterCommonServices(backend, draftStore);

        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));
        cut.Find("button.btn-outline-primary").Click();
        cut.WaitForAssertion(() => Assert.Contains("Clear order", cut.Markup));

        var clearButton = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Clear order");
        clearButton.Click();

        cut.WaitForAssertion(() => Assert.Contains("No items selected yet.", cut.Markup));
        Assert.Equal(OrderStatusResult.Voided, backend.Order!.Status);
        Assert.Null(await draftStore.GetOrderIdAsync(DeviceId));
    }

    [Fact]
    public void WhenOrderHasLines_PayButtonNavigatesToThePayPage()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        RegisterCommonServices(backend);

        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));

        Assert.DoesNotContain("Pay", cut.FindAll("button").Select(b => b.TextContent.Trim()));

        cut.Find("button.btn-outline-primary").Click();
        cut.WaitForAssertion(() => Assert.Contains("Clear order", cut.Markup));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Pay").Click();

        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith($"/sales/pay/{backend.Order!.Id}", navigation.Uri);
    }

    [Fact]
    public async Task OnRefresh_ARestorableStoredOrder_RebuildsTheCartFromTheServer()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        var existingLine = new OrderLineResult(Guid.NewGuid(), productId, 1, "Flat White", 5.5m, 5.5m, null, OrderLineStatusResult.Active, []);
        var existingOrder = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 5.5m, 0m, 5.5m, [existingLine]);
        backend.Order = existingOrder;
        var draftStore = RegisterDraftStore();
        await draftStore.SaveOrderIdAsync(DeviceId, existingOrder.Id);
        RegisterCommonServices(backend, draftStore);

        var cut = RenderComponent<Sales>();

        cut.WaitForAssertion(() => Assert.Contains("$5.50", cut.Markup));
        Assert.DoesNotContain("No items selected yet.", cut.Markup);
    }

    [Fact]
    public async Task OnRefresh_AStoredOrderThatNoLongerExists_ClearsThePointer_AndStartsEmpty()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        // backend.Order stays null -> GetOrderAsync(orderId) 404s for whatever id is stored.
        var draftStore = RegisterDraftStore();
        var staleOrderId = Guid.NewGuid();
        await draftStore.SaveOrderIdAsync(DeviceId, staleOrderId);
        RegisterCommonServices(backend, draftStore);

        var cut = RenderComponent<Sales>();

        cut.WaitForAssertion(() => Assert.Contains("No items selected yet.", cut.Markup));
        Assert.Null(await draftStore.GetOrderIdAsync(DeviceId));
    }

    private static T Also<T>(T value) => value;

    /// <summary>
    /// PLAN-0007 Milestone A. Separate from <see cref="RegisterCommonServices"/> so the existing
    /// tests above keep constructing their <see cref="DaxaApiClient"/> exactly as before — only the
    /// new connectivity-specific tests below opt into <see cref="ConnectivityHandler"/>/
    /// <see cref="IConnectivityTracker"/> wiring.
    /// </summary>
    private StubHttpMessageHandler RegisterCommonServicesWithConnectivity(FakeOrderBackend backend, IConnectivityTracker connectivity, DraftOrderStore? draftStore = null)
    {
        var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
        deviceStore.SaveAsync(SampleDevice()).AsTask().Wait();
        Services.AddSingleton<IDeviceContextStore>(deviceStore);

        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        sessionStore.SaveAsync(new SessionState(
            "token", DateTimeOffset.UtcNow.AddHours(1), Guid.NewGuid(), "Jane Staff", ["StaffPin"], ["orders.manage"], TerminalId)).AsTask().Wait();
        Services.AddSingleton<IAuthSessionStore>(sessionStore);

        Services.AddSingleton<IDraftOrderStore>(draftStore ?? RegisterDraftStore());
        Services.AddSingleton(connectivity);

        var stub = new StubHttpMessageHandler { Respond = backend.Respond };
        var handler = new ConnectivityHandler(connectivity) { InnerHandler = stub };
        Services.AddSingleton(new DaxaApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://test/") }));
        return stub;
    }

    [Fact]
    public void WhenMenuLoadFailsWithNetworkFailure_ShowsConnectionLostMessage_AndRetryButton()
    {
        var backend = new FakeOrderBackend();
        var stub = RegisterCommonServicesWithConnectivity(backend, new ConnectivityTracker());
        stub.ThrowNetworkFailure = true;

        var cut = RenderComponent<Sales>();

        cut.WaitForAssertion(() => Assert.Contains(ApiErrorMessages.ConnectionLost, cut.Markup));
        Assert.NotEmpty(cut.FindAll("#retry-load"));
    }

    [Fact]
    public void RetryButton_OnceConnectivityRestored_LoadsTheMenu()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        var stub = RegisterCommonServicesWithConnectivity(backend, new ConnectivityTracker());
        stub.ThrowNetworkFailure = true;
        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#retry-load")));

        stub.ThrowNetworkFailure = false;
        cut.Find("#retry-load").Click();

        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));
    }

    [Fact]
    public void NetworkFailureOnLoad_ShowsOfflineBanner()
    {
        var backend = new FakeOrderBackend();
        var stub = RegisterCommonServicesWithConnectivity(backend, new ConnectivityTracker());
        stub.ThrowNetworkFailure = true;

        var cut = RenderComponent<Sales>();

        cut.WaitForAssertion(() => Assert.Contains("Reconnecting", cut.Markup));
    }

    [Fact]
    public void NetworkFailureAddingALine_ShowsNotConfirmedMessage_AndOffersRetry()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        var connectivity = new ConnectivityTracker();
        var stub = RegisterCommonServicesWithConnectivity(backend, connectivity);
        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));

        stub.ThrowNetworkFailure = true;
        cut.Find("button.btn-outline-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains(ApiErrorMessages.AddLineNotConfirmed, cut.Markup));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#retry-add-line")));
        Assert.Contains("No items selected yet.", cut.Markup);
        Assert.Null(backend.Order);
    }

    [Fact]
    public void RetryPendingAddLine_OnceConnectivityRestored_CompletesTheAdd()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        var connectivity = new ConnectivityTracker();
        var stub = RegisterCommonServicesWithConnectivity(backend, connectivity);
        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));

        stub.ThrowNetworkFailure = true;
        cut.Find("button.btn-outline-primary").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#retry-add-line")));

        stub.ThrowNetworkFailure = false;
        cut.Find("#retry-add-line").Click();

        cut.WaitForAssertion(() => Assert.Contains("$5.50", cut.Markup));
        Assert.Single(backend.Order!.Lines);
        Assert.Empty(cut.FindAll("#retry-add-line"));
    }

    [Fact]
    public void ConnectivityRestoring_WithoutRetryClick_DoesNotAutoReplayTheAdd()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        var connectivity = new ConnectivityTracker();
        var stub = RegisterCommonServicesWithConnectivity(backend, connectivity);
        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));

        stub.ThrowNetworkFailure = true;
        cut.Find("button.btn-outline-primary").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#retry-add-line")));

        stub.ThrowNetworkFailure = false;
        connectivity.ReportOnline();

        Assert.Null(backend.Order);
        Assert.Contains("No items selected yet.", cut.Markup);
        Assert.NotEmpty(cut.FindAll("#retry-add-line"));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, ApiErrorMessages.SessionExpired)]
    [InlineData(HttpStatusCode.Forbidden, ApiErrorMessages.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, "Could not add that item to the order.")]
    public void HttpRejectionAddingALine_IsNotOfferedAsRetryable(HttpStatusCode statusCode, string expectedMessage)
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        var connectivity = new ConnectivityTracker();
        var stub = RegisterCommonServicesWithConnectivity(backend, connectivity);
        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));

        var originalRespond = stub.Respond;
        stub.Respond = request =>
            request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.EndsWith("/lines", StringComparison.Ordinal)
                ? new HttpResponseMessage(statusCode)
                : originalRespond(request);

        cut.Find("button.btn-outline-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains(expectedMessage, cut.Markup));
        Assert.Empty(cut.FindAll("#retry-add-line"));
    }

    [Fact]
    public void AnotherTabChangesTheDraftPointer_ShowsRefreshPrompt()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        var watcher = new FakeDraftPointerWatcher();
        RegisterCommonServices(backend, draftWatcher: watcher);

        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));
        Assert.Equal(watcher.WatchedKey, RegisterDraftStore().KeyFor(DeviceId));

        watcher.RaiseChangedElsewhere();

        cut.WaitForAssertion(() => Assert.Contains(ApiErrorMessages.DraftChangedElsewhere, cut.Markup));
        Assert.NotEmpty(cut.FindAll("#refresh-draft"));
    }

    [Fact]
    public void RefreshButton_AfterDraftChangedElsewhere_ReloadsFromServer_AndClearsThePrompt()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        var watcher = new FakeDraftPointerWatcher();
        RegisterCommonServices(backend, draftWatcher: watcher);
        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));
        watcher.RaiseChangedElsewhere();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#refresh-draft")));

        cut.Find("#refresh-draft").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("#refresh-draft")));
    }

    [Fact]
    public async Task DraftPointerDivergedFromInMemoryOrder_BlocksAddLine_AndShowsPrompt()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        var draftStore = RegisterDraftStore();
        RegisterCommonServices(backend, draftStore);
        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));
        cut.Find("button.btn-outline-primary").Click();
        cut.WaitForAssertion(() => Assert.Contains("$5.50", cut.Markup));

        // Simulate another tab moving the shared pointer to a different order.
        await draftStore.SaveOrderIdAsync(DeviceId, Guid.NewGuid());

        cut.Find("button.btn-outline-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains(ApiErrorMessages.DraftChangedElsewhere, cut.Markup));
        Assert.Single(backend.Order!.Lines);
    }

    [Fact]
    public async Task DraftPointerAlreadyPointsElsewhere_WhenNoLocalOrderYet_BlocksOpeningASecondOrder()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        var draftStore = RegisterDraftStore();
        RegisterCommonServices(backend, draftStore);
        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));

        // Another tab already opened an order and saved its pointer before this tab's first tap.
        await draftStore.SaveOrderIdAsync(DeviceId, Guid.NewGuid());

        cut.Find("button.btn-outline-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains(ApiErrorMessages.DraftChangedElsewhere, cut.Markup));
        Assert.Null(backend.Order);
    }
}
