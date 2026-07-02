using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Device-token authentication and credential lifecycle tests (PLAN-0003 Milestone E). The core
/// invariant under test: a device token is trusted device context only — it authenticates and
/// yields a partial <c>AuthContext</c> with zero roles/permissions, so every permission-gated
/// endpoint returns 403 for it (ADR-0013: a device credential "must not grant user permissions by
/// itself"). Rotation invalidates the old credential immediately; revocation is terminal.
/// </summary>
public class DeviceEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public DeviceEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
            builder.UseSetting("DeviceRegistration:RateLimitPermitLimit", "1000");
        });
    }

    [Fact]
    public async Task DeviceToken_Authenticates_WithZeroPermissions()
    {
        var adminClient = _factory.CreateClient();
        var (registered, caller, location) = await RegisterDeviceAsync(adminClient);

        var deviceClient = _factory.CreateClient();
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, registered.DeviceToken);

        var response = await deviceClient.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<AuthContextResponse>();
        Assert.NotNull(me);
        Assert.Equal("DeviceToken", me!.AuthMethod);
        Assert.Equal(caller.TenantId, me.TenantId);
        Assert.Equal(caller.OrganisationId, me.OrganisationId);
        Assert.Equal(location.Id, me.LocationId);
        Assert.Equal(registered.DeviceId, me.DeviceId);
        Assert.Null(me.UserId);
        Assert.Null(me.StaffMemberId);
        Assert.Empty(me.Roles);
        Assert.Empty(me.Permissions);
    }

    [Fact]
    public async Task DeviceToken_Receives403_OnEveryPermissionGatedEndpoint()
    {
        var adminClient = _factory.CreateClient();
        var (registered, _, location) = await RegisterDeviceAsync(adminClient);

        var deviceClient = _factory.CreateClient();
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, registered.DeviceToken);

        // A representative endpoint per permission-gated group this plan has built — the device
        // token authenticates (not 401) but must be denied everywhere (403).
        Assert.Equal(HttpStatusCode.Forbidden, (await deviceClient.GetAsync("/api/v1/organisations")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await deviceClient.GetAsync("/api/v1/locations")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await deviceClient.GetAsync("/api/v1/terminals")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await deviceClient.GetAsync("/api/v1/devices")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await deviceClient.PostAsJsonAsync(
            "/api/v1/device-registration-pins", new CreateDeviceRegistrationPinRequest(location.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await deviceClient.PostAsync(
            $"/api/v1/devices/{registered.DeviceId}/revoke", content: null)).StatusCode);
    }

    [Fact]
    public async Task MalformedOrUnknownDeviceToken_Returns401()
    {
        var client = _factory.CreateClient();

        DeviceTestHelper.AuthenticateWithDeviceToken(client, "not-a-real-token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);

        DeviceTestHelper.AuthenticateWithDeviceToken(client, $"{Guid.NewGuid()}.bogus-secret");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task DeviceToken_WithWrongSecret_ForARealCredential_Returns401()
    {
        var adminClient = _factory.CreateClient();
        var (registered, _, _) = await RegisterDeviceAsync(adminClient);
        var credentialId = registered.DeviceToken.Split('.', 2)[0];

        var deviceClient = _factory.CreateClient();
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, $"{credentialId}.tampered-secret");

        Assert.Equal(HttpStatusCode.Unauthorized, (await deviceClient.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Rotate_InvalidatesOldToken_AndNewTokenWorks()
    {
        var adminClient = _factory.CreateClient();
        var (registered, _, _) = await RegisterDeviceAsync(adminClient);

        var rotateResponse = await adminClient.PostAsync($"/api/v1/devices/{registered.DeviceId}/rotate-credential", content: null);
        Assert.Equal(HttpStatusCode.OK, rotateResponse.StatusCode);
        var rotated = await rotateResponse.Content.ReadFromJsonAsync<RotateDeviceCredentialResponse>();
        Assert.NotNull(rotated);
        Assert.NotEqual(registered.DeviceToken, rotated!.DeviceToken);

        var deviceClient = _factory.CreateClient();
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, registered.DeviceToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await deviceClient.GetAsync("/api/v1/auth/me")).StatusCode);

        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, rotated.DeviceToken);
        Assert.Equal(HttpStatusCode.OK, (await deviceClient.GetAsync("/api/v1/auth/me")).StatusCode);

        await using var dbContext = CreateDbContext();
        var eventTypes = await dbContext.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.DeviceId == registered.DeviceId)
            .Select(a => a.EventType)
            .ToListAsync();
        Assert.Contains("DeviceCredentialRotated", eventTypes);
    }

    [Fact]
    public async Task Revoke_BlocksFurtherCredentialUse_AndIsAudited()
    {
        var adminClient = _factory.CreateClient();
        var (registered, _, _) = await RegisterDeviceAsync(adminClient);

        var revokeResponse = await adminClient.PostAsync($"/api/v1/devices/{registered.DeviceId}/revoke", content: null);
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);
        var revoked = await revokeResponse.Content.ReadFromJsonAsync<DeviceResponse>();
        Assert.False(revoked!.HasActiveCredential);

        var deviceClient = _factory.CreateClient();
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, registered.DeviceToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await deviceClient.GetAsync("/api/v1/auth/me")).StatusCode);

        await using var dbContext = CreateDbContext();
        var eventTypes = await dbContext.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.DeviceId == registered.DeviceId)
            .Select(a => a.EventType)
            .ToListAsync();
        Assert.Contains("DeviceRevoked", eventTypes);
    }

    [Fact]
    public async Task Rotate_OnARevokedDevice_ReturnsConflict()
    {
        var adminClient = _factory.CreateClient();
        var (registered, _, _) = await RegisterDeviceAsync(adminClient);
        await adminClient.PostAsync($"/api/v1/devices/{registered.DeviceId}/revoke", content: null);

        var response = await adminClient.PostAsync($"/api/v1/devices/{registered.DeviceId}/rotate-credential", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RotateAndRevoke_Blocked_ForDifferentTenant()
    {
        var adminClient = _factory.CreateClient();
        var (registered, _, _) = await RegisterDeviceAsync(adminClient);

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(adminClient, "OrganisationOwner");
        AuthenticateAs(adminClient, otherTenantCaller);

        Assert.Equal(HttpStatusCode.NotFound, (await adminClient.PostAsync($"/api/v1/devices/{registered.DeviceId}/rotate-credential", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await adminClient.PostAsync($"/api/v1/devices/{registered.DeviceId}/revoke", content: null)).StatusCode);
    }

    [Fact]
    public async Task RotateAndRevoke_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var adminClient = _factory.CreateClient();
        var (registered, caller, _) = await RegisterDeviceAsync(adminClient);

        var otherOrgCaller = await RbacTestSeeder.SeedAsync(adminClient, "OrganisationOwner", caller.TenantId);
        AuthenticateAs(adminClient, otherOrgCaller);

        Assert.Equal(HttpStatusCode.NotFound, (await adminClient.PostAsync($"/api/v1/devices/{registered.DeviceId}/rotate-credential", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await adminClient.PostAsync($"/api/v1/devices/{registered.DeviceId}/revoke", content: null)).StatusCode);
    }

    [Fact]
    public async Task List_ShowsOwnOrganisationsDevices_WithCredentialFlag_AndFiltersByLocation()
    {
        var adminClient = _factory.CreateClient();
        var (registered, caller, location) = await RegisterDeviceAsync(adminClient);
        var otherLocation = await DeviceTestHelper.CreateLocationAsync(adminClient, caller.OrganisationId, "Second Venue");

        var list = await (await adminClient.GetAsync("/api/v1/devices")).Content.ReadFromJsonAsync<List<DeviceResponse>>();
        var listed = Assert.Single(list!, d => d.Id == registered.DeviceId);
        Assert.True(listed.HasActiveCredential);
        Assert.Equal(location.Id, listed.LocationId);

        var filtered = await (await adminClient.GetAsync($"/api/v1/devices?locationId={location.Id}")).Content.ReadFromJsonAsync<List<DeviceResponse>>();
        Assert.Contains(filtered!, d => d.Id == registered.DeviceId);

        var otherLocationList = await (await adminClient.GetAsync($"/api/v1/devices?locationId={otherLocation.Id}")).Content.ReadFromJsonAsync<List<DeviceResponse>>();
        Assert.DoesNotContain(otherLocationList!, d => d.Id == registered.DeviceId);
    }

    [Fact]
    public async Task List_ExcludesOtherTenantsAndOrganisationsDevices()
    {
        var adminClient = _factory.CreateClient();
        var (registered, caller, _) = await RegisterDeviceAsync(adminClient);

        var otherOrgCaller = await RbacTestSeeder.SeedAsync(adminClient, "OrganisationOwner", caller.TenantId);
        AuthenticateAs(adminClient, otherOrgCaller);
        var sameTenantList = await (await adminClient.GetAsync("/api/v1/devices")).Content.ReadFromJsonAsync<List<DeviceResponse>>();
        Assert.DoesNotContain(sameTenantList!, d => d.Id == registered.DeviceId);

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(adminClient, "OrganisationOwner");
        AuthenticateAs(adminClient, otherTenantCaller);
        var otherTenantList = await (await adminClient.GetAsync("/api/v1/devices")).Content.ReadFromJsonAsync<List<DeviceResponse>>();
        Assert.DoesNotContain(otherTenantList!, d => d.Id == registered.DeviceId);
    }

    /// <summary>Seeds an OrganisationOwner, creates a location + PIN, and registers a device.</summary>
    private async Task<(RegisterDeviceResponse Registered, SeededCaller Caller, LocationResponse Location)> RegisterDeviceAsync(HttpClient client)
    {
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, $"Device Venue {Guid.NewGuid()}");
        var pin = await DeviceTestHelper.CreatePinAsync(client, location.Id);
        var registered = await DeviceTestHelper.RegisterDeviceAsync(client, pin.Pin, name: "Test POS Device");
        return (registered, caller, location);
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
