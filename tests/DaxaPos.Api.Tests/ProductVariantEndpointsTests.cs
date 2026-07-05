using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Catalog;
using DaxaPos.Api.Endpoints.Tax;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Domain.Enums;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization and behaviour tests for <see cref="ProductVariantEndpoints"/> (PLAN-0004
/// Milestone E). See <see cref="TaxDefinitionEndpointsTests"/>'s class remarks for why staff-PIN
/// rejection and the permission filter's unit-level behaviour aren't re-tested per entity here.
/// </summary>
public class ProductVariantEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public ProductVariantEndpointsTests(WebApplicationFactory<Program> factory)
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
        var product = await CreateProductAsync(client, caller.OrganisationId, "CREATE_TEST");

        var response = await CreateVariantAsync(client, product.Id, "Large", 1.00m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProductVariantResponse>();
        Assert.NotNull(created);
        Assert.Equal(caller.TenantId, created!.TenantId);
        Assert.True(created.IsActive);

        var list = await (await client.GetAsync("/api/v1/product-variants")).Content.ReadFromJsonAsync<List<ProductVariantResponse>>();
        Assert.Contains(list!, v => v.Id == created.Id);
    }

    [Theory]
    [InlineData(-2.50)]
    [InlineData(0)]
    [InlineData(3.00)]
    public async Task Create_AcceptsNegativeZeroAndPositivePriceDeltas(decimal priceDelta)
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var product = await CreateProductAsync(client, caller.OrganisationId, $"DELTA_TEST_{priceDelta}");

        var response = await CreateVariantAsync(client, product.Id, "Variant", priceDelta);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProductVariantResponse>();
        Assert.Equal(priceDelta, created!.PriceDelta);
    }

    [Fact]
    public async Task Create_Rejects_ProductFromDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var productA = await CreateProductAsync(client, callerA.OrganisationId, "ORG_A_PRODUCT");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        var response = await CreateVariantAsync(client, productA.Id, "Should Fail", 1.00m);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsVariant_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var product = await CreateProductAsync(client, caller.OrganisationId, "GET_TEST");
        var created = await CreateAndParseVariantAsync(client, product.Id, "Get Variant", 1.00m);

        var response = await client.GetAsync($"/api/v1/product-variants/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesNamePriceDeltaAndSku_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var product = await CreateProductAsync(client, caller.OrganisationId, "UPDATE_TEST");
        var created = await CreateAndParseVariantAsync(client, product.Id, "Update Variant", 1.00m);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/product-variants/{created.Id}",
            new UpdateProductVariantRequest("Renamed Variant", -0.50m, "SKU-V1"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProductVariantResponse>();
        Assert.Equal("Renamed Variant", updated!.Name);
        Assert.Equal(-0.50m, updated.PriceDelta);
        Assert.Equal("SKU-V1", updated.Sku);
    }

    [Fact]
    public async Task Deactivate_Then_Reactivate_TogglesIsActive_AndListReflectsIt()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var product = await CreateProductAsync(client, caller.OrganisationId, "TOGGLE_TEST");
        var created = await CreateAndParseVariantAsync(client, product.Id, "Toggle Variant", 1.00m);

        var deactivateResponse = await client.PostAsync($"/api/v1/product-variants/{created.Id}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.False((await deactivateResponse.Content.ReadFromJsonAsync<ProductVariantResponse>())!.IsActive);

        var listAfterDeactivate = await (await client.GetAsync("/api/v1/product-variants")).Content.ReadFromJsonAsync<List<ProductVariantResponse>>();
        Assert.DoesNotContain(listAfterDeactivate!, v => v.Id == created.Id);

        var reactivateResponse = await client.PostAsync($"/api/v1/product-variants/{created.Id}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
        Assert.True((await reactivateResponse.Content.ReadFromJsonAsync<ProductVariantResponse>())!.IsActive);
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var product = await CreateProductAsync(client, caller.OrganisationId, "TENANTID_TRAP");

        var response = await client.PostAsJsonAsync(
            "/api/v1/product-variants",
            new CreateProductVariantRequest("Should Fail", product.Id, 1.00m, null, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(ownerClient, "OrganisationOwner");
        AuthenticateAs(ownerClient, owner);
        var product = await CreateProductAsync(ownerClient, owner.OrganisationId, "NO_PERMISSION_TEST");

        var staffClient = _factory.CreateClient();
        var staffCaller = await RbacTestSeeder.SeedAsync(staffClient, "Staff", owner.TenantId);
        AuthenticateAs(staffClient, staffCaller);

        var response = await CreateVariantAsync(staffClient, product.Id, "Should Fail", 1.00m);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Read_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, owner);
        var product = await CreateProductAsync(client, owner.OrganisationId, "CROSS_TENANT_TEST");
        var created = await CreateAndParseVariantAsync(client, product.Id, "Cross Tenant Variant", 1.00m);

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, otherTenantCaller);

        var response = await client.GetAsync($"/api/v1/product-variants/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReadAndUpdateAndDeactivate_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var product = await CreateProductAsync(client, callerA.OrganisationId, "CROSS_ORG_RW_TEST");
        var created = await CreateAndParseVariantAsync(client, product.Id, "Cross Org Variant", 1.00m);

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/product-variants/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/product-variants/{created.Id}/deactivate", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync(
            $"/api/v1/product-variants/{created.Id}", new UpdateProductVariantRequest("Hijacked", 0m, null))).StatusCode);
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var product = await CreateProductAsync(client, caller.OrganisationId, "AUDIT_TEST");
        var created = await CreateAndParseVariantAsync(client, product.Id, "Audit Variant", 1.00m);

        await client.PatchAsJsonAsync($"/api/v1/product-variants/{created.Id}", new UpdateProductVariantRequest("Audit Renamed", 2.00m, null));
        await client.PostAsync($"/api/v1/product-variants/{created.Id}/deactivate", content: null);
        await client.PostAsync($"/api/v1/product-variants/{created.Id}/reactivate", content: null);

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "ProductVariant")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("ProductVariantCreated", eventTypes);
        Assert.Contains("ProductVariantUpdated", eventTypes);
        Assert.Contains("ProductVariantDeactivated", eventTypes);
        Assert.Contains("ProductVariantReactivated", eventTypes);
    }

    private static async Task<ProductResponse> CreateProductAsync(HttpClient client, Guid organisationId, string codeSuffix)
    {
        var categoryResponse = await client.PostAsJsonAsync("/api/v1/product-categories", new CreateProductCategoryRequest($"Category {codeSuffix}", 0, organisationId));
        var category = (await categoryResponse.Content.ReadFromJsonAsync<ProductCategoryResponse>())!;

        var taxCategoryResponse = await client.PostAsJsonAsync(
            "/api/v1/tax-categories", new CreateTaxCategoryRequest($"TAXCAT_{codeSuffix}", "Taxable", organisationId, TaxTreatment.Taxable));
        var taxCategory = (await taxCategoryResponse.Content.ReadFromJsonAsync<TaxCategoryResponse>())!;

        var productResponse = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductRequest($"Product {codeSuffix}", organisationId, category.Id, taxCategory.Id, null, null, null, 5.00m));
        return (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    private static Task<HttpResponseMessage> CreateVariantAsync(HttpClient client, Guid productId, string name, decimal priceDelta) =>
        client.PostAsJsonAsync("/api/v1/product-variants", new CreateProductVariantRequest(name, productId, priceDelta, null));

    private static async Task<ProductVariantResponse> CreateAndParseVariantAsync(HttpClient client, Guid productId, string name, decimal priceDelta)
    {
        var response = await CreateVariantAsync(client, productId, name, priceDelta);
        return (await response.Content.ReadFromJsonAsync<ProductVariantResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
