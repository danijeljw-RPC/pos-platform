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
/// Authorization and behaviour tests for <see cref="TaxDefinitionEndpoints"/> and
/// <see cref="TaxDefinitionTemplateEndpoints"/> (PLAN-0004 Milestone C, OI-0007). Every request
/// authenticates via a LocalUsernamePassword session against endpoints configured with
/// <c>rejectStaffPin: true</c> — see <c>StaffPinLoginTests.AssertAllSensitiveEndpointsForbiddenAsync</c>
/// for the staff-PIN-rejection proof shared across every PLAN-0004 Milestone C endpoint, and
/// <c>RequirePermissionFilterTests.cs</c> for the flag's unit-level behaviour, matching the
/// <c>OrganisationEndpointsTests.cs</c> precedent of not re-testing either mechanism per entity.
/// </summary>
public class TaxDefinitionEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public TaxDefinitionEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
        });
    }

    [Fact]
    public async Task Templates_ListsSeededAuNzRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);

        var response = await client.GetAsync("/api/v1/tax-definition-templates");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var templates = await response.Content.ReadFromJsonAsync<List<TaxDefinitionTemplateResponse>>();
        Assert.NotNull(templates);
        Assert.Contains(templates!, t => t.Code == "AU_GST_10" && t.RatePercent == 10m);
        Assert.Contains(templates!, t => t.Code == "AU_GST_FREE" && t.ReceiptMarkerCode == "F");
        Assert.Contains(templates!, t => t.Code == "NZ_GST_15" && t.RatePercent == 15m);
        Assert.Contains(templates!, t => t.Code == "NZ_ZERO_RATED" && t.ReceiptMarkerCode == "Z");
        Assert.Contains(templates!, t => t.Code == "NZ_EXEMPT" && t.ReceiptMarkerCode == "E");
    }

    [Fact]
    public async Task Create_Succeeds_ForOrganisationOwner_AndAppearsInList()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);

        var response = await CreateAuGst10Async(client, caller.OrganisationId, "CUSTOM_AU_10");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaxDefinitionResponse>();
        Assert.NotNull(created);
        Assert.Equal(caller.TenantId, created!.TenantId);
        Assert.Equal(caller.OrganisationId, created.OrganisationId);
        Assert.True(created.IsActive);
        Assert.Null(created.SourceTemplateCode);

        var list = await (await client.GetAsync("/api/v1/tax-definitions")).Content.ReadFromJsonAsync<List<TaxDefinitionResponse>>();
        Assert.Contains(list!, t => t.Id == created.Id);
    }

    [Fact]
    public async Task CreateFromTemplate_ClonesTemplateFields_AndSetsSourceTemplateCode()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);

        var response = await client.PostAsJsonAsync(
            "/api/v1/tax-definitions/from-template",
            new CreateTaxDefinitionFromTemplateRequest(caller.OrganisationId, "AU_GST_10"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaxDefinitionResponse>();
        Assert.NotNull(created);
        Assert.Equal("AU_GST_10", created!.Code);
        Assert.Equal(10m, created.RatePercent);
        Assert.Equal("AU", created.CountryCode);
        Assert.Equal("AU_GST_10", created.SourceTemplateCode);
    }

    [Fact]
    public async Task CreateFromTemplate_Fails_ForUnknownTemplateCode()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);

        var response = await client.PostAsJsonAsync(
            "/api/v1/tax-definitions/from-template",
            new CreateTaxDefinitionFromTemplateRequest(caller.OrganisationId, "NOT_A_REAL_TEMPLATE"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Rejects_DuplicateCodeWithinSameTenant()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        await CreateAuGst10Async(client, caller.OrganisationId, "DUPLICATE_CODE");

        var response = await CreateAuGst10Async(client, caller.OrganisationId, "DUPLICATE_CODE");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsTaxDefinition_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "GET_TEST");

        var response = await client.GetAsync($"/api/v1/tax-definitions/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesRateAndName_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "UPDATE_TEST");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tax-definitions/{created.Id}",
            new UpdateTaxDefinitionRequest(
                "Updated GST", 12.5m, "Australia", TaxJurisdictionType.Country, IncludedInPrice: true,
                TaxRoundingMode.NearestCent, RoundingPrecision: 2, ReceiptMarkerCode: null, ReceiptMarkerLabel: null, ReportingCategory: "GST"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TaxDefinitionResponse>();
        Assert.Equal("Updated GST", updated!.Name);
        Assert.Equal(12.5m, updated.RatePercent);
        // Code/CountryCode are the definition's stable identity and are not affected by Update.
        Assert.Equal("UPDATE_TEST", updated.Code);
        Assert.Equal("AU", updated.CountryCode);
    }

    [Fact]
    public async Task Deactivate_Then_Reactivate_TogglesIsActive_AndListReflectsIt()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "TOGGLE_TEST");

        var deactivateResponse = await client.PostAsync($"/api/v1/tax-definitions/{created.Id}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.False((await deactivateResponse.Content.ReadFromJsonAsync<TaxDefinitionResponse>())!.IsActive);

        var listAfterDeactivate = await (await client.GetAsync("/api/v1/tax-definitions")).Content.ReadFromJsonAsync<List<TaxDefinitionResponse>>();
        Assert.DoesNotContain(listAfterDeactivate!, t => t.Id == created.Id);

        var getInactiveResponse = await client.GetAsync($"/api/v1/tax-definitions/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getInactiveResponse.StatusCode);

        var reactivateResponse = await client.PostAsync($"/api/v1/tax-definitions/{created.Id}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
        Assert.True((await reactivateResponse.Content.ReadFromJsonAsync<TaxDefinitionResponse>())!.IsActive);
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);

        var response = await client.PostAsJsonAsync(
            "/api/v1/tax-definitions",
            new CreateTaxDefinitionRequest(
                "SHOULD_FAIL", "GST", caller.OrganisationId, "AU", null, 10m, "Australia", TaxJurisdictionType.Country,
                true, TaxRoundingMode.NearestCent, 2, TaxCalculationScope.PerLine, null, null, "GST", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var client = _factory.CreateClient();
        // Staff holds only catalog.sold-out-toggle in the catalogue, not catalog.manage.
        var caller = await RbacTestSeeder.SeedAsync(client, "Staff");
        AuthenticateAs(client, caller);

        var response = await CreateAuGst10Async(client, caller.OrganisationId, "NO_PERMISSION_TEST");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

        var response = await client.GetAsync($"/api/v1/tax-definitions/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);

        AuthenticateAs(client, callerB);
        var response = await CreateAuGst10Async(client, callerA.OrganisationId, "CROSS_ORG_TEST");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReadAndUpdateAndDeactivate_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var created = await CreateAndParseAsync(client, callerA.OrganisationId, "CROSS_ORG_RW_TEST");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/tax-definitions/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/tax-definitions/{created.Id}/deactivate", content: null)).StatusCode);

        var list = await (await client.GetAsync("/api/v1/tax-definitions")).Content.ReadFromJsonAsync<List<TaxDefinitionResponse>>();
        Assert.DoesNotContain(list!, t => t.Id == created.Id);
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "AUDIT_TEST");
        await client.PostAsJsonAsync("/api/v1/tax-definitions/from-template", new CreateTaxDefinitionFromTemplateRequest(caller.OrganisationId, "NZ_GST_15"));

        await client.PatchAsJsonAsync(
            $"/api/v1/tax-definitions/{created.Id}",
            new UpdateTaxDefinitionRequest("Audit GST", 10m, "Australia", TaxJurisdictionType.Country, true, TaxRoundingMode.NearestCent, 2, null, null, "GST"));
        await client.PostAsync($"/api/v1/tax-definitions/{created.Id}/deactivate", content: null);
        await client.PostAsync($"/api/v1/tax-definitions/{created.Id}/reactivate", content: null);

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityType == "TaxDefinition" && a.TenantId == caller.TenantId)
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("TaxDefinitionCreated", eventTypes);
        Assert.Contains("TaxDefinitionCreatedFromTemplate", eventTypes);
        Assert.Contains("TaxDefinitionUpdated", eventTypes);
        Assert.Contains("TaxDefinitionDeactivated", eventTypes);
        Assert.Contains("TaxDefinitionReactivated", eventTypes);
    }

    private static Task<HttpResponseMessage> CreateAuGst10Async(HttpClient client, Guid organisationId, string code) =>
        client.PostAsJsonAsync(
            "/api/v1/tax-definitions",
            new CreateTaxDefinitionRequest(
                code, "GST", organisationId, "AU", null, 10m, "Australia", TaxJurisdictionType.Country,
                IncludedInPrice: true, TaxRoundingMode.NearestCent, RoundingPrecision: 2, TaxCalculationScope.PerLine,
                ReceiptMarkerCode: null, ReceiptMarkerLabel: null, ReportingCategory: "GST"));

    private static async Task<TaxDefinitionResponse> CreateAndParseAsync(HttpClient client, Guid organisationId, string code)
    {
        var response = await CreateAuGst10Async(client, organisationId, code);
        return (await response.Content.ReadFromJsonAsync<TaxDefinitionResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
