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
/// Authorization and behaviour tests for <see cref="ProductEndpoints"/> (PLAN-0004 Milestone D,
/// OI-0007) — including the archive-and-replace rule for <c>TaxCategoryId</c>-changing updates. See
/// <see cref="TaxDefinitionEndpointsTests"/>'s class remarks for why staff-PIN rejection and the
/// permission filter's unit-level behaviour aren't re-tested per entity here.
/// </summary>
public class ProductEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public ProductEndpointsTests(WebApplicationFactory<Program> factory)
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
        var (category, taxCategory) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "CREATE_TEST");

        var response = await CreateProductAsync(client, caller.OrganisationId, category.Id, taxCategory.Id, "Flat White");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(created);
        Assert.Equal(caller.TenantId, created!.TenantId);
        Assert.True(created.IsActive);
        Assert.False(created.IsArchived);
        Assert.Null(created.SupersededByProductId);
        Assert.Null(created.PreviousProductId);

        var list = await (await client.GetAsync("/api/v1/products")).Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.Contains(list!, p => p.Id == created.Id);
    }

    [Fact]
    public async Task Create_Rejects_TaxCategoryFromDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var (categoryA, _) = await CreatePrerequisitesAsync(client, callerA.OrganisationId, "ORG_A_TAX_TEST");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);
        var (_, taxCategoryB) = await CreatePrerequisitesAsync(client, callerB.OrganisationId, "ORG_B_TAX_TEST");

        // callerA's own product category, but callerB's tax category — the tax category reference must be rejected.
        AuthenticateAs(client, callerA);
        var response = await CreateProductAsync(client, callerA.OrganisationId, categoryA.Id, taxCategoryB.Id, "Should Fail");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Accepts_TaxCategoryFromSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (category, taxCategory) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "SAME_ORG_TAX_TEST");

        var response = await CreateProductAsync(client, caller.OrganisationId, category.Id, taxCategory.Id, "Same Org Product");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(taxCategory.Id, created!.TaxCategoryId);
    }

    [Fact]
    public async Task Create_Rejects_ProductCategoryFromDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var (categoryA, taxCategoryA) = await CreatePrerequisitesAsync(client, callerA.OrganisationId, "ORG_A_CAT_TEST");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        // callerB tries to create a product against callerA's ProductCategory.
        var response = await CreateProductAsync(client, callerB.OrganisationId, categoryA.Id, taxCategoryA.Id, "Should Fail");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_AllowsDuplicateSkuAndBarcode_NoUniquenessConstraint()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (category, taxCategory) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "DUP_SKU_TEST");

        var first = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductRequest("Product One", caller.OrganisationId, category.Id, taxCategory.Id, null, "SKU-123", "BARCODE-123", 5.00m));
        var second = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductRequest("Product Two", caller.OrganisationId, category.Id, taxCategory.Id, null, "SKU-123", "BARCODE-123", 6.00m));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsProduct_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseProductAsync(client, caller.OrganisationId, "GET_TEST");

        var response = await client.GetAsync($"/api/v1/products/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_NonTaxAffectingChange_UpdatesInPlace_AndDoesNotArchive()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseProductAsync(client, caller.OrganisationId, "IN_PLACE_TEST");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/products/{created.Id}",
            new UpdateProductRequest("Renamed Product", created.ProductCategoryId, created.TaxCategoryId, "New description", created.Sku, created.Barcode, 7.50m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal("Renamed Product", updated.Name);
        Assert.Equal(7.50m, updated.BasePrice);
        Assert.False(updated.IsArchived);
        Assert.Null(updated.SupersededByProductId);
    }

    [Fact]
    public async Task Update_TaxCategoryChange_ArchivesOldRow_AndCreatesReplacementWithLink()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseProductAsync(client, caller.OrganisationId, "ARCHIVE_REPLACE_TEST");
        var newTaxCategory = await CreateTaxCategoryAsync(client, caller.OrganisationId, "ARCHIVE_REPLACE_NEW_TAX");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/products/{created.Id}",
            new UpdateProductRequest("Renamed Product", created.ProductCategoryId, newTaxCategory.Id, created.Description, created.Sku, created.Barcode, created.BasePrice));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var replacement = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(replacement);
        Assert.NotEqual(created.Id, replacement!.Id);
        Assert.Equal(newTaxCategory.Id, replacement.TaxCategoryId);
        Assert.Equal("Renamed Product", replacement.Name);
        Assert.False(replacement.IsArchived);
        Assert.Equal(created.Id, replacement.PreviousProductId);

        var oldRow = await (await client.GetAsync($"/api/v1/products/{created.Id}")).Content.ReadFromJsonAsync<ProductResponse>();
        Assert.True(oldRow!.IsArchived);
        Assert.NotNull(oldRow.ArchivedAtUtc);
        Assert.Equal(replacement.Id, oldRow.SupersededByProductId);

        // The archived row is a permanent historical record — it must not disappear from the API.
        var getArchivedResponse = await client.GetAsync($"/api/v1/products/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getArchivedResponse.StatusCode);

        // But it must not appear in the default (active, non-archived) list.
        var list = await (await client.GetAsync("/api/v1/products")).Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.DoesNotContain(list!, p => p.Id == created.Id);
        Assert.Contains(list!, p => p.Id == replacement.Id);
    }

    [Fact]
    public async Task Update_OnAnAlreadyArchivedProduct_IsRejectedWithConflict()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseProductAsync(client, caller.OrganisationId, "ARCHIVED_UPDATE_TEST");
        var newTaxCategory = await CreateTaxCategoryAsync(client, caller.OrganisationId, "ARCHIVED_UPDATE_NEW_TAX");
        await client.PatchAsJsonAsync(
            $"/api/v1/products/{created.Id}",
            new UpdateProductRequest(created.Name, created.ProductCategoryId, newTaxCategory.Id, created.Description, created.Sku, created.Barcode, created.BasePrice));

        // created.Id is now archived — any further PATCH against it must be rejected.
        var response = await client.PatchAsJsonAsync(
            $"/api/v1/products/{created.Id}",
            new UpdateProductRequest("Should Fail", created.ProductCategoryId, created.TaxCategoryId, created.Description, created.Sku, created.Barcode, created.BasePrice));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateAndReactivate_OnAnArchivedProduct_IsRejectedWithConflict()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseProductAsync(client, caller.OrganisationId, "ARCHIVED_TOGGLE_TEST");
        var newTaxCategory = await CreateTaxCategoryAsync(client, caller.OrganisationId, "ARCHIVED_TOGGLE_NEW_TAX");
        await client.PatchAsJsonAsync(
            $"/api/v1/products/{created.Id}",
            new UpdateProductRequest(created.Name, created.ProductCategoryId, newTaxCategory.Id, created.Description, created.Sku, created.Barcode, created.BasePrice));

        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/v1/products/{created.Id}/deactivate", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/v1/products/{created.Id}/reactivate", content: null)).StatusCode);
    }

    [Fact]
    public async Task Deactivate_Then_Reactivate_TogglesIsActive_AndListReflectsIt()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseProductAsync(client, caller.OrganisationId, "DEACTIVATE_TEST");

        var deactivateResponse = await client.PostAsync($"/api/v1/products/{created.Id}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.False((await deactivateResponse.Content.ReadFromJsonAsync<ProductResponse>())!.IsActive);

        var listAfterDeactivate = await (await client.GetAsync("/api/v1/products")).Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.DoesNotContain(listAfterDeactivate!, p => p.Id == created.Id);

        var reactivateResponse = await client.PostAsync($"/api/v1/products/{created.Id}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
        Assert.True((await reactivateResponse.Content.ReadFromJsonAsync<ProductResponse>())!.IsActive);
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (category, taxCategory) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "TENANTID_TRAP");

        var response = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductRequest("Should Fail", caller.OrganisationId, category.Id, taxCategory.Id, null, null, null, 5.00m, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(ownerClient, "OrganisationOwner");
        AuthenticateAs(ownerClient, owner);
        var (category, taxCategory) = await CreatePrerequisitesAsync(ownerClient, owner.OrganisationId, "NO_PERMISSION_TEST");

        var staffClient = _factory.CreateClient();
        var staffCaller = await RbacTestSeeder.SeedAsync(staffClient, "Staff", owner.TenantId);
        AuthenticateAs(staffClient, staffCaller);

        var response = await CreateProductAsync(staffClient, owner.OrganisationId, category.Id, taxCategory.Id, "Should Fail");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Read_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, owner);
        var created = await CreateAndParseProductAsync(client, owner.OrganisationId, "CROSS_TENANT_TEST");

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, otherTenantCaller);

        var response = await client.GetAsync($"/api/v1/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReadAndUpdateAndDeactivate_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var created = await CreateAndParseProductAsync(client, callerA.OrganisationId, "CROSS_ORG_RW_TEST");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/products/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/products/{created.Id}/deactivate", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync(
            $"/api/v1/products/{created.Id}",
            new UpdateProductRequest(created.Name, created.ProductCategoryId, created.TaxCategoryId, created.Description, created.Sku, created.Barcode, created.BasePrice))).StatusCode);

        var list = await (await client.GetAsync("/api/v1/products")).Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.DoesNotContain(list!, p => p.Id == created.Id);
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseProductAsync(client, caller.OrganisationId, "AUDIT_TEST");
        var newTaxCategory = await CreateTaxCategoryAsync(client, caller.OrganisationId, "AUDIT_NEW_TAX");

        await client.PostAsync($"/api/v1/products/{created.Id}/deactivate", content: null);
        await client.PostAsync($"/api/v1/products/{created.Id}/reactivate", content: null);
        var replaceResponse = await client.PatchAsJsonAsync(
            $"/api/v1/products/{created.Id}",
            new UpdateProductRequest(created.Name, created.ProductCategoryId, newTaxCategory.Id, created.Description, created.Sku, created.Barcode, created.BasePrice));
        var replacement = await replaceResponse.Content.ReadFromJsonAsync<ProductResponse>();

        await using var context = CreateDbContext();
        var oldRowEvents = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "Product")
            .Select(a => a.EventType)
            .ToListAsync();
        var newRowEvents = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == replacement!.Id && a.EntityType == "Product")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("ProductCreated", oldRowEvents);
        Assert.Contains("ProductDeactivated", oldRowEvents);
        Assert.Contains("ProductReactivated", oldRowEvents);
        Assert.Contains("ProductArchived", oldRowEvents);
        Assert.Contains("ProductCreatedFromReplace", newRowEvents);
    }

    private async Task<(ProductCategoryResponse Category, TaxCategoryResponse TaxCategory)> CreatePrerequisitesAsync(HttpClient client, Guid organisationId, string codeSuffix)
    {
        var categoryResponse = await client.PostAsJsonAsync("/api/v1/product-categories", new CreateProductCategoryRequest($"Category {codeSuffix}", 0, organisationId));
        var category = (await categoryResponse.Content.ReadFromJsonAsync<ProductCategoryResponse>())!;
        var taxCategory = await CreateTaxCategoryAsync(client, organisationId, codeSuffix);
        return (category, taxCategory);
    }

    private static async Task<TaxCategoryResponse> CreateTaxCategoryAsync(HttpClient client, Guid organisationId, string codeSuffix)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/tax-categories",
            new CreateTaxCategoryRequest($"TAXCAT_{codeSuffix}", "Taxable", organisationId, TaxTreatment.Taxable));
        return (await response.Content.ReadFromJsonAsync<TaxCategoryResponse>())!;
    }

    private static Task<HttpResponseMessage> CreateProductAsync(HttpClient client, Guid organisationId, Guid productCategoryId, Guid taxCategoryId, string name) =>
        client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductRequest(name, organisationId, productCategoryId, taxCategoryId, null, null, null, 5.00m));

    private async Task<ProductResponse> CreateAndParseProductAsync(HttpClient client, Guid organisationId, string codeSuffix)
    {
        var (category, taxCategory) = await CreatePrerequisitesAsync(client, organisationId, codeSuffix);
        var response = await CreateProductAsync(client, organisationId, category.Id, taxCategory.Id, $"Product {codeSuffix}");
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
