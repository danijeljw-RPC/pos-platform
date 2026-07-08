using DaxaPos.Web.State;

namespace DaxaPos.Web.Tests.Fakes;

/// <summary>
/// PLAN-0007 Milestone D: test double for <see cref="IDraftPointerWatcher"/>. bUnit has no real
/// browser to dispatch a native <c>storage</c> event, so tests call <see cref="RaiseChangedElsewhere"/>
/// directly to simulate another tab changing the watched key.
/// </summary>
public sealed class FakeDraftPointerWatcher : IDraftPointerWatcher
{
    public event Action? ChangedElsewhere;

    public string? WatchedKey { get; private set; }

    public ValueTask WatchAsync(string key)
    {
        WatchedKey = key;
        return ValueTask.CompletedTask;
    }

    public void RaiseChangedElsewhere() => ChangedElsewhere?.Invoke();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
