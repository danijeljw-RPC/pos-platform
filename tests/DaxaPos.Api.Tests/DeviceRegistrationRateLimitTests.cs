using System.Net;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Rate-limit test for the pre-auth device registration endpoint (ADR-0008: "PIN attempts are
/// rate-limited"), run at the default 10-per-minute permit limit. Deliberately <b>not</b> an
/// <c>IClassFixture</c> — it builds its own factory so tripping the limiter here can never poison
/// the other device test classes (which raise the limit via configuration), and vice versa.
/// </summary>
public class DeviceRegistrationRateLimitTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    [Fact]
    public async Task RepeatedInvalidPins_HitThe429RateLimit()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
        });

        var client = factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 11; attempt++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/v1/device-registration",
                new RegisterDeviceRequest("000000", "WindowsPos"));
            statuses.Add(response.StatusCode);
        }

        // The first requests fail generically as unknown PINs; once the 10-per-minute window is
        // exhausted the limiter answers instead of the endpoint.
        Assert.Contains(HttpStatusCode.Unauthorized, statuses);
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }
}
