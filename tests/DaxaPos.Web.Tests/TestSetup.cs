using System.Runtime.CompilerServices;

namespace DaxaPos.Web.Tests;

/// <summary>
/// bUnit's 1s default <c>WaitForAssertion</c>/<c>WaitForElement</c> timeout is tuned for a fast
/// dev machine. GitHub Actions' hosted runners are slow enough at cold start (thread-pool ramp-up,
/// JIT warm-up) that several tests timed out there with a "Check count: 1" render that simply
/// hadn't landed yet — not an actual failure, just not enough wall-clock time. Raise the default
/// once for the whole assembly instead of tuning timeouts test-by-test.
/// </summary>
internal static class TestSetup
{
    [ModuleInitializer]
    internal static void RaiseDefaultWaitTimeoutForSlowCiRunners()
    {
        Bunit.TestContext.DefaultWaitTimeout = TimeSpan.FromSeconds(10);
    }
}
