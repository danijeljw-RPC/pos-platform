using DaxaPos.Web.Storage;

namespace DaxaPos.Web.State;

/// <summary>
/// <see cref="IDraftOrderStore"/> over <see cref="IBrowserStorage"/>. No in-memory cache like
/// <see cref="JsonLocalStore{T}"/> — called at most once per page load and once per order-open, so
/// a fixed per-device key computed at call time is simpler than pre-wiring a cache per possible
/// device id.
/// </summary>
public sealed class DraftOrderStore(IBrowserStorage storage) : IDraftOrderStore
{
    public async ValueTask<Guid?> GetOrderIdAsync(Guid deviceId)
    {
        var raw = await storage.GetItemAsync(KeyFor(deviceId));
        return Guid.TryParse(raw, out var orderId) ? orderId : null;
    }

    public ValueTask SaveOrderIdAsync(Guid deviceId, Guid orderId) =>
        storage.SetItemAsync(KeyFor(deviceId), orderId.ToString());

    public ValueTask ClearAsync(Guid deviceId) =>
        storage.RemoveItemAsync(KeyFor(deviceId));

    public string KeyFor(Guid deviceId) => $"daxa.sales-draft.v1.{deviceId}";
}
