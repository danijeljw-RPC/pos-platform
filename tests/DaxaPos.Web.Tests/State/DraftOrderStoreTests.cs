using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;

namespace DaxaPos.Web.Tests.State;

public class DraftOrderStoreTests
{
    [Fact]
    public async Task GetOrderIdAsync_WhenNothingSaved_ReturnsNull()
    {
        var store = new DraftOrderStore(new InMemoryBrowserStorage());

        Assert.Null(await store.GetOrderIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SaveThenGet_RoundTrips_TheOrderId()
    {
        var store = new DraftOrderStore(new InMemoryBrowserStorage());
        var deviceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await store.SaveOrderIdAsync(deviceId, orderId);

        Assert.Equal(orderId, await store.GetOrderIdAsync(deviceId));
    }

    [Fact]
    public async Task DifferentDeviceIds_NeverCollide()
    {
        var store = new DraftOrderStore(new InMemoryBrowserStorage());
        var deviceA = Guid.NewGuid();
        var deviceB = Guid.NewGuid();
        var orderForA = Guid.NewGuid();

        await store.SaveOrderIdAsync(deviceA, orderForA);

        Assert.Equal(orderForA, await store.GetOrderIdAsync(deviceA));
        Assert.Null(await store.GetOrderIdAsync(deviceB));
    }

    [Fact]
    public async Task ClearAsync_RemovesTheSavedOrderId()
    {
        var store = new DraftOrderStore(new InMemoryBrowserStorage());
        var deviceId = Guid.NewGuid();
        await store.SaveOrderIdAsync(deviceId, Guid.NewGuid());

        await store.ClearAsync(deviceId);

        Assert.Null(await store.GetOrderIdAsync(deviceId));
    }
}
