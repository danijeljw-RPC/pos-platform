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
/// Authorization and behaviour tests for <see cref="ModifierEndpoints"/> (PLAN-0004 Milestone E).
/// See <see cref="TaxDefinitionEndpointsTests"/>'s class remarks for why staff-PIN rejection and
/// the permission filter's unit-level behaviour aren't re-tested per entity here.
/// </summary>
public class ModifierEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public ModifierEndpointsTests(WebApplicationFactory<Program> factory)
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
        var modifierGroup = await CreateModifierGroupAsync(client, caller.OrganisationId, "CREATE_TEST");

        var response = await CreateModifierAsync(client, modifierGroup.Id, "Oat Milk", 1.00m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ModifierResponse>();
        Assert.NotNull(created);
        Assert.Equal(caller.TenantId, created!.TenantId);
        Assert.True(created.IsActive);

        var list = await (await client.GetAsync("/api/v1/modifiers")).Content.ReadFromJsonAsync<List<ModifierResponse>>();
        Assert.Contains(list!, m => m.Id == created.Id);
    }

    [Theory]
    [InlineData(-1.00)]
    [InlineData(0)]
    [InlineData(2.50)]
    public async Task Create_AcceptsNegativeZeroAndPositivePriceDeltas(decimal priceDelta)
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var modifierGroup = await CreateModifierGroupAsync(client, caller.OrganisationId, $"DELTA_TEST_{priceDelta}");

        var response = await CreateModifierAsync(client, modifierGroup.Id, "Modifier", priceDelta);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ModifierResponse>();
        Assert.Equal(priceDelta, created!.PriceDelta);
    }

    [Fact]
    public async Task Create_Rejects_ModifierGroupFromDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var groupA = await CreateModifierGroupAsync(client, callerA.OrganisationId, "ORG_A_GROUP");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        var response = await CreateModifierAsync(client, groupA.Id, "Should Fail", 1.00m);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsModifier_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var modifierGroup = await CreateModifierGroupAsync(client, caller.OrganisationId, "GET_TEST");
        var created = await CreateAndParseModifierAsync(client, modifierGroup.Id, "Get Modifier", 1.00m);

        var response = await client.GetAsync($"/api/v1/modifiers/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesNameAndPriceDelta_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var modifierGroup = await CreateModifierGroupAsync(client, caller.OrganisationId, "UPDATE_TEST");
        var created = await CreateAndParseModifierAsync(client, modifierGroup.Id, "Update Modifier", 1.00m);

        var response = await client.PatchAsJsonAsync($"/api/v1/modifiers/{created.Id}", new UpdateModifierRequest("Extra Shot", 1.50m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ModifierResponse>();
        Assert.Equal("Extra Shot", updated!.Name);
        Assert.Equal(1.50m, updated.PriceDelta);
    }

    [Fact]
    public async Task Deactivate_Then_Reactivate_TogglesIsActive_AndListReflectsIt()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var modifierGroup = await CreateModifierGroupAsync(client, caller.OrganisationId, "TOGGLE_TEST");
        var created = await CreateAndParseModifierAsync(client, modifierGroup.Id, "Toggle Modifier", 1.00m);

        var deactivateResponse = await client.PostAsync($"/api/v1/modifiers/{created.Id}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.False((await deactivateResponse.Content.ReadFromJsonAsync<ModifierResponse>())!.IsActive);

        var listAfterDeactivate = await (await client.GetAsync("/api/v1/modifiers")).Content.ReadFromJsonAsync<List<ModifierResponse>>();
        Assert.DoesNotContain(listAfterDeactivate!, m => m.Id == created.Id);

        var reactivateResponse = await client.PostAsync($"/api/v1/modifiers/{created.Id}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
        Assert.True((await reactivateResponse.Content.ReadFromJsonAsync<ModifierResponse>())!.IsActive);
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var modifierGroup = await CreateModifierGroupAsync(client, caller.OrganisationId, "TENANTID_TRAP");

        var response = await client.PostAsJsonAsync(
            "/api/v1/modifiers",
            new CreateModifierRequest("Should Fail", modifierGroup.Id, 1.00m, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(ownerClient, "OrganisationOwner");
        AuthenticateAs(ownerClient, owner);
        var modifierGroup = await CreateModifierGroupAsync(ownerClient, owner.OrganisationId, "NO_PERMISSION_TEST");

        var staffClient = _factory.CreateClient();
        var staffCaller = await RbacTestSeeder.SeedAsync(staffClient, "Staff", owner.TenantId);
        AuthenticateAs(staffClient, staffCaller);

        var response = await CreateModifierAsync(staffClient, modifierGroup.Id, "Should Fail", 1.00m);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Read_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, owner);
        var modifierGroup = await CreateModifierGroupAsync(client, owner.OrganisationId, "CROSS_TENANT_TEST");
        var created = await CreateAndParseModifierAsync(client, modifierGroup.Id, "Cross Tenant Modifier", 1.00m);

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, otherTenantCaller);

        var response = await client.GetAsync($"/api/v1/modifiers/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReadAndUpdateAndDeactivate_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var modifierGroup = await CreateModifierGroupAsync(client, callerA.OrganisationId, "CROSS_ORG_RW_TEST");
        var created = await CreateAndParseModifierAsync(client, modifierGroup.Id, "Cross Org Modifier", 1.00m);

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/modifiers/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/modifiers/{created.Id}/deactivate", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync(
            $"/api/v1/modifiers/{created.Id}", new UpdateModifierRequest("Hijacked", 0m))).StatusCode);
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var modifierGroup = await CreateModifierGroupAsync(client, caller.OrganisationId, "AUDIT_TEST");
        var created = await CreateAndParseModifierAsync(client, modifierGroup.Id, "Audit Modifier", 1.00m);

        await client.PatchAsJsonAsync($"/api/v1/modifiers/{created.Id}", new UpdateModifierRequest("Audit Renamed", 2.00m));
        await client.PostAsync($"/api/v1/modifiers/{created.Id}/deactivate", content: null);
        await client.PostAsync($"/api/v1/modifiers/{created.Id}/reactivate", content: null);

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "Modifier")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("ModifierCreated", eventTypes);
        Assert.Contains("ModifierUpdated", eventTypes);
        Assert.Contains("ModifierDeactivated", eventTypes);
        Assert.Contains("ModifierReactivated", eventTypes);
    }

    private static async Task<ModifierGroupResponse> CreateModifierGroupAsync(HttpClient client, Guid organisationId, string codeSuffix)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/modifier-groups", new CreateModifierGroupRequest($"Group {codeSuffix}", organisationId, 0, 1, false));
        return (await response.Content.ReadFromJsonAsync<ModifierGroupResponse>())!;
    }

    private static Task<HttpResponseMessage> CreateModifierAsync(HttpClient client, Guid modifierGroupId, string name, decimal priceDelta) =>
        client.PostAsJsonAsync("/api/v1/modifiers", new CreateModifierRequest(name, modifierGroupId, priceDelta));

    private static async Task<ModifierResponse> CreateAndParseModifierAsync(HttpClient client, Guid modifierGroupId, string name, decimal priceDelta)
    {
        var response = await CreateModifierAsync(client, modifierGroupId, name, priceDelta);
        return (await response.Content.ReadFromJsonAsync<ModifierResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
