namespace DaxaPos.Web.State;

public interface IBackOfficeSessionStore
{
    BackOfficeSessionState? Current { get; }

    event Action? Changed;

    ValueTask EnsureLoadedAsync();

    ValueTask SaveAsync(BackOfficeSessionState session);

    ValueTask ClearAsync();
}
