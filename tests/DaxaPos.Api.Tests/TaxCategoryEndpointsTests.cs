using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Tax;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Domain.Enums;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization and behaviour tests for <see cref="TaxCategoryEndpoints"/> (PLAN-0004 Milestone C,
/// OI-0007). See <see cref="TaxDefinitionEndpointsTests"/>'s class remarks for why staff-PIN
/// rejection and the permission filter's unit-level behaviour aren't re-tested per entity here.
/// </summary>
public class TaxCategoryEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public TaxCategoryEndpointsTests(WebApplicationFactory<Program> factory)
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

        var response = await CreateTaxableAsync(client, caller.OrganisationId, "TAXABLE");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaxCategoryResponse>();
        Assert.NotNull(created);
        Assert.Equal(caller.TenantId, created!.TenantId);
        Assert.Equal(TaxTreatment.Taxable, created.TaxTreatment);
        Assert.True(created.IsActive);

        var list = await (await client.GetAsync("/api/v1/tax-categories")).Content.ReadFromJsonAsync<List<TaxCategoryResponse>>();
        Assert.Contains(list!, c => c.Id == created.Id);
    }

    [Fact]
    public async Task Create_Rejects_DuplicateCodeWithinSameTenant()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        await CreateTaxableAsync(client, caller.OrganisationId, "DUP_CATEGORY");

        var response = await CreateTaxableAsync(client, caller.OrganisationId, "DUP_CATEGORY");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsTaxCategory_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "GET_CATEGORY");

        var response = await client.GetAsync($"/api/v1/tax-categories/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesNameAndTreatment_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "UPDATE_CATEGORY");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tax-categories/{created.Id}",
            new UpdateTaxCategoryRequest("GST-Free", TaxTreatment.GSTFree));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TaxCategoryResponse>();
        Assert.Equal("GST-Free", updated!.Name);
        Assert.Equal(TaxTreatment.GSTFree, updated.TaxTreatment);
        Assert.Equal("UPDATE_CATEGORY", updated.Code);
    }

    [Fact]
    public async Task Deactivate_Then_Reactivate_TogglesIsActive_AndListReflectsIt()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "TOGGLE_CATEGORY");

        var deactivateResponse = await client.PostAsync($"/api/v1/tax-categories/{created.Id}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.False((await deactivateResponse.Content.ReadFromJsonAsync<TaxCategoryResponse>())!.IsActive);

        var listAfterDeactivate = await (await client.GetAsync("/api/v1/tax-categories")).Content.ReadFromJsonAsync<List<TaxCategoryResponse>>();
        Assert.DoesNotContain(listAfterDeactivate!, c => c.Id == created.Id);

        var reactivateResponse = await client.PostAsync($"/api/v1/tax-categories/{created.Id}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
        Assert.True((await reactivateResponse.Content.ReadFromJsonAsync<TaxCategoryResponse>())!.IsActive);
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);

        var response = await client.PostAsJsonAsync(
            "/api/v1/tax-categories",
            new CreateTaxCategoryRequest("SHOULD_FAIL", "Taxable", caller.OrganisationId, TaxTreatment.Taxable, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "Staff");
        AuthenticateAs(client, caller);

        var response = await CreateTaxableAsync(client, caller.OrganisationId, "NO_PERMISSION_CATEGORY");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Read_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, owner);
        var created = await CreateAndParseAsync(client, owner.OrganisationId, "CROSS_TENANT_CATEGORY");

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, otherTenantCaller);

        var response = await client.GetAsync($"/api/v1/tax-categories/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);

        AuthenticateAs(client, callerB);
        var response = await CreateTaxableAsync(client, callerA.OrganisationId, "CROSS_ORG_CATEGORY");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "AUDIT_CATEGORY");

        await client.PatchAsJsonAsync($"/api/v1/tax-categories/{created.Id}", new UpdateTaxCategoryRequest("Renamed", TaxTreatment.ZeroRated));
        await client.PostAsync($"/api/v1/tax-categories/{created.Id}/deactivate", content: null);
        await client.PostAsync($"/api/v1/tax-categories/{created.Id}/reactivate", content: null);

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "TaxCategory")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("TaxCategoryCreated", eventTypes);
        Assert.Contains("TaxCategoryUpdated", eventTypes);
        Assert.Contains("TaxCategoryDeactivated", eventTypes);
        Assert.Contains("TaxCategoryReactivated", eventTypes);
    }

    private static Task<HttpResponseMessage> CreateTaxableAsync(HttpClient client, Guid organisationId, string code) =>
        client.PostAsJsonAsync("/api/v1/tax-categories", new CreateTaxCategoryRequest(code, "Taxable", organisationId, TaxTreatment.Taxable));

    private static async Task<TaxCategoryResponse> CreateAndParseAsync(HttpClient client, Guid organisationId, string code)
    {
        var response = await CreateTaxableAsync(client, organisationId, code);
        return (await response.Content.ReadFromJsonAsync<TaxCategoryResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
