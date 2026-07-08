namespace DaxaPos.Web.Tests.Fakes;

/// <summary>
/// Terminal (non-forwarding) handler for <see cref="DaxaApiClient"/>/<see cref="AuthHeaderHandler"/>
/// tests: returns whatever <see cref="Respond"/> produces, or throws
/// <see cref="HttpRequestException"/> when <see cref="ThrowNetworkFailure"/> is set, and records the
/// last request it saw so tests can assert on headers.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } = _ => new HttpResponseMessage(System.Net.HttpStatusCode.OK);

    public bool ThrowNetworkFailure { get; set; }

    /// <summary>
    /// PLAN-0007 Milestone C: fails only requests whose path ends with this suffix, leaving the rest
    /// of the API reachable — needed to simulate a receipt-fetch-specific network failure right after
    /// a payment POST that must itself still succeed.
    /// </summary>
    public string? FailingPathSuffix { get; set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;

        if (ThrowNetworkFailure || (FailingPathSuffix is not null && request.RequestUri!.AbsolutePath.EndsWith(FailingPathSuffix, StringComparison.Ordinal)))
        {
            throw new HttpRequestException("Simulated network failure.");
        }

        return Task.FromResult(Respond(request));
    }
}
