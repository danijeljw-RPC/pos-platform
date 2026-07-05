using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Catalog;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Endpoints.Tax;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Domain.Enums;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization and behaviour tests for <see cref="ProductLocationOverrideEndpoints"/> (PLAN-0004
/// Milestone F) — the full record, gated <c>pricing.manage</c> + <c>rejectStaffPin: true</c>
/// (confirmed against the plan's exact permission table before implementation, not
/// <c>catalog.manage</c>). See <see cref="ProductSoldOutEndpointsTests"/> for the separate,
/// narrower, staff-accessible sold-out toggle.
/// </summary>
public class ProductLocationOverrideEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public ProductLocationOverrideEndpointsTests(WebApplicationFactory<Program> factory)
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
        var (product, location) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "CREATE_TEST");

        var response = await CreateOverrideAsync(client, product.Id, location.Id, true, false, 7.50m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProductLocationOverrideResponse>();
        Assert.NotNull(created);
        Assert.Equal(product.Id, created!.ProductId);
        Assert.Equal(location.Id, created.LocationId);
        Assert.Equal(7.50m, created.PriceOverride);

        var list = await (await client.GetAsync("/api/v1/product-location-overrides")).Content.ReadFromJsonAsync<List<ProductLocationOverrideResponse>>();
        Assert.Contains(list!, o => o.Id == created.Id);
    }

    [Fact]
    public async Task Create_Rejects_ProductFromDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var (productA, _) = await CreatePrerequisitesAsync(client, callerA.OrganisationId, "ORG_A_PRODUCT");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);
        var (_, locationB) = await CreatePrerequisitesAsync(client, callerB.OrganisationId, "ORG_B_LOCATION");

        AuthenticateAs(client, callerA);
        var response = await CreateOverrideAsync(client, productA.Id, locationB.Id, true, false, null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Rejects_LocationFromDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var (productA, locationA) = await CreatePrerequisitesAsync(client, callerA.OrganisationId, "ORG_A_BOTH");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        var response = await CreateOverrideAsync(client, productA.Id, locationA.Id, true, false, null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Rejects_DuplicateProductLocationPair()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (product, location) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "DUP_TEST");
        await CreateOverrideAsync(client, product.Id, location.Id, true, false, null);

        var response = await CreateOverrideAsync(client, product.Id, location.Id, true, false, null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsOverride_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "GET_TEST");

        var response = await client.GetAsync($"/api/v1/product-location-overrides/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesFields_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "UPDATE_TEST");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/product-location-overrides/{created.Id}",
            new UpdateProductLocationOverrideRequest(false, true, 9.99m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProductLocationOverrideResponse>();
        Assert.False(updated!.IsAvailable);
        Assert.True(updated.IsSoldOut);
        Assert.Equal(9.99m, updated.PriceOverride);
    }

    [Fact]
    public async Task Delete_RemovesOverride()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "DELETE_TEST");

        var response = await client.DeleteAsync($"/api/v1/product-location-overrides/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var list = await (await client.GetAsync("/api/v1/product-location-overrides")).Content.ReadFromJsonAsync<List<ProductLocationOverrideResponse>>();
        Assert.DoesNotContain(list!, o => o.Id == created.Id);
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (product, location) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "TENANTID_TRAP");

        var response = await client.PostAsJsonAsync(
            "/api/v1/product-location-overrides",
            new CreateProductLocationOverrideRequest(product.Id, location.Id, true, false, null, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Rejects_NegativePriceOverride()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (product, location) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "NEGATIVE_PRICE_TEST");

        var response = await CreateOverrideAsync(client, product.Id, location.Id, true, false, -1.00m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(ownerClient, "OrganisationOwner");
        AuthenticateAs(ownerClient, owner);
        var (product, location) = await CreatePrerequisitesAsync(ownerClient, owner.OrganisationId, "NO_PERMISSION_TEST");

        var staffClient = _factory.CreateClient();
        var staffCaller = await RbacTestSeeder.SeedAsync(staffClient, "Staff", owner.TenantId);
        AuthenticateAs(staffClient, staffCaller);

        var response = await CreateOverrideAsync(staffClient, product.Id, location.Id, true, false, null);

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

        var response = await client.GetAsync($"/api/v1/product-location-overrides/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReadAndUpdateAndDelete_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var created = await CreateAndParseAsync(client, callerA.OrganisationId, "CROSS_ORG_RW_TEST");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/product-location-overrides/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync(
            $"/api/v1/product-location-overrides/{created.Id}", new UpdateProductLocationOverrideRequest(true, false, null))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/v1/product-location-overrides/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "AUDIT_TEST");

        await client.PatchAsJsonAsync($"/api/v1/product-location-overrides/{created.Id}", new UpdateProductLocationOverrideRequest(false, true, 5.00m));
        await client.DeleteAsync($"/api/v1/product-location-overrides/{created.Id}");

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "ProductLocationOverride")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("ProductLocationOverrideCreated", eventTypes);
        Assert.Contains("ProductLocationOverrideUpdated", eventTypes);
        Assert.Contains("ProductLocationOverrideDeleted", eventTypes);
    }

    private async Task<(ProductResponse Product, LocationResponse Location)> CreatePrerequisitesAsync(HttpClient client, Guid organisationId, string codeSuffix)
    {
        var categoryResponse = await client.PostAsJsonAsync("/api/v1/product-categories", new CreateProductCategoryRequest($"Category {codeSuffix}", 0, organisationId));
        var category = (await categoryResponse.Content.ReadFromJsonAsync<ProductCategoryResponse>())!;

        var taxCategoryResponse = await client.PostAsJsonAsync(
            "/api/v1/tax-categories", new CreateTaxCategoryRequest($"TAXCAT_{codeSuffix}", "Taxable", organisationId, TaxTreatment.Taxable));
        var taxCategory = (await taxCategoryResponse.Content.ReadFromJsonAsync<TaxCategoryResponse>())!;

        var productResponse = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductRequest($"Product {codeSuffix}", organisationId, category.Id, taxCategory.Id, null, null, null, 5.00m));
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;

        var location = await DeviceTestHelper.CreateLocationAsync(client, organisationId, $"Venue {codeSuffix}");

        return (product, location);
    }

    private static Task<HttpResponseMessage> CreateOverrideAsync(HttpClient client, Guid productId, Guid locationId, bool isAvailable, bool isSoldOut, decimal? priceOverride) =>
        client.PostAsJsonAsync("/api/v1/product-location-overrides", new CreateProductLocationOverrideRequest(productId, locationId, isAvailable, isSoldOut, priceOverride));

    private async Task<ProductLocationOverrideResponse> CreateAndParseAsync(HttpClient client, Guid organisationId, string codeSuffix)
    {
        var (product, location) = await CreatePrerequisitesAsync(client, organisationId, codeSuffix);
        var response = await CreateOverrideAsync(client, product.Id, location.Id, true, false, null);
        return (await response.Content.ReadFromJsonAsync<ProductLocationOverrideResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
