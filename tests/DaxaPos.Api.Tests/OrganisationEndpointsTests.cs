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
/// Authorization tests for the Organisation create/read/rename/deactivate/reactivate endpoints
/// (PLAN-0003 Milestone D). Every request in this file authenticates via a LocalUsernamePassword
/// session against endpoints configured with <c>rejectStaffPin: true</c> — every passing test here
/// doubles as proof that flag doesn't break current admin sessions. See
/// <c>RequirePermissionFilterTests.cs</c> for the flag's unit-level behaviour across all
/// AuthMethod/rejectStaffPin combinations, including a rejected <c>LocalStaffPin</c> context, which
/// cannot be produced end-to-end until Milestone F.
/// </summary>
public class OrganisationEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public OrganisationEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
        });
    }

    [Fact]
    public async Task Create_Succeeds_ForSystemAdmin_AndAppearsInList()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");
        AuthenticateAs(client, caller);

        var createResponse = await client.PostAsJsonAsync("/api/v1/organisations", new CreateOrganisationRequest("Second Org"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.NotNull(created);
        Assert.Equal(caller.TenantId, created!.TenantId);
        Assert.True(created.IsActive);

        var list = await (await client.GetAsync("/api/v1/organisations")).Content.ReadFromJsonAsync<List<OrganisationResponse>>();
        Assert.Contains(list!, o => o.Id == created.Id);
        Assert.Contains(list!, o => o.Id == caller.OrganisationId);
    }

    [Fact]
    public async Task Get_ReturnsOrganisation_WithinSameTenant()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");
        AuthenticateAs(client, caller);

        var response = await client.GetAsync($"/api/v1/organisations/{caller.OrganisationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.Equal(caller.OrganisationId, body!.Id);
    }

    [Fact]
    public async Task Update_RenamesOrganisation_WithinSameTenant()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");
        AuthenticateAs(client, caller);

        var response = await client.PatchAsJsonAsync($"/api/v1/organisations/{caller.OrganisationId}", new UpdateOrganisationRequest("Renamed Org"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.Equal("Renamed Org", updated!.Name);
    }

    [Fact]
    public async Task Deactivate_Then_Reactivate_TogglesIsActive_AndListReflectsIt()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");
        AuthenticateAs(client, caller);

        var created = await (await client.PostAsJsonAsync("/api/v1/organisations", new CreateOrganisationRequest("To Deactivate")))
            .Content.ReadFromJsonAsync<OrganisationResponse>();

        var deactivateResponse = await client.PostAsync($"/api/v1/organisations/{created!.Id}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        var deactivated = await deactivateResponse.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.False(deactivated!.IsActive);

        // List hides inactive records by default.
        var listAfterDeactivate = await (await client.GetAsync("/api/v1/organisations")).Content.ReadFromJsonAsync<List<OrganisationResponse>>();
        Assert.DoesNotContain(listAfterDeactivate!, o => o.Id == created.Id);

        // GET by id still finds the inactive row for a manage-permission caller.
        var getInactiveResponse = await client.GetAsync($"/api/v1/organisations/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getInactiveResponse.StatusCode);
        var fetchedInactive = await getInactiveResponse.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.False(fetchedInactive!.IsActive);

        var reactivateResponse = await client.PostAsync($"/api/v1/organisations/{created.Id}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
        var reactivated = await reactivateResponse.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.True(reactivated!.IsActive);

        var listAfterReactivate = await (await client.GetAsync("/api/v1/organisations")).Content.ReadFromJsonAsync<List<OrganisationResponse>>();
        Assert.Contains(listAfterReactivate!, o => o.Id == created.Id);
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");
        AuthenticateAs(client, caller);

        var response = await client.PostAsJsonAsync("/api/v1/organisations", new CreateOrganisationRequest("Should Fail", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");
        AuthenticateAs(client, caller);

        var response = await client.PatchAsJsonAsync($"/api/v1/organisations/{caller.OrganisationId}", new UpdateOrganisationRequest("Should Fail", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var client = _factory.CreateClient();
        // VenueManager does not hold organisations.manage in the Initial Permission Catalogue.
        var caller = await RbacTestSeeder.SeedAsync(client, "VenueManager");
        AuthenticateAs(client, caller);

        var response = await client.PostAsJsonAsync("/api/v1/organisations", new CreateOrganisationRequest("Should Fail"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Read_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");
        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");

        AuthenticateAs(client, otherTenantCaller);
        var response = await client.GetAsync($"/api/v1/organisations/{owner.OrganisationId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");
        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");

        AuthenticateAs(client, otherTenantCaller);
        var response = await client.PatchAsJsonAsync($"/api/v1/organisations/{owner.OrganisationId}", new UpdateOrganisationRequest("Hijacked"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateAndReactivate_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");
        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");

        AuthenticateAs(client, otherTenantCaller);

        var deactivateResponse = await client.PostAsync($"/api/v1/organisations/{owner.OrganisationId}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.NotFound, deactivateResponse.StatusCode);

        var reactivateResponse = await client.PostAsync($"/api/v1/organisations/{owner.OrganisationId}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.NotFound, reactivateResponse.StatusCode);
    }

    [Fact]
    public async Task List_OnlyReturnsCallersTenantOrganisations()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");
        var callerB = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");

        AuthenticateAs(client, callerA);
        var list = await (await client.GetAsync("/api/v1/organisations")).Content.ReadFromJsonAsync<List<OrganisationResponse>>();

        Assert.Contains(list!, o => o.Id == callerA.OrganisationId);
        Assert.DoesNotContain(list!, o => o.Id == callerB.OrganisationId);
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "SystemAdmin");
        AuthenticateAs(client, caller);

        var created = await (await client.PostAsJsonAsync("/api/v1/organisations", new CreateOrganisationRequest("Audited Org")))
            .Content.ReadFromJsonAsync<OrganisationResponse>();

        await client.PatchAsJsonAsync($"/api/v1/organisations/{created!.Id}", new UpdateOrganisationRequest("Audited Org Renamed"));
        await client.PostAsync($"/api/v1/organisations/{created.Id}/deactivate", content: null);
        await client.PostAsync($"/api/v1/organisations/{created.Id}/reactivate", content: null);

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "Organisation")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("OrganisationCreated", eventTypes);
        Assert.Contains("OrganisationUpdated", eventTypes);
        Assert.Contains("OrganisationDeactivated", eventTypes);
        Assert.Contains("OrganisationReactivated", eventTypes);
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
