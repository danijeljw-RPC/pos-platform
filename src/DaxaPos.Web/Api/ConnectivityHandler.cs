using DaxaPos.Web.State;

namespace DaxaPos.Web.Api;

/// <summary>
/// Observes every request's transport-level outcome and reports it to
/// <see cref="IConnectivityTracker"/>, independent of <see cref="DaxaApiClient"/>'s own
/// <see cref="ApiResult{T}"/> mapping (PLAN-0007 Milestone A). Any HTTP response — even a
/// 401/403/404/500 — proves the server was reached, so only a transport-level
/// <see cref="HttpRequestException"/> counts as a connectivity failure. Registered alongside
/// <see cref="AuthHeaderHandler"/> in the same <c>HttpClient</c> pipeline (see Program.cs); rethrows
/// so <see cref="DaxaApiClient"/>'s existing catch blocks are unaffected.
/// </summary>
public sealed class ConnectivityHandler(IConnectivityTracker tracker) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            tracker.ReportOnline();
            return response;
        }
        catch (HttpRequestException)
        {
            tracker.ReportNetworkFailure();
            throw;
        }
    }
}
