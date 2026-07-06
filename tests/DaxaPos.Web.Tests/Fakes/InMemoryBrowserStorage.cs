using DaxaPos.Web.Storage;

namespace DaxaPos.Web.Tests.Fakes;

public sealed class InMemoryBrowserStorage : IBrowserStorage
{
    private readonly Dictionary<string, string> _values = [];

    public ValueTask<string?> GetItemAsync(string key) =>
        ValueTask.FromResult(_values.TryGetValue(key, out var value) ? value : null);

    public ValueTask SetItemAsync(string key, string value)
    {
        _values[key] = value;
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveItemAsync(string key)
    {
        _values.Remove(key);
        return ValueTask.CompletedTask;
    }
}
