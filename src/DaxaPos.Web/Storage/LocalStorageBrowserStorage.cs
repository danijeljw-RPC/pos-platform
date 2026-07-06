using Microsoft.JSInterop;

namespace DaxaPos.Web.Storage;

/// <summary>
/// Calls <c>window.localStorage</c> directly via JS interop's dotted-identifier resolution — no
/// custom JS file needed for these three calls.
/// </summary>
public sealed class LocalStorageBrowserStorage(IJSRuntime jsRuntime) : IBrowserStorage
{
    public ValueTask<string?> GetItemAsync(string key) =>
        jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);

    public ValueTask SetItemAsync(string key, string value) =>
        jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);

    public ValueTask RemoveItemAsync(string key) =>
        jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
}
