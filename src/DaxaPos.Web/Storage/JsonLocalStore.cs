using System.Text.Json;

namespace DaxaPos.Web.Storage;

/// <summary>
/// JSON-backed persistence over <see cref="IBrowserStorage"/> with an in-memory cache. Shared by
/// <c>DeviceContextStore</c> and <c>AuthSessionStore</c>, which have identical load/save/clear
/// shapes but distinct payload types and public interfaces.
/// </summary>
public sealed class JsonLocalStore<T>(IBrowserStorage storage, string key)
    where T : class
{
    private bool _loaded;

    public T? Current { get; private set; }

    public event Action? Changed;

    /// <summary>
    /// Restores <see cref="Current"/> from storage on first call; a no-op afterwards so repeated
    /// calls (e.g. from multiple components' <c>OnInitializedAsync</c>) don't re-read storage.
    /// </summary>
    public async ValueTask EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        var json = await storage.GetItemAsync(key);
        Current = json is null ? null : JsonSerializer.Deserialize<T>(json);
        _loaded = true;
    }

    public async ValueTask SaveAsync(T value)
    {
        await storage.SetItemAsync(key, JsonSerializer.Serialize(value));
        Current = value;
        _loaded = true;
        Changed?.Invoke();
    }

    public async ValueTask ClearAsync()
    {
        await storage.RemoveItemAsync(key);
        Current = null;
        _loaded = true;
        Changed?.Invoke();
    }
}
