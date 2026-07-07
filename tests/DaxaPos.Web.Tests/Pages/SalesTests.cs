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

    private StubHttpMessageHandler RegisterCommonServices(FakeOrderBackend backend, DraftOrderStore? draftStore = null, bool withTerminal = true)
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
    public void WhenMenuLoadFails_ShowsErrorMessage()
    {
        var deviceStore = new DeviceContextStore(new InMemoryBrowserStorage());
        deviceStore.SaveAsync(SampleDevice()).AsTask().Wait();
        Services.AddSingleton<IDeviceContextStore>(deviceStore);

        var sessionStore = new AuthSessionStore(new InMemoryBrowserStorage());
        sessionStore.SaveAsync(new SessionState(
            "token", DateTimeOffset.UtcNow.AddHours(1), Guid.NewGuid(), "Jane Staff", ["StaffPin"], ["orders.manage"], TerminalId)).AsTask().Wait();
        Services.AddSingleton<IAuthSessionStore>(sessionStore);
        Services.AddSingleton<IDraftOrderStore>(RegisterDraftStore());
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildFailure(HttpStatusCode.NotFound, out _));

        var cut = RenderComponent<Sales>();

        cut.WaitForAssertion(() => Assert.Contains("Could not load the menu", cut.Markup));
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
        Assert.Contains("$5.50", cut.Markup);
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
    public void ClearOrder_VoidsTheOrderServerSide_AndClearsTheStoredPointer()
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
        Assert.Null(draftStore.GetOrderIdAsync(DeviceId).AsTask().GetAwaiter().GetResult());
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
    public void OnRefresh_ARestorableStoredOrder_RebuildsTheCartFromTheServer()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        var existingLine = new OrderLineResult(Guid.NewGuid(), productId, 1, "Flat White", 5.5m, 5.5m, null, OrderLineStatusResult.Active, []);
        var existingOrder = new OrderResult(Guid.NewGuid(), TerminalId, OrderStatusResult.Open, 5.5m, 0m, 5.5m, [existingLine]);
        backend.Order = existingOrder;
        var draftStore = RegisterDraftStore();
        draftStore.SaveOrderIdAsync(DeviceId, existingOrder.Id).AsTask().Wait();
        RegisterCommonServices(backend, draftStore);

        var cut = RenderComponent<Sales>();

        cut.WaitForAssertion(() => Assert.Contains("$5.50", cut.Markup));
        Assert.DoesNotContain("No items selected yet.", cut.Markup);
    }

    [Fact]
    public void OnRefresh_AStoredOrderThatNoLongerExists_ClearsThePointer_AndStartsEmpty()
    {
        var productId = Guid.NewGuid();
        var backend = new FakeOrderBackend { Menu = SimpleMenu(productId, "Flat White", 5.5m) };
        backend.RegisterProduct(productId, "Flat White", 5.5m);
        // backend.Order stays null -> GetOrderAsync(orderId) 404s for whatever id is stored.
        var draftStore = RegisterDraftStore();
        var staleOrderId = Guid.NewGuid();
        draftStore.SaveOrderIdAsync(DeviceId, staleOrderId).AsTask().Wait();
        RegisterCommonServices(backend, draftStore);

        var cut = RenderComponent<Sales>();

        cut.WaitForAssertion(() => Assert.Contains("No items selected yet.", cut.Markup));
        Assert.Null(draftStore.GetOrderIdAsync(DeviceId).AsTask().GetAwaiter().GetResult());
    }

    private static T Also<T>(T value) => value;
}
