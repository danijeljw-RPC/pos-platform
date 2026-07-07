using DaxaPos.Web.State;
using DaxaPos.Web.Tests.Fakes;

namespace DaxaPos.Web.Tests.State;

public class BackOfficeSessionStoreTests
{
    private static BackOfficeSessionState SampleSession(DateTimeOffset expiresAtUtc) => new(
        SessionToken: "admin-token",
        ExpiresAtUtc: expiresAtUtc,
        Email: "admin@example.com",
        Roles: ["SystemAdmin"],
        Permissions: ["devices.register"]);

    [Fact]
    public async Task SaveAsync_ThenLoadInNewStore_RestoresSession()
    {
        var storage = new InMemoryBrowserStorage();
        var session = SampleSession(DateTimeOffset.UtcNow.AddHours(1));
        var store = new BackOfficeSessionStore(storage);

        await store.SaveAsync(session);

        var restored = new BackOfficeSessionStore(storage);
        await restored.EnsureLoadedAsync();

        // Assert.Equivalent, not Assert.Equal: List<string> (from JSON deserialization) has no
        // value equality, so a record-equality comparison against the original array-backed
        // collections would fail even though the round-trip is correct.
        Assert.Equivalent(session, restored.Current);
    }

    [Fact]
    public async Task ClearAsync_RemovesSession()
    {
        var storage = new InMemoryBrowserStorage();
        var store = new BackOfficeSessionStore(storage);
        await store.SaveAsync(SampleSession(DateTimeOffset.UtcNow.AddHours(1)));

        await store.ClearAsync();

        Assert.Null(store.Current);
    }

    [Fact]
    public void IsExpired_WhenNowAfterExpiry_ReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow;
        var session = SampleSession(now.AddHours(-1));

        Assert.True(session.IsExpired(now));
    }

    [Fact]
    public void IsExpired_WhenNowBeforeExpiry_ReturnsFalse()
    {
        var now = DateTimeOffset.UtcNow;
        var session = SampleSession(now.AddHours(1));

        Assert.False(session.IsExpired(now));
    }
}
