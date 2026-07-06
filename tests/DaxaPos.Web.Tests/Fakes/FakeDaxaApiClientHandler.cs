using System.Net;
using System.Net.Http.Json;
using DaxaPos.Web.Api;

namespace DaxaPos.Web.Tests.Fakes;

/// <summary>Builds a real <see cref="DaxaApiClient"/> backed by a <see cref="StubHttpMessageHandler"/> for component tests.</summary>
public static class FakeDaxaApiClientHandler
{
    public static DaxaApiClient BuildSuccess<T>(T responseBody, out StubHttpMessageHandler stub)
    {
        stub = new StubHttpMessageHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(responseBody) },
        };
        return new DaxaApiClient(new HttpClient(stub) { BaseAddress = new Uri("http://test/") });
    }

    public static DaxaApiClient BuildFailure(HttpStatusCode statusCode, out StubHttpMessageHandler stub)
    {
        stub = new StubHttpMessageHandler { Respond = _ => new HttpResponseMessage(statusCode) };
        return new DaxaApiClient(new HttpClient(stub) { BaseAddress = new Uri("http://test/") });
    }
}
