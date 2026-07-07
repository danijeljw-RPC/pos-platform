using DaxaPos.Web.Storage;

namespace DaxaPos.Web.State;

public sealed class BackOfficeSessionStore(IBrowserStorage storage) : IBackOfficeSessionStore
{
    private const string StorageKey = "daxa.backoffice-session.v1";

    private readonly JsonLocalStore<BackOfficeSessionState> _store = new(storage, StorageKey);

    public BackOfficeSessionState? Current => _store.Current;

    public event Action? Changed
    {
        add => _store.Changed += value;
        remove => _store.Changed -= value;
    }

    public ValueTask EnsureLoadedAsync() => _store.EnsureLoadedAsync();

    public ValueTask SaveAsync(BackOfficeSessionState session) => _store.SaveAsync(session);

    public ValueTask ClearAsync() => _store.ClearAsync();
}
