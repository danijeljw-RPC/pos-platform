using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Endpoints.Tax;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Domain.Enums;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization and behaviour tests for <see cref="VenueTaxConfigurationEndpoints"/> (PLAN-0004
/// Milestone F), gated <c>pricing.manage</c> + <c>rejectStaffPin: true</c>. Includes the plan's
/// approved Human Decision #5 proof: a missing configuration 404s rather than silently defaulting.
/// </summary>
public class VenueTaxConfigurationEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public VenueTaxConfigurationEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
        });
    }

    [Fact]
    public async Task Create_Succeeds_ForOrganisationOwner_AndAppearsInList()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "Create Test Venue");

        var response = await CreateConfigAsync(client, location.Id, true, TaxCalculationScope.PerLine);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<VenueTaxConfigurationResponse>();
        Assert.NotNull(created);
        Assert.Equal(location.Id, created!.LocationId);
        Assert.True(created.TaxInclusivePricing);

        var list = await (await client.GetAsync("/api/v1/venue-tax-configurations")).Content.ReadFromJsonAsync<List<VenueTaxConfigurationResponse>>();
        Assert.Contains(list!, v => v.Id == created.Id);
    }

    [Fact]
    public async Task Create_Rejects_LocationFromDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var locationA = await DeviceTestHelper.CreateLocationAsync(client, callerA.OrganisationId, "Org A Venue");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        var response = await CreateConfigAsync(client, locationA.Id, true, TaxCalculationScope.PerLine);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Rejects_DuplicateLocation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "Duplicate Test Venue");
        await CreateConfigAsync(client, location.Id, true, TaxCalculationScope.PerLine);

        var response = await CreateConfigAsync(client, location.Id, false, TaxCalculationScope.PerLine);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsConfig_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "Get Test Venue");

        var response = await client.GetAsync($"/api/v1/venue-tax-configurations/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_MissingConfiguration_ReturnsNotFound_InsteadOfSilentlyDefaulting()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);

        // No VenueTaxConfiguration was ever created for this id — per the plan's approved Human
        // Decision #5, this must 404, never silently default to an AU-flavoured configuration.
        var response = await client.GetAsync($"/api/v1/venue-tax-configurations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesFields_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "Update Test Venue");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/venue-tax-configurations/{created.Id}",
            new UpdateVenueTaxConfigurationRequest(false, TaxCalculationScope.PerComponent));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<VenueTaxConfigurationResponse>();
        Assert.False(updated!.TaxInclusivePricing);
        Assert.Equal(TaxCalculationScope.PerComponent, updated.TaxCalculationMode);
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "TenantId Trap Venue");

        var response = await client.PostAsJsonAsync(
            "/api/v1/venue-tax-configurations",
            new CreateVenueTaxConfigurationRequest(location.Id, true, TaxCalculationScope.PerLine, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(ownerClient, "OrganisationOwner");
        AuthenticateAs(ownerClient, owner);
        var location = await DeviceTestHelper.CreateLocationAsync(ownerClient, owner.OrganisationId, "No Permission Venue");

        var staffClient = _factory.CreateClient();
        var staffCaller = await RbacTestSeeder.SeedAsync(staffClient, "Staff", owner.TenantId);
        AuthenticateAs(staffClient, staffCaller);

        var response = await CreateConfigAsync(staffClient, location.Id, true, TaxCalculationScope.PerLine);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Read_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, owner);
        var created = await CreateAndParseAsync(client, owner.OrganisationId, "Cross Tenant Venue");

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, otherTenantCaller);

        var response = await client.GetAsync($"/api/v1/venue-tax-configurations/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReadAndUpdate_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var created = await CreateAndParseAsync(client, callerA.OrganisationId, "Cross Org RW Venue");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/venue-tax-configurations/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync(
            $"/api/v1/venue-tax-configurations/{created.Id}", new UpdateVenueTaxConfigurationRequest(true, TaxCalculationScope.PerLine))).StatusCode);
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "Audit Test Venue");

        await client.PatchAsJsonAsync($"/api/v1/venue-tax-configurations/{created.Id}", new UpdateVenueTaxConfigurationRequest(false, TaxCalculationScope.PerLine));

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "VenueTaxConfiguration")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("VenueTaxConfigurationCreated", eventTypes);
        Assert.Contains("VenueTaxConfigurationUpdated", eventTypes);
    }

    private static Task<HttpResponseMessage> CreateConfigAsync(HttpClient client, Guid locationId, bool taxInclusivePricing, TaxCalculationScope mode) =>
        client.PostAsJsonAsync("/api/v1/venue-tax-configurations", new CreateVenueTaxConfigurationRequest(locationId, taxInclusivePricing, mode));

    private async Task<VenueTaxConfigurationResponse> CreateAndParseAsync(HttpClient client, Guid organisationId, string venueName)
    {
        var location = await DeviceTestHelper.CreateLocationAsync(client, organisationId, venueName);
        var response = await CreateConfigAsync(client, location.Id, true, TaxCalculationScope.PerLine);
        return (await response.Content.ReadFromJsonAsync<VenueTaxConfigurationResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
