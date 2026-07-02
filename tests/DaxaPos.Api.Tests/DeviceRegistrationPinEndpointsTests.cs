using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Infrastructure.Security;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization and lifecycle tests for device registration PIN issuance/revocation (PLAN-0003
/// Milestone E). The raw PIN is returned once and only its salted hash is persisted; a revoked
/// PIN can no longer register a device. Endpoints are gated <c>devices.register</c> +
/// <c>rejectStaffPin: true</c> — the staff-PIN rejection mechanism itself is unit-tested in
/// <c>RequirePermissionFilterTests.cs</c>, not re-tested per endpoint (no staff-PIN login exists
/// until Milestone F).
/// </summary>
public class DeviceRegistrationPinEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public DeviceRegistrationPinEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
            // Raised so this class's registration attempts never trip the per-IP limiter — the
            // limiter's own behaviour is tested in DeviceRegistrationRateLimitTests with the
            // default limit.
            builder.UseSetting("DeviceRegistration:RateLimitPermitLimit", "1000");
        });
    }

    [Fact]
    public async Task Create_ReturnsSixDigitPin_WithApprovedDefaults()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "Pin Test Venue");

        var before = DateTimeOffset.UtcNow;
        var response = await client.PostAsJsonAsync(
            "/api/v1/device-registration-pins",
            new CreateDeviceRegistrationPinRequest(location.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<DeviceRegistrationPinCreatedResponse>();
        Assert.NotNull(created);
        Assert.Matches(new Regex("^[0-9]{6}$"), created!.Pin);
        Assert.Equal(1, created.MaxUses);
        Assert.InRange(created.ExpiresAtUtc, before.AddMinutes(14), before.AddMinutes(16));
    }

    [Fact]
    public async Task Create_NeverPersistsTheRawPin()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "Pin Hash Venue");

        var created = await DeviceTestHelper.CreatePinAsync(client, location.Id);

        await using var dbContext = CreateDbContext();
        var row = await dbContext.DeviceRegistrationPins.IgnoreQueryFilters().SingleAsync(p => p.Id == created.Id);

        Assert.NotEqual(created.Pin, row.PinHash);
        Assert.True(new HmacDeviceCredentialHasher().Verify(created.Pin, row.PinHash));
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "Tenant Reject Venue");

        var response = await client.PostAsJsonAsync(
            "/api/v1/device-registration-pins",
            new CreateDeviceRegistrationPinRequest(location.Id, TenantId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public async Task Create_Rejects_MaxUsesOutsideApprovedRange(int maxUses)
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "MaxUses Venue");

        var response = await client.PostAsJsonAsync(
            "/api/v1/device-registration-pins",
            new CreateDeviceRegistrationPinRequest(location.Id, maxUses));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_AllowsMaxUses_WithinApprovedRange()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "MaxUses OK Venue");

        var created = await DeviceTestHelper.CreatePinAsync(client, location.Id, maxUses: 5);

        Assert.Equal(5, created.MaxUses);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, owner);
        var location = await DeviceTestHelper.CreateLocationAsync(client, owner.OrganisationId, "No Permission Venue");

        // Staff holds none of the catalogue permissions, including devices.register.
        var staff = await RbacTestSeeder.SeedAsync(client, "Staff", owner.TenantId);
        AuthenticateAs(client, staff);

        var response = await client.PostAsJsonAsync(
            "/api/v1/device-registration-pins",
            new CreateDeviceRegistrationPinRequest(location.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var locationA = await DeviceTestHelper.CreateLocationAsync(client, callerA.OrganisationId, "Org A Venue");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        var response = await client.PostAsJsonAsync(
            "/api/v1/device-registration-pins",
            new CreateDeviceRegistrationPinRequest(locationA.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, owner);
        var location = await DeviceTestHelper.CreateLocationAsync(client, owner.OrganisationId, "Cross Tenant Venue");

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, otherTenantCaller);

        var response = await client.PostAsJsonAsync(
            "/api/v1/device-registration-pins",
            new CreateDeviceRegistrationPinRequest(location.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Revoke_Works_AndRevokedPinCannotRegister()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "Revoke Venue");
        var pin = await DeviceTestHelper.CreatePinAsync(client, location.Id);

        var revokeResponse = await client.PostAsync($"/api/v1/device-registration-pins/{pin.Id}/revoke", content: null);

        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);
        var revoked = await revokeResponse.Content.ReadFromJsonAsync<DeviceRegistrationPinResponse>();
        Assert.NotNull(revoked!.RevokedAtUtc);

        var registerResponse = await DeviceTestHelper.RegisterDeviceRawAsync(client, pin.Pin);
        Assert.Equal(HttpStatusCode.Unauthorized, registerResponse.StatusCode);
    }

    [Fact]
    public async Task Revoke_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, owner);
        var location = await DeviceTestHelper.CreateLocationAsync(client, owner.OrganisationId, "Revoke Cross Tenant Venue");
        var pin = await DeviceTestHelper.CreatePinAsync(client, location.Id);

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, otherTenantCaller);

        var response = await client.PostAsync($"/api/v1/device-registration-pins/{pin.Id}/revoke", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndRevoke_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "Pin Audit Venue");
        var pin = await DeviceTestHelper.CreatePinAsync(client, location.Id);
        await client.PostAsync($"/api/v1/device-registration-pins/{pin.Id}/revoke", content: null);

        await using var dbContext = CreateDbContext();
        var eventTypes = await dbContext.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == pin.Id && a.EntityType == "DeviceRegistrationPin")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("DeviceRegistrationPinCreated", eventTypes);
        Assert.Contains("DeviceRegistrationPinRevoked", eventTypes);
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
