using DaxaPos.Web.Storage;

namespace DaxaPos.Web.State;

public sealed class DeviceContextStore(IBrowserStorage storage) : IDeviceContextStore
{
    private const string StorageKey = "daxa.device-context.v1";

    private readonly JsonLocalStore<DeviceContext> _store = new(storage, StorageKey);

    public DeviceContext? Current => _store.Current;

    public event Action? Changed
    {
        add => _store.Changed += value;
        remove => _store.Changed -= value;
    }

    public ValueTask EnsureLoadedAsync() => _store.EnsureLoadedAsync();

    public ValueTask SaveAsync(DeviceContext context) => _store.SaveAsync(context);

    public ValueTask ClearAsync() => _store.ClearAsync();
}
