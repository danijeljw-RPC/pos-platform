using Microsoft.JSInterop;

namespace DaxaPos.Web.State;

/// <summary>
/// <see cref="IDraftPointerWatcher"/> over the browser's native <c>storage</c> event, via
/// <c>wwwroot/js/storageWatch.js</c>. Deliberately not unit-tested directly (thin JS pass-through,
/// same idiom as <see cref="Storage.LocalStorageBrowserStorage"/>) — pages depend on the
/// <see cref="IDraftPointerWatcher"/> interface and are tested against a fake that raises
/// <see cref="ChangedElsewhere"/> directly, since bUnit has no real browser dispatching storage
/// events for a JS module to observe.
/// </summary>
public sealed class JsDraftPointerWatcher(IJSRuntime jsRuntime) : IDraftPointerWatcher
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<JsDraftPointerWatcher>? _selfRef;
    private string? _watchedKey;

    public event Action? ChangedElsewhere;

    public async ValueTask WatchAsync(string key)
    {
        _watchedKey = key;
        _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/storageWatch.js");
        _selfRef ??= DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("subscribe", key, _selfRef);
    }

    [JSInvokable]
    public void OnStorageChangedElsewhere(string changedKey)
    {
        if (changedKey == _watchedKey)
        {
            ChangedElsewhere?.Invoke();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            if (_watchedKey is not null)
            {
                await _module.InvokeVoidAsync("unsubscribe", _watchedKey);
            }

            await _module.DisposeAsync();
        }

        _selfRef?.Dispose();
    }
}
