using System.Net;
using System.Net.Http.Json;
using Bunit;
using DaxaPos.Web.Api;
using DaxaPos.Web.Pages;
using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Web.Tests.Pages;

public class SalesTests : TestContext
{
    private static DeviceContext SampleDevice(Guid locationId) => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), locationId, "KioskBrowser", "Front Counter", "device-token");

    private static ResolvedMenuResult SampleMenu(Guid locationId, Guid productId, string productName, decimal price) => new(
        locationId,
        [
            new ResolvedMenuSectionResult(Guid.NewGuid(), Guid.NewGuid(), "Coffee", 0,
            [
                new ResolvedMenuItemResult(productId, productName, 0, price, true, "AU_GST_10"),
            ]),
        ]);

    private DeviceContextStore RegisterDeviceStore(Guid locationId)
    {
        var store = new DeviceContextStore(new InMemoryBrowserStorage());
        store.SaveAsync(SampleDevice(locationId)).AsTask().Wait();
        Services.AddSingleton<IDeviceContextStore>(store);
        return store;
    }

    [Fact]
    public void WhenNoDeviceRegistered_ShowsPromptInsteadOfMenu()
    {
        Services.AddSingleton<IDeviceContextStore>(new DeviceContextStore(new InMemoryBrowserStorage()));
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildFailure(HttpStatusCode.OK, out _));

        var cut = RenderComponent<Sales>();

        Assert.Contains("No device is registered", cut.Markup);
    }

    [Fact]
    public void WhenMenuLoadFails_ShowsErrorMessage()
    {
        var locationId = Guid.NewGuid();
        RegisterDeviceStore(locationId);
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildFailure(HttpStatusCode.NotFound, out _));

        var cut = RenderComponent<Sales>();

        cut.WaitForAssertion(() => Assert.Contains("Could not load the menu", cut.Markup));
    }

    [Fact]
    public void WhenMenuHasNoItems_ShowsEmptyState()
    {
        var locationId = Guid.NewGuid();
        RegisterDeviceStore(locationId);
        var emptyMenu = new ResolvedMenuResult(locationId, []);
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildSuccess(emptyMenu, out _));

        var cut = RenderComponent<Sales>();

        cut.WaitForAssertion(() => Assert.Contains("No menu items are available", cut.Markup));
    }

    [Fact]
    public void TappingMenuTile_AddsLineToOrderPanel()
    {
        var locationId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        RegisterDeviceStore(locationId);
        var menu = SampleMenu(locationId, productId, "Flat White", 5.5m);
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildSuccess(menu, out var stub));

        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));
        Assert.Contains($"locationId={locationId}", stub.LastRequest!.RequestUri!.Query);

        cut.Find("button.btn-outline-primary").Click();

        Assert.DoesNotContain("No items selected yet.", cut.Markup);
        Assert.Contains("Draft subtotal (estimate)", cut.Markup);
    }

    [Fact]
    public void TappingMenuTileTwice_IncrementsQuantityInsteadOfDuplicatingLine()
    {
        var locationId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        RegisterDeviceStore(locationId);
        var menu = SampleMenu(locationId, productId, "Flat White", 5.5m);
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildSuccess(menu, out _));

        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));

        var tile = cut.Find("button.btn-outline-primary");
        tile.Click();
        tile.Click();

        // Two taps, one line, quantity 2 — the "2" appears once as the line's quantity, and the
        // draft subtotal reflects 2x unit price ($11.00), not two separate $5.50 lines.
        Assert.Contains("$11.00", cut.Markup);
    }

    [Fact]
    public void DecrementingToZero_RemovesTheLine()
    {
        var locationId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        RegisterDeviceStore(locationId);
        var menu = SampleMenu(locationId, productId, "Flat White", 5.5m);
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildSuccess(menu, out _));

        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));
        cut.Find("button.btn-outline-primary").Click();

        // "Clear order", "-", and "+" all share the same Bootstrap classes, so disambiguate by
        // text content rather than a CSS selector.
        var decrementButton = cut.FindAll("button").Single(b => b.TextContent.Trim() == "-");
        decrementButton.Click();

        Assert.Contains("No items selected yet.", cut.Markup);
    }

    [Fact]
    public void ClearOrder_EmptiesTheDraft()
    {
        var locationId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        RegisterDeviceStore(locationId);
        var menu = SampleMenu(locationId, productId, "Flat White", 5.5m);
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildSuccess(menu, out _));

        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));
        cut.Find("button.btn-outline-primary").Click();
        cut.WaitForAssertion(() => Assert.Contains("Clear order", cut.Markup));

        var clearButton = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Clear order");
        clearButton.Click();

        Assert.Contains("No items selected yet.", cut.Markup);
    }

    [Fact]
    public void EnteringANote_UpdatesLineState()
    {
        var locationId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        RegisterDeviceStore(locationId);
        var menu = SampleMenu(locationId, productId, "Flat White", 5.5m);
        Services.AddSingleton(FakeDaxaApiClientHandler.BuildSuccess(menu, out _));

        var cut = RenderComponent<Sales>();
        cut.WaitForAssertion(() => Assert.Contains("Flat White", cut.Markup));
        cut.Find("button.btn-outline-primary").Click();

        cut.Find("input").Change("Extra hot");

        Assert.Contains("Extra hot", cut.Find("input").GetAttribute("value"));
    }
}
