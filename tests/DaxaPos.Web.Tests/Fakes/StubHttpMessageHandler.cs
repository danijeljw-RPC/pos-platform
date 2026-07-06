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

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;

        if (ThrowNetworkFailure)
        {
            throw new HttpRequestException("Simulated network failure.");
        }

        return Task.FromResult(Respond(request));
    }
}
