using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Endpoints.Tax;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Application.Tax;
using DaxaPos.Domain.Enums;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization and behaviour tests for <see cref="TaxCategoryDefinitionEndpoints"/> (PLAN-0004
/// Milestone C). Unlike <see cref="TaxDefinitionEndpointsTests"/>/<see cref="TaxCategoryEndpointsTests"/>,
/// this mapping row has no <c>OrganisationId</c> column of its own, so every cross-organisation
/// check here walks through its referenced <see cref="TaxCategory"/>/<see cref="TaxDefinition"/>/
/// <see cref="Location"/> — see <c>Create_Blocked_When*BelongsToDifferentOrganisation</c> below.
/// </summary>
public class TaxCategoryDefinitionEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public TaxCategoryDefinitionEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
        });
    }

    [Fact]
    public async Task Create_Succeeds_ForOrganisationWideMapping_AndAppearsInList()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (category, definition) = await CreateCategoryAndDefinitionAsync(client, caller.OrganisationId, "ORGWIDE");

        var response = await client.PostAsJsonAsync(
            "/api/v1/tax-category-definitions",
            new CreateTaxCategoryDefinitionRequest(category.Id, definition.Id, LocationId: null, Priority: 0));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaxCategoryDefinitionResponse>();
        Assert.NotNull(created);
        Assert.Null(created!.LocationId);
        Assert.True(created.IsActive);

        var list = await (await client.GetAsync("/api/v1/tax-category-definitions")).Content.ReadFromJsonAsync<List<TaxCategoryDefinitionResponse>>();
        Assert.Contains(list!, d => d.Id == created.Id);
    }

    [Fact]
    public async Task Create_Succeeds_ForLocationScopedMapping()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (category, definition) = await CreateCategoryAndDefinitionAsync(client, caller.OrganisationId, "LOCSCOPED");
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "Mapping Test Venue");

        var response = await client.PostAsJsonAsync(
            "/api/v1/tax-category-definitions",
            new CreateTaxCategoryDefinitionRequest(category.Id, definition.Id, location.Id, Priority: 0));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaxCategoryDefinitionResponse>();
        Assert.Equal(location.Id, created!.LocationId);
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (category, definition) = await CreateCategoryAndDefinitionAsync(client, caller.OrganisationId, "TENANTID_TRAP");

        var response = await client.PostAsJsonAsync(
            "/api/v1/tax-category-definitions",
            new CreateTaxCategoryDefinitionRequest(category.Id, definition.Id, null, 0, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var owner = _factory.CreateClient();
        var ownerCaller = await RbacTestSeeder.SeedAsync(owner, "OrganisationOwner");
        AuthenticateAs(owner, ownerCaller);
        var (category, definition) = await CreateCategoryAndDefinitionAsync(owner, ownerCaller.OrganisationId, "NO_PERMISSION_MAPPING");

        var staffClient = _factory.CreateClient();
        var staffCaller = await RbacTestSeeder.SeedAsync(staffClient, "Staff", ownerCaller.TenantId);
        AuthenticateAs(staffClient, staffCaller);

        var response = await staffClient.PostAsJsonAsync(
            "/api/v1/tax-category-definitions",
            new CreateTaxCategoryDefinitionRequest(category.Id, definition.Id, null, 0));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_Blocked_WhenTaxCategoryBelongsToDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var (categoryA, _) = await CreateCategoryAndDefinitionAsync(client, callerA.OrganisationId, "ORG_A_CATEGORY");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);
        var (_, definitionB) = await CreateCategoryAndDefinitionAsync(client, callerB.OrganisationId, "ORG_B_DEFINITION");

        // callerB tries to map their own definition against callerA's category.
        var response = await client.PostAsJsonAsync(
            "/api/v1/tax-category-definitions",
            new CreateTaxCategoryDefinitionRequest(categoryA.Id, definitionB.Id, null, 0));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Blocked_WhenTaxDefinitionBelongsToDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var (categoryA, definitionA) = await CreateCategoryAndDefinitionAsync(client, callerA.OrganisationId, "ORG_A_BOTH");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        // callerB tries to map callerA's own definition to callerA's category, from callerB's session.
        var response = await client.PostAsJsonAsync(
            "/api/v1/tax-category-definitions",
            new CreateTaxCategoryDefinitionRequest(categoryA.Id, definitionA.Id, null, 0));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Blocked_WhenLocationBelongsToDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var locationA = await DeviceTestHelper.CreateLocationAsync(client, callerA.OrganisationId, "Org A Venue");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);
        var (categoryB, definitionB) = await CreateCategoryAndDefinitionAsync(client, callerB.OrganisationId, "ORG_B_LOCATION_TEST");

        // callerB's own category/definition, but locationA belongs to a different organisation.
        var response = await client.PostAsJsonAsync(
            "/api/v1/tax-category-definitions",
            new CreateTaxCategoryDefinitionRequest(categoryB.Id, definitionB.Id, locationA.Id, 0));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_FailsWithBadRequest_WhenExceedingTenComponentLimitForSameCategoryAndLocation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var category = await CreateCategoryAsync(client, caller.OrganisationId, "LIMIT_CATEGORY");

        for (var i = 0; i < TaxCalculationEngine.MaxComponentsPerLine; i++)
        {
            var definition = await CreateDefinitionAsync(client, caller.OrganisationId, $"LIMIT_DEF_{i}");
            var response = await client.PostAsJsonAsync(
                "/api/v1/tax-category-definitions",
                new CreateTaxCategoryDefinitionRequest(category.Id, definition.Id, null, i));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var eleventhDefinition = await CreateDefinitionAsync(client, caller.OrganisationId, "LIMIT_DEF_ELEVENTH");
        var overLimitResponse = await client.PostAsJsonAsync(
            "/api/v1/tax-category-definitions",
            new CreateTaxCategoryDefinitionRequest(category.Id, eleventhDefinition.Id, null, TaxCalculationEngine.MaxComponentsPerLine));

        Assert.Equal(HttpStatusCode.BadRequest, overLimitResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesMapping_AndAuditsEvent()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (category, definition) = await CreateCategoryAndDefinitionAsync(client, caller.OrganisationId, "DELETE_TEST");
        var created = await CreateMappingAndParseAsync(client, category.Id, definition.Id, null, 0);

        var response = await client.DeleteAsync($"/api/v1/tax-category-definitions/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var list = await (await client.GetAsync("/api/v1/tax-category-definitions")).Content.ReadFromJsonAsync<List<TaxCategoryDefinitionResponse>>();
        Assert.DoesNotContain(list!, d => d.Id == created.Id);

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "TaxCategoryDefinition")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("TaxCategoryDefinitionCreated", eventTypes);
        Assert.Contains("TaxCategoryDefinitionDeleted", eventTypes);
    }

    [Fact]
    public async Task Delete_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var (categoryA, definitionA) = await CreateCategoryAndDefinitionAsync(client, callerA.OrganisationId, "DELETE_CROSS_ORG");
        var created = await CreateMappingAndParseAsync(client, categoryA.Id, definitionA.Id, null, 0);

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        var response = await client.DeleteAsync($"/api/v1/tax-category-definitions/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_ExcludesRowsFromOtherOrganisations()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var (categoryA, definitionA) = await CreateCategoryAndDefinitionAsync(client, callerA.OrganisationId, "LIST_ISOLATION_A");
        var createdA = await CreateMappingAndParseAsync(client, categoryA.Id, definitionA.Id, null, 0);

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        var list = await (await client.GetAsync("/api/v1/tax-category-definitions")).Content.ReadFromJsonAsync<List<TaxCategoryDefinitionResponse>>();
        Assert.DoesNotContain(list!, d => d.Id == createdA.Id);
    }

    private async Task<(TaxCategoryResponse Category, TaxDefinitionResponse Definition)> CreateCategoryAndDefinitionAsync(HttpClient client, Guid organisationId, string codeSuffix)
    {
        var category = await CreateCategoryAsync(client, organisationId, codeSuffix);
        var definition = await CreateDefinitionAsync(client, organisationId, codeSuffix);
        return (category, definition);
    }

    private static async Task<TaxCategoryResponse> CreateCategoryAsync(HttpClient client, Guid organisationId, string codeSuffix)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/tax-categories",
            new CreateTaxCategoryRequest($"CAT_{codeSuffix}", "Taxable", organisationId, TaxTreatment.Taxable));
        return (await response.Content.ReadFromJsonAsync<TaxCategoryResponse>())!;
    }

    private static async Task<TaxDefinitionResponse> CreateDefinitionAsync(HttpClient client, Guid organisationId, string codeSuffix)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/tax-definitions",
            new CreateTaxDefinitionRequest(
                $"DEF_{codeSuffix}", "GST", organisationId, "AU", null, 10m, "Australia", TaxJurisdictionType.Country,
                IncludedInPrice: true, TaxRoundingMode.NearestCent, RoundingPrecision: 2, TaxCalculationScope.PerLine,
                ReceiptMarkerCode: null, ReceiptMarkerLabel: null, ReportingCategory: "GST"));
        return (await response.Content.ReadFromJsonAsync<TaxDefinitionResponse>())!;
    }

    private static async Task<TaxCategoryDefinitionResponse> CreateMappingAndParseAsync(HttpClient client, Guid taxCategoryId, Guid taxDefinitionId, Guid? locationId, int priority)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/tax-category-definitions",
            new CreateTaxCategoryDefinitionRequest(taxCategoryId, taxDefinitionId, locationId, priority));
        return (await response.Content.ReadFromJsonAsync<TaxCategoryDefinitionResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
