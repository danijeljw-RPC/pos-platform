namespace DaxaPos.Web.State;

public interface IDeviceContextStore
{
    DeviceContext? Current { get; }

    event Action? Changed;

    ValueTask EnsureLoadedAsync();

    ValueTask SaveAsync(DeviceContext context);

    ValueTask ClearAsync();
}
