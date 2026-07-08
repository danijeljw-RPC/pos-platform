using DaxaPos.Web.State;

namespace DaxaPos.Web.Tests.State;

public class ConnectivityTrackerTests
{
    [Fact]
    public void InitialStatus_IsOnline()
    {
        var tracker = new ConnectivityTracker();

        Assert.Equal(ConnectivityStatus.Online, tracker.Status);
    }

    [Fact]
    public void FirstNetworkFailure_TransitionsToReconnecting()
    {
        var tracker = new ConnectivityTracker();

        tracker.ReportNetworkFailure();

        Assert.Equal(ConnectivityStatus.Reconnecting, tracker.Status);
    }

    [Fact]
    public void SecondConsecutiveNetworkFailure_TransitionsToOffline()
    {
        var tracker = new ConnectivityTracker();

        tracker.ReportNetworkFailure();
        tracker.ReportNetworkFailure();

        Assert.Equal(ConnectivityStatus.Offline, tracker.Status);
    }

    [Fact]
    public void SuccessAfterFailures_ReturnsToOnline()
    {
        var tracker = new ConnectivityTracker();
        tracker.ReportNetworkFailure();
        tracker.ReportNetworkFailure();

        tracker.ReportOnline();

        Assert.Equal(ConnectivityStatus.Online, tracker.Status);
    }

    [Fact]
    public void StatusChange_RaisesChangedEvent()
    {
        var tracker = new ConnectivityTracker();
        var raised = 0;
        tracker.Changed += () => raised++;

        tracker.ReportNetworkFailure();

        Assert.Equal(1, raised);
    }

    [Fact]
    public void RepeatedSameStatus_DoesNotRaiseChangedAgain()
    {
        var tracker = new ConnectivityTracker();
        tracker.ReportOnline(); // already Online — no-op
        var raised = 0;
        tracker.Changed += () => raised++;

        tracker.ReportOnline();

        Assert.Equal(0, raised);
    }
}
