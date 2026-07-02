using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Shared scenario state for <see cref="RbacTests"/>: seeded once per class, because the matrix
/// theories below each fire one HTTP request per protected endpoint and re-seeding the full
/// admin/device/staff chain per case would be needlessly slow.
/// </summary>
public sealed class RbacScenarioFixture : IAsyncLifetime
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    /// <summary>Anonymous client — no Authorization header at all.</summary>
    public HttpClient UnauthenticatedClient { get; private set; } = null!;

    /// <summary>Bearer token that was never issued by the server.</summary>
    public HttpClient GarbageTokenClient { get; private set; } = null!;

    /// <summary>
    /// A real LocalUsernamePassword session whose role is the seeded <c>Staff</c> role — which
    /// deliberately carries zero permission codes, so it is authenticated but authorized for none
    /// of the permission-gated endpoints.
    /// </summary>
    public HttpClient NoPermissionClient { get; private set; } = null!;

    /// <summary>A registered device's token — trusted device context, empty roles/permissions.</summary>
    public HttpClient DeviceClient { get; private set; } = null!;

    /// <summary>A real LocalStaffPin session issued through the full trusted-device login flow.</summary>
    public HttpClient StaffSessionClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
            builder.UseSetting("DeviceRegistration:RateLimitPermitLimit", "1000");
        });

        UnauthenticatedClient = Factory.CreateClient();

        GarbageTokenClient = Factory.CreateClient();
        GarbageTokenClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not-a-token-the-server-ever-issued");

        NoPermissionClient = Factory.CreateClient();
        var noPermissionCaller = await RbacTestSeeder.SeedAsync(NoPermissionClient, "Staff");
        NoPermissionClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", noPermissionCaller.Token);

        // Full trusted-device chain for the device-token and staff-PIN-session scenarios.
        var adminClient = Factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(adminClient, "OrganisationOwner");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);
        var location = await DeviceTestHelper.CreateLocationAsync(adminClient, caller.OrganisationId, "RBAC Matrix Venue");
        var pin = await DeviceTestHelper.CreatePinAsync(adminClient, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(adminClient, pin.Pin);

        DeviceClient = Factory.CreateClient();
        DeviceTestHelper.AuthenticateWithDeviceToken(DeviceClient, device.DeviceToken);

        await StaffTestHelper.CreateStaffMemberAsync(adminClient, location.Id, "RBAC1");
        var staffLogin = await StaffTestHelper.StaffLoginAsync(
            DeviceClient, location.Id, "RBAC1", StaffTestHelper.DefaultPin);

        StaffSessionClient = Factory.CreateClient();
        StaffSessionClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", staffLogin.SessionToken);
    }

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Consolidated cross-method authorization matrix (PLAN-0003 Milestone G, plan step 40). The
/// per-milestone test files prove each endpoint's behaviour for the callers that milestone cared
/// about; this file sweeps every protected endpoint against every authentication posture in one
/// place, driven by a single endpoint inventory — when a later plan adds a protected endpoint, it
/// gets full matrix coverage by adding one line to <see cref="PermissionGatedEndpoints"/>.
/// </summary>
public class RbacTests : IClassFixture<RbacScenarioFixture>
{
    // A syntactically valid id that matches no row. Irrelevant to the matrix outcomes: 401 is
    // produced by authentication middleware and 403 by RequirePermissionFilter, both before any
    // row lookup — which is itself part of what these tests prove (no existence disclosure).
    private const string Id = "00000000-0000-0000-0000-000000000000";

    private readonly RbacScenarioFixture _fixture;

    public RbacTests(RbacScenarioFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Every permission-gated endpoint built by PLAN-0003 (Milestones C–F). All of them are also
    /// <c>rejectStaffPin: true</c>, so this one inventory drives the wrong-permission,
    /// device-token, and staff-PIN-session 403 sweeps as well as the 401 sweeps.
    /// </summary>
    public static TheoryData<string, string> PermissionGatedEndpoints() => new()
    {
        { "POST", "/api/v1/organisations" },
        { "GET", "/api/v1/organisations" },
        { "GET", $"/api/v1/organisations/{Id}" },
        { "PATCH", $"/api/v1/organisations/{Id}" },
        { "POST", $"/api/v1/organisations/{Id}/deactivate" },
        { "POST", $"/api/v1/organisations/{Id}/reactivate" },
        { "POST", "/api/v1/locations" },
        { "GET", "/api/v1/locations" },
        { "GET", $"/api/v1/locations/{Id}" },
        { "PATCH", $"/api/v1/locations/{Id}" },
        { "POST", $"/api/v1/locations/{Id}/deactivate" },
        { "POST", $"/api/v1/locations/{Id}/reactivate" },
        { "POST", "/api/v1/terminals" },
        { "GET", "/api/v1/terminals" },
        { "GET", $"/api/v1/terminals/{Id}" },
        { "PATCH", $"/api/v1/terminals/{Id}" },
        { "POST", $"/api/v1/terminals/{Id}/deactivate" },
        { "POST", $"/api/v1/terminals/{Id}/reactivate" },
        { "GET", "/api/v1/devices" },
        { "POST", $"/api/v1/devices/{Id}/rotate-credential" },
        { "POST", $"/api/v1/devices/{Id}/revoke" },
        { "POST", "/api/v1/device-registration-pins" },
        { "POST", $"/api/v1/device-registration-pins/{Id}/revoke" },
        { "POST", "/api/v1/staff-members" },
        { "GET", "/api/v1/staff-members" },
        { "GET", $"/api/v1/staff-members/{Id}" },
        { "POST", $"/api/v1/staff-members/{Id}/reset-pin" },
        { "POST", $"/api/v1/staff-members/{Id}/roles" },
        { "POST", $"/api/v1/staff-members/{Id}/disable" },
    };

    /// <summary>
    /// The full protected surface: the permission-gated inventory plus the endpoints protected by
    /// authentication alone (<c>RequireAuthorization()</c> without a permission code).
    /// </summary>
    public static TheoryData<string, string> AllProtectedEndpoints()
    {
        var data = PermissionGatedEndpoints();
        data.Add("GET", "/api/v1/auth/me");
        data.Add("POST", "/api/v1/auth/logout");
        data.Add("POST", "/api/v1/auth/staff-pin/login");
        return data;
    }

    [Theory]
    [MemberData(nameof(AllProtectedEndpoints))]
    public async Task Unauthenticated_Request_Returns401(string method, string path)
    {
        var response = await SendAsync(_fixture.UnauthenticatedClient, method, path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AllProtectedEndpoints))]
    public async Task GarbageBearerToken_Returns401(string method, string path)
    {
        var response = await SendAsync(_fixture.GarbageTokenClient, method, path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(PermissionGatedEndpoints))]
    public async Task AuthenticatedSession_WithoutThePermission_Returns403(string method, string path)
    {
        var response = await SendAsync(_fixture.NoPermissionClient, method, path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(PermissionGatedEndpoints))]
    public async Task DeviceToken_Returns403_OnEveryPermissionGatedEndpoint(string method, string path)
    {
        // A device token is trusted device context only — empty roles/permissions by design
        // (ADR-0008): it must never grant user authority by itself.
        var response = await SendAsync(_fixture.DeviceClient, method, path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(PermissionGatedEndpoints))]
    public async Task StaffPinSession_Returns403_OnEveryRejectStaffPinEndpoint(string method, string path)
    {
        // Every permission-gated endpoint in the inventory is rejectStaffPin: true — the ADR-0013
        // rule that a staff PIN session is operational only, never identity/admin management.
        var response = await SendAsync(_fixture.StaffSessionClient, method, path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RevokedSession_Returns401_EverywhereImmediately()
    {
        var client = _fixture.Factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);

        // The revoked token is dead immediately (ADR-0015: DB-validated opaque tokens exist
        // precisely so revocation is instant) — 401 even where the permission would have matched.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/organisations")).StatusCode);
    }

    [Fact]
    public async Task ValidSession_ForAnotherTenant_SeesNothing_AndNeverAnError()
    {
        // Tenant A: a SystemAdmin holding every catalogue permission — authorization is not what
        // stands between it and tenant B's data; the fail-closed tenant filter is.
        var callerAClient = _fixture.Factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(callerAClient, "SystemAdmin");
        callerAClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", callerA.Token);

        // Tenant B: a full resource set — organisation, location, terminal, staff, PIN, device.
        var callerBClient = _fixture.Factory.CreateClient();
        var callerB = await RbacTestSeeder.SeedAsync(callerBClient, "SystemAdmin");
        callerBClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", callerB.Token);
        var locationB = await DeviceTestHelper.CreateLocationAsync(callerBClient, callerB.OrganisationId, "Tenant B Venue");
        var terminalB = await (await callerBClient.PostAsJsonAsync(
                "/api/v1/terminals", new CreateTerminalRequest("Tenant B Terminal", locationB.Id)))
            .Content.ReadFromJsonAsync<TerminalResponse>();
        var staffB = await StaffTestHelper.CreateStaffMemberAsync(callerBClient, locationB.Id, "TB01");
        var pinB = await DeviceTestHelper.CreatePinAsync(callerBClient, locationB.Id);
        var deviceB = await DeviceTestHelper.RegisterDeviceAsync(callerBClient, pinB.Pin);

        // Single-row reads and writes: B's rows are 404 to A — indistinguishable from nonexistent
        // ids, never 403 (no existence disclosure), never 500.
        var attempts = new[]
        {
            await callerAClient.GetAsync($"/api/v1/organisations/{callerB.OrganisationId}"),
            await callerAClient.PatchAsJsonAsync($"/api/v1/organisations/{callerB.OrganisationId}", new UpdateOrganisationRequest("Hijacked")),
            await callerAClient.PostAsync($"/api/v1/organisations/{callerB.OrganisationId}/deactivate", null),
            await callerAClient.GetAsync($"/api/v1/locations/{locationB.Id}"),
            await callerAClient.PatchAsJsonAsync($"/api/v1/locations/{locationB.Id}", new UpdateLocationRequest("Hijacked")),
            await callerAClient.PostAsync($"/api/v1/locations/{locationB.Id}/deactivate", null),
            await callerAClient.GetAsync($"/api/v1/terminals/{terminalB!.Id}"),
            await callerAClient.GetAsync($"/api/v1/staff-members/{staffB.Id}"),
            await callerAClient.PostAsync($"/api/v1/staff-members/{staffB.Id}/reset-pin", null),
            await callerAClient.PostAsync($"/api/v1/staff-members/{staffB.Id}/disable", null),
            await callerAClient.PostAsync($"/api/v1/devices/{deviceB.DeviceId}/rotate-credential", null),
            await callerAClient.PostAsync($"/api/v1/devices/{deviceB.DeviceId}/revoke", null),
            await callerAClient.PostAsync($"/api/v1/device-registration-pins/{pinB.Id}/revoke", null),
        };

        Assert.All(attempts, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));

        // Lists: 200 with B's rows absent — the fail-closed filter yields empty, not an error.
        var organisations = await callerAClient.GetFromJsonAsync<List<OrganisationResponse>>("/api/v1/organisations");
        Assert.DoesNotContain(organisations!, o => o.Id == callerB.OrganisationId);

        var locations = await callerAClient.GetFromJsonAsync<List<LocationResponse>>("/api/v1/locations");
        Assert.DoesNotContain(locations!, l => l.Id == locationB.Id);

        var terminals = await callerAClient.GetFromJsonAsync<List<TerminalResponse>>("/api/v1/terminals");
        Assert.DoesNotContain(terminals!, t => t.Id == terminalB.Id);

        var staffMembers = await callerAClient.GetFromJsonAsync<List<StaffMemberResponse>>("/api/v1/staff-members");
        Assert.DoesNotContain(staffMembers!, s => s.Id == staffB.Id);

        var devices = await callerAClient.GetFromJsonAsync<List<DeviceResponse>>("/api/v1/devices");
        Assert.DoesNotContain(devices!, d => d.Id == deviceB.DeviceId);
    }

    /// <summary>
    /// Sends <c>{}</c> as the JSON body on non-GET requests: minimal-API body binding runs before
    /// endpoint filters, so a missing body would surface as 400 and mask the 401/403 under test.
    /// </summary>
    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Parse(method), path);

        if (method is not "GET")
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        return client.SendAsync(request);
    }
}
