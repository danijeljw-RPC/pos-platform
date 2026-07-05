using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Catalog;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization and behaviour tests for <see cref="ModifierGroupEndpoints"/> (PLAN-0004
/// Milestone E). See <see cref="TaxDefinitionEndpointsTests"/>'s class remarks for why staff-PIN
/// rejection and the permission filter's unit-level behaviour aren't re-tested per entity here.
/// </summary>
public class ModifierGroupEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public ModifierGroupEndpointsTests(WebApplicationFactory<Program> factory)
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

        var response = await CreateAsync(client, caller.OrganisationId, "Milk Type");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ModifierGroupResponse>();
        Assert.NotNull(created);
        Assert.Equal(caller.TenantId, created!.TenantId);
        Assert.True(created.IsActive);

        var list = await (await client.GetAsync("/api/v1/modifier-groups")).Content.ReadFromJsonAsync<List<ModifierGroupResponse>>();
        Assert.Contains(list!, g => g.Id == created.Id);
    }

    [Fact]
    public async Task Create_Rejects_SelectionMaxLessThanSelectionMin()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);

        var response = await client.PostAsJsonAsync(
            "/api/v1/modifier-groups",
            new CreateModifierGroupRequest("Invalid Group", caller.OrganisationId, 3, 1, false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsModifierGroup_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "Get Test");

        var response = await client.GetAsync($"/api/v1/modifier-groups/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesFields_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "Update Test");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/modifier-groups/{created.Id}",
            new UpdateModifierGroupRequest("Add-ons", 0, 3, true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ModifierGroupResponse>();
        Assert.Equal("Add-ons", updated!.Name);
        Assert.Equal(0, updated.SelectionMin);
        Assert.Equal(3, updated.SelectionMax);
        Assert.True(updated.IsRequired);
    }

    [Fact]
    public async Task Deactivate_Then_Reactivate_TogglesIsActive_AndListReflectsIt()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "Toggle Test");

        var deactivateResponse = await client.PostAsync($"/api/v1/modifier-groups/{created.Id}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.False((await deactivateResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>())!.IsActive);

        var listAfterDeactivate = await (await client.GetAsync("/api/v1/modifier-groups")).Content.ReadFromJsonAsync<List<ModifierGroupResponse>>();
        Assert.DoesNotContain(listAfterDeactivate!, g => g.Id == created.Id);

        var reactivateResponse = await client.PostAsync($"/api/v1/modifier-groups/{created.Id}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
        Assert.True((await reactivateResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>())!.IsActive);
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);

        var response = await client.PostAsJsonAsync(
            "/api/v1/modifier-groups",
            new CreateModifierGroupRequest("Should Fail", caller.OrganisationId, 0, 1, false, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "Staff");
        AuthenticateAs(client, caller);

        var response = await CreateAsync(client, caller.OrganisationId, "Should Fail");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Read_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, owner);
        var created = await CreateAndParseAsync(client, owner.OrganisationId, "Cross Tenant Test");

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, otherTenantCaller);

        var response = await client.GetAsync($"/api/v1/modifier-groups/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);

        AuthenticateAs(client, callerB);
        var response = await CreateAsync(client, callerA.OrganisationId, "Cross Org Test");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "Audit Test");

        await client.PatchAsJsonAsync($"/api/v1/modifier-groups/{created.Id}", new UpdateModifierGroupRequest("Renamed", 0, 2, false));
        await client.PostAsync($"/api/v1/modifier-groups/{created.Id}/deactivate", content: null);
        await client.PostAsync($"/api/v1/modifier-groups/{created.Id}/reactivate", content: null);

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "ModifierGroup")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("ModifierGroupCreated", eventTypes);
        Assert.Contains("ModifierGroupUpdated", eventTypes);
        Assert.Contains("ModifierGroupDeactivated", eventTypes);
        Assert.Contains("ModifierGroupReactivated", eventTypes);
    }

    private static Task<HttpResponseMessage> CreateAsync(HttpClient client, Guid organisationId, string name) =>
        client.PostAsJsonAsync("/api/v1/modifier-groups", new CreateModifierGroupRequest(name, organisationId, 0, 1, false));

    private static async Task<ModifierGroupResponse> CreateAndParseAsync(HttpClient client, Guid organisationId, string name)
    {
        var response = await CreateAsync(client, organisationId, name);
        return (await response.Content.ReadFromJsonAsync<ModifierGroupResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
