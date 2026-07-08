using Bunit;
using DaxaPos.Web.Shared;
using DaxaPos.Web.State;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Web.Tests.Shared;

/// <summary>
/// PLAN-0007 Milestone A. Resolves <see cref="IConnectivityTracker"/> defensively (via
/// <see cref="IServiceProvider.GetService{T}"/>, not <c>@inject</c>'s <c>GetRequiredService</c>) so
/// pages/tests that never register a tracker keep rendering — this component must never be the
/// reason an unrelated test starts failing (see worker notes).
/// </summary>
public class ConnectivityBannerTests : TestContext
{
    [Fact]
    public void NoTrackerRegistered_RendersNothing()
    {
        var cut = RenderComponent<ConnectivityBanner>();

        Assert.Equal(string.Empty, cut.Markup.Trim());
    }

    [Fact]
    public void TrackerOnline_RendersNothing()
    {
        Services.AddSingleton<IConnectivityTracker>(new ConnectivityTracker());

        var cut = RenderComponent<ConnectivityBanner>();

        Assert.Equal(string.Empty, cut.Markup.Trim());
    }

    [Fact]
    public void TrackerReconnecting_ShowsReconnectingMessage()
    {
        var tracker = new ConnectivityTracker();
        tracker.ReportNetworkFailure();
        Services.AddSingleton<IConnectivityTracker>(tracker);

        var cut = RenderComponent<ConnectivityBanner>();

        Assert.Contains("Reconnecting", cut.Markup);
    }

    [Fact]
    public void TrackerOffline_ShowsOfflineMessage()
    {
        var tracker = new ConnectivityTracker();
        tracker.ReportNetworkFailure();
        tracker.ReportNetworkFailure();
        Services.AddSingleton<IConnectivityTracker>(tracker);

        var cut = RenderComponent<ConnectivityBanner>();

        Assert.Contains("Offline", cut.Markup);
    }

    [Fact]
    public void TrackerStatusChangesAfterRender_UpdatesWithoutManualRefresh()
    {
        var tracker = new ConnectivityTracker();
        Services.AddSingleton<IConnectivityTracker>(tracker);
        var cut = RenderComponent<ConnectivityBanner>();
        Assert.Equal(string.Empty, cut.Markup.Trim());

        tracker.ReportNetworkFailure();

        cut.WaitForAssertion(() => Assert.Contains("Reconnecting", cut.Markup));
    }
}
