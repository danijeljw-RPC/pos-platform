using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Endpoints.Menus;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization and behaviour tests for <see cref="MenuEndpoints"/> (PLAN-0004 Milestone G) —
/// gated <c>menus.manage</c> + <c>rejectStaffPin: true</c>, matching every configuration endpoint
/// group in the plan except the resolved-menu read (see <see cref="ResolvedMenuEndpointsTests"/>).
/// </summary>
public class MenuEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public MenuEndpointsTests(WebApplicationFactory<Program> factory)
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

        var response = await client.PostAsJsonAsync("/api/v1/menus", new CreateMenuRequest("Main Menu", caller.OrganisationId, null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MenuResponse>();
        Assert.NotNull(created);
        Assert.Equal("Main Menu", created!.Name);
        Assert.Null(created.LocationId);

        var list = await (await client.GetAsync("/api/v1/menus")).Content.ReadFromJsonAsync<List<MenuResponse>>();
        Assert.Contains(list!, m => m.Id == created.Id);
    }

    [Fact]
    public async Task Create_Succeeds_WithLocationSpecificMenu()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "Menu Location Test");

        var response = await client.PostAsJsonAsync("/api/v1/menus", new CreateMenuRequest("Location Menu", caller.OrganisationId, location.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MenuResponse>();
        Assert.Equal(location.Id, created!.LocationId);
    }

    [Fact]
    public async Task Create_Rejects_LocationFromDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);
        var locationB = await DeviceTestHelper.CreateLocationAsync(client, callerB.OrganisationId, "Org B Location");

        AuthenticateAs(client, callerA);
        var response = await client.PostAsJsonAsync("/api/v1/menus", new CreateMenuRequest("Nope", callerA.OrganisationId, locationB.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);

        var response = await client.PostAsJsonAsync("/api/v1/menus", new CreateMenuRequest("Nope", caller.OrganisationId, null, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsMenu_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "GET_TEST");

        var response = await client.GetAsync($"/api/v1/menus/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "UPDATE_TEST");

        var response = await client.PatchAsJsonAsync($"/api/v1/menus/{created.Id}", new UpdateMenuRequest("Renamed Menu", null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<MenuResponse>();
        Assert.Equal("Renamed Menu", updated!.Name);
    }

    [Fact]
    public async Task DeactivateAndReactivate_TogglesIsActive_AndAffectsListVisibility()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "DEACTIVATE_TEST");

        var deactivateResponse = await client.PostAsync($"/api/v1/menus/{created.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        var listAfterDeactivate = await (await client.GetAsync("/api/v1/menus")).Content.ReadFromJsonAsync<List<MenuResponse>>();
        Assert.DoesNotContain(listAfterDeactivate!, m => m.Id == created.Id);

        var reactivateResponse = await client.PostAsync($"/api/v1/menus/{created.Id}/reactivate", null);
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);

        var listAfterReactivate = await (await client.GetAsync("/api/v1/menus")).Content.ReadFromJsonAsync<List<MenuResponse>>();
        Assert.Contains(listAfterReactivate!, m => m.Id == created.Id);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(ownerClient, "OrganisationOwner");

        var staffClient = _factory.CreateClient();
        var staffCaller = await RbacTestSeeder.SeedAsync(staffClient, "Staff", owner.TenantId);
        AuthenticateAs(staffClient, staffCaller);

        var response = await staffClient.PostAsJsonAsync("/api/v1/menus", new CreateMenuRequest("Nope", staffCaller.OrganisationId, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReadAndUpdate_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var created = await CreateAndParseAsync(client, callerA.OrganisationId, "CROSS_ORG_TEST");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/menus/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync($"/api/v1/menus/{created.Id}", new UpdateMenuRequest("Nope", null))).StatusCode);
    }

    [Fact]
    public async Task Read_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, owner);
        var created = await CreateAndParseAsync(client, owner.OrganisationId, "CROSS_TENANT_TEST");

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, otherTenantCaller);

        var response = await client.GetAsync($"/api/v1/menus/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "AUDIT_TEST");

        await client.PatchAsJsonAsync($"/api/v1/menus/{created.Id}", new UpdateMenuRequest("Renamed", null));
        await client.PostAsync($"/api/v1/menus/{created.Id}/deactivate", null);
        await client.PostAsync($"/api/v1/menus/{created.Id}/reactivate", null);

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "Menu")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("MenuCreated", eventTypes);
        Assert.Contains("MenuUpdated", eventTypes);
        Assert.Contains("MenuDeactivated", eventTypes);
        Assert.Contains("MenuReactivated", eventTypes);
    }

    private async Task<MenuResponse> CreateAndParseAsync(HttpClient client, Guid organisationId, string codeSuffix)
    {
        var response = await client.PostAsJsonAsync("/api/v1/menus", new CreateMenuRequest($"Menu {codeSuffix}", organisationId, null));
        return (await response.Content.ReadFromJsonAsync<MenuResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
