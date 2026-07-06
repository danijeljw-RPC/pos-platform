using DaxaPos.Web.Storage;
using DaxaPos.Web.Tests.Fakes;

namespace DaxaPos.Web.Tests.Storage;

public class JsonLocalStoreTests
{
    private sealed record Payload(string Value);

    [Fact]
    public async Task EnsureLoadedAsync_WhenStorageEmpty_LeavesCurrentNull()
    {
        var store = new JsonLocalStore<Payload>(new InMemoryBrowserStorage(), "key");

        await store.EnsureLoadedAsync();

        Assert.Null(store.Current);
    }

    [Fact]
    public async Task SaveAsync_PersistsAndRestoresAcrossInstances()
    {
        var storage = new InMemoryBrowserStorage();
        var store = new JsonLocalStore<Payload>(storage, "key");

        await store.SaveAsync(new Payload("hello"));

        var reloaded = new JsonLocalStore<Payload>(storage, "key");
        await reloaded.EnsureLoadedAsync();

        Assert.Equal("hello", reloaded.Current?.Value);
    }

    [Fact]
    public async Task ClearAsync_RemovesPersistedValue()
    {
        var storage = new InMemoryBrowserStorage();
        var store = new JsonLocalStore<Payload>(storage, "key");
        await store.SaveAsync(new Payload("hello"));

        await store.ClearAsync();

        Assert.Null(store.Current);
        var reloaded = new JsonLocalStore<Payload>(storage, "key");
        await reloaded.EnsureLoadedAsync();
        Assert.Null(reloaded.Current);
    }

    [Fact]
    public async Task SaveAsync_RaisesChanged()
    {
        var store = new JsonLocalStore<Payload>(new InMemoryBrowserStorage(), "key");
        var raised = false;
        store.Changed += () => raised = true;

        await store.SaveAsync(new Payload("hello"));

        Assert.True(raised);
    }

    [Fact]
    public async Task ClearAsync_RaisesChanged()
    {
        var store = new JsonLocalStore<Payload>(new InMemoryBrowserStorage(), "key");
        await store.SaveAsync(new Payload("hello"));
        var raised = false;
        store.Changed += () => raised = true;

        await store.ClearAsync();

        Assert.True(raised);
    }
}
