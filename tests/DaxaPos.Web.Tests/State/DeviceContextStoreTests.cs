using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;

namespace DaxaPos.Web.Tests.State;

public class DeviceContextStoreTests
{
    private static DeviceContext SampleDevice() => new(
        DeviceId: Guid.NewGuid(),
        TenantId: Guid.NewGuid(),
        OrganisationId: Guid.NewGuid(),
        LocationId: Guid.NewGuid(),
        DeviceType: "KioskBrowser",
        Name: "Front Counter",
        DeviceToken: "credential-id.secret");

    [Fact]
    public async Task SaveAsync_ThenLoadInNewStore_RestoresDeviceContext()
    {
        var storage = new InMemoryBrowserStorage();
        var device = SampleDevice();
        var store = new DeviceContextStore(storage);

        await store.SaveAsync(device);

        var restored = new DeviceContextStore(storage);
        await restored.EnsureLoadedAsync();

        Assert.Equal(device, restored.Current);
    }

    [Fact]
    public async Task ClearAsync_RemovesDeviceContext()
    {
        var storage = new InMemoryBrowserStorage();
        var store = new DeviceContextStore(storage);
        await store.SaveAsync(SampleDevice());

        await store.ClearAsync();

        Assert.Null(store.Current);
    }
}
