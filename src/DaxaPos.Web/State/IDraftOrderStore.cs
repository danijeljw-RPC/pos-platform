namespace DaxaPos.Web.State;

/// <summary>
/// Persists only the <c>OrderId</c> of the sales screen's currently open/held real order
/// (PLAN-0006 Milestone C.1) — never order lines, totals, or pricing, since the server remains
/// the authoritative source for those (rebuilt via <c>DaxaApiClient.GetOrderAsync</c>). Keyed per
/// device so multiple terminals sharing a browser profile never collide.
/// </summary>
public interface IDraftOrderStore
{
    ValueTask<Guid?> GetOrderIdAsync(Guid deviceId);

    ValueTask SaveOrderIdAsync(Guid deviceId, Guid orderId);

    ValueTask ClearAsync(Guid deviceId);
}
