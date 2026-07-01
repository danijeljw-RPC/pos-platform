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
/// Authorization tests for the Location create/read/rename/deactivate/reactivate endpoints
/// (PLAN-0003 Milestone D). Unlike Organisation, every operation here is also checked against
/// <c>AuthContext.OrganisationId</c> — see the "different organisation, same tenant" tests below,
/// which exercise that check independently of the tenant filter. Every request authenticates via a
/// LocalUsernamePassword session against endpoints configured with <c>rejectStaffPin: true</c>; see
/// <c>OrganisationEndpointsTests.cs</c>'s class remarks and <c>RequirePermissionFilterTests.cs</c>
/// for why that mechanism isn't re-tested per entity.
/// </summary>
public class LocationEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public LocationEndpointsTests(WebApplicationFactory<Program> factory)
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

        var response = await client.PostAsJsonAsync("/api/v1/locations", new CreateLocationRequest("Sydney CBD", caller.OrganisationId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<LocationResponse>();
        Assert.NotNull(created);
        Assert.Equal(caller.TenantId, created!.TenantId);
        Assert.Equal(caller.OrganisationId, created.OrganisationId);
        Assert.True(created.IsActive);

        var list = await (await client.GetAsync("/api/v1/locations")).Content.ReadFromJsonAsync<List<LocationResponse>>();
        Assert.Contains(list!, l => l.Id == created.Id);
    }

    [Fact]
    public async Task Get_ReturnsLocation_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateLocationAsync(client, caller.OrganisationId, "Bondi");

        var response = await client.GetAsync($"/api/v1/locations/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_RenamesLocation_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateLocationAsync(client, caller.OrganisationId, "Parramatta");

        var response = await client.PatchAsJsonAsync($"/api/v1/locations/{created.Id}", new UpdateLocationRequest("Parramatta Renamed"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<LocationResponse>();
        Assert.Equal("Parramatta Renamed", updated!.Name);
    }

    [Fact]
    public async Task Deactivate_Then_Reactivate_TogglesIsActive_AndListReflectsIt()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateLocationAsync(client, caller.OrganisationId, "Newcastle");

        var deactivateResponse = await client.PostAsync($"/api/v1/locations/{created.Id}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.False((await deactivateResponse.Content.ReadFromJsonAsync<LocationResponse>())!.IsActive);

        var listAfterDeactivate = await (await client.GetAsync("/api/v1/locations")).Content.ReadFromJsonAsync<List<LocationResponse>>();
        Assert.DoesNotContain(listAfterDeactivate!, l => l.Id == created.Id);

        var getInactiveResponse = await client.GetAsync($"/api/v1/locations/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getInactiveResponse.StatusCode);

        var reactivateResponse = await client.PostAsync($"/api/v1/locations/{created.Id}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
        Assert.True((await reactivateResponse.Content.ReadFromJsonAsync<LocationResponse>())!.IsActive);

        var listAfterReactivate = await (await client.GetAsync("/api/v1/locations")).Content.ReadFromJsonAsync<List<LocationResponse>>();
        Assert.Contains(listAfterReactivate!, l => l.Id == created.Id);
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);

        var response = await client.PostAsJsonAsync(
            "/api/v1/locations",
            new CreateLocationRequest("Should Fail", caller.OrganisationId, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateLocationAsync(client, caller.OrganisationId, "Should Not Rename");

        var response = await client.PatchAsJsonAsync($"/api/v1/locations/{created.Id}", new UpdateLocationRequest("Should Fail", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var client = _factory.CreateClient();
        // VenueManager holds terminals.manage but not locations.manage in the catalogue.
        var caller = await RbacTestSeeder.SeedAsync(client, "VenueManager");
        AuthenticateAs(client, caller);

        var response = await client.PostAsJsonAsync("/api/v1/locations", new CreateLocationRequest("Should Fail", caller.OrganisationId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Read_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, owner);
        var created = await CreateLocationAsync(client, owner.OrganisationId, "Owner's Location");

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, otherTenantCaller);

        var response = await client.GetAsync($"/api/v1/locations/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAndDeactivateAndReactivate_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, owner);
        var created = await CreateLocationAsync(client, owner.OrganisationId, "Owner's Other Location");

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, otherTenantCaller);

        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync($"/api/v1/locations/{created.Id}", new UpdateLocationRequest("Hijacked"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/locations/{created.Id}/deactivate", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/locations/{created.Id}/reactivate", content: null)).StatusCode);
    }

    [Fact]
    public async Task Create_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);

        AuthenticateAs(client, callerB);
        // callerB tries to create a location under callerA's organisation — same tenant, wrong org.
        var response = await client.PostAsJsonAsync("/api/v1/locations", new CreateLocationRequest("Should Fail", callerA.OrganisationId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReadAndUpdateAndDeactivate_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var created = await CreateLocationAsync(client, callerA.OrganisationId, "Org A Location");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/locations/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync($"/api/v1/locations/{created.Id}", new UpdateLocationRequest("Hijacked"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/locations/{created.Id}/deactivate", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/locations/{created.Id}/reactivate", content: null)).StatusCode);

        // The location must not appear in callerB's list either.
        var list = await (await client.GetAsync("/api/v1/locations")).Content.ReadFromJsonAsync<List<LocationResponse>>();
        Assert.DoesNotContain(list!, l => l.Id == created.Id);
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateLocationAsync(client, caller.OrganisationId, "Audited Location");

        await client.PatchAsJsonAsync($"/api/v1/locations/{created.Id}", new UpdateLocationRequest("Audited Location Renamed"));
        await client.PostAsync($"/api/v1/locations/{created.Id}/deactivate", content: null);
        await client.PostAsync($"/api/v1/locations/{created.Id}/reactivate", content: null);

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "Location")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("LocationCreated", eventTypes);
        Assert.Contains("LocationUpdated", eventTypes);
        Assert.Contains("LocationDeactivated", eventTypes);
        Assert.Contains("LocationReactivated", eventTypes);
    }

    private static async Task<LocationResponse> CreateLocationAsync(HttpClient client, Guid organisationId, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/locations", new CreateLocationRequest(name, organisationId));
        return (await response.Content.ReadFromJsonAsync<LocationResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
