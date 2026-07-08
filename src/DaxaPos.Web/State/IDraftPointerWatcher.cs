namespace DaxaPos.Web.State;

/// <summary>
/// PLAN-0007 Milestone D: notifies when another browser tab of the same origin/device changes a
/// watched <c>localStorage</c> key — used to detect that another tab moved the draft order pointer
/// (<see cref="IDraftOrderStore"/>). Wraps the native browser <c>storage</c> event, which fires only
/// in <i>other</i> tabs, never the tab that made the change — exactly the "stale tab" signal this
/// needs, with no polling and no cross-tab coordination protocol of its own.
/// </summary>
public interface IDraftPointerWatcher : IAsyncDisposable
{
    /// <summary>
    /// Raised when the watched key changed in another tab. Never raised for a change made by this
    /// tab itself (the underlying browser event already excludes that case).
    /// </summary>
    event Action? ChangedElsewhere;

    ValueTask WatchAsync(string key);
}
