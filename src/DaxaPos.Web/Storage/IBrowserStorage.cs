namespace DaxaPos.Web.Storage;

/// <summary>
/// Thin abstraction over browser <c>localStorage</c> so device-context/session persistence logic
/// can be unit-tested with an in-memory fake instead of a real JS runtime.
/// </summary>
public interface IBrowserStorage
{
    ValueTask<string?> GetItemAsync(string key);

    ValueTask SetItemAsync(string key, string value);

    ValueTask RemoveItemAsync(string key);
}
