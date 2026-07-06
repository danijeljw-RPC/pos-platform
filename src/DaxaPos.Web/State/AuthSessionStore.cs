using DaxaPos.Web.Storage;

namespace DaxaPos.Web.State;

public sealed class AuthSessionStore(IBrowserStorage storage) : IAuthSessionStore
{
    private const string StorageKey = "daxa.session.v1";

    private readonly JsonLocalStore<SessionState> _store = new(storage, StorageKey);

    public SessionState? Current => _store.Current;

    public event Action? Changed
    {
        add => _store.Changed += value;
        remove => _store.Changed -= value;
    }

    public ValueTask EnsureLoadedAsync() => _store.EnsureLoadedAsync();

    public ValueTask SaveAsync(SessionState session) => _store.SaveAsync(session);

    public ValueTask ClearAsync() => _store.ClearAsync();
}
