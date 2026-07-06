namespace DaxaPos.Web.State;

public interface IAuthSessionStore
{
    SessionState? Current { get; }

    event Action? Changed;

    ValueTask EnsureLoadedAsync();

    ValueTask SaveAsync(SessionState session);

    ValueTask ClearAsync();
}
