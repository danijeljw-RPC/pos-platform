namespace DaxaPos.Web.State;

/// <summary>
/// Browser-tab-local connectivity signal (PLAN-0007 Milestone A). Deliberately not persisted to
/// <c>localStorage</c> — unlike <see cref="DeviceContextStore"/>/<see cref="SessionState"/>, this is
/// a transient runtime signal for the current tab, not app state to share across tabs or survive a
/// reload.
/// </summary>
public enum ConnectivityStatus
{
    Online,
    Reconnecting,
    Offline,
}

public interface IConnectivityTracker
{
    ConnectivityStatus Status { get; }

    event Action? Changed;

    void ReportOnline();

    void ReportNetworkFailure();
}

/// <summary>
/// Fed by <see cref="DaxaPos.Web.Api.ConnectivityHandler"/> after every API call. A single network
/// failure is treated as "just started reconnecting" (the very next attempt might succeed); a
/// second consecutive failure escalates to "confirmed offline," so a single transient blip doesn't
/// immediately read as a full outage.
/// </summary>
public sealed class ConnectivityTracker : IConnectivityTracker
{
    private ConnectivityStatus _status = ConnectivityStatus.Online;

    public ConnectivityStatus Status => _status;

    public event Action? Changed;

    public void ReportOnline() => SetStatus(ConnectivityStatus.Online);

    public void ReportNetworkFailure() =>
        SetStatus(_status == ConnectivityStatus.Online ? ConnectivityStatus.Reconnecting : ConnectivityStatus.Offline);

    private void SetStatus(ConnectivityStatus next)
    {
        if (next == _status)
        {
            return;
        }

        _status = next;
        Changed?.Invoke();
    }
}
