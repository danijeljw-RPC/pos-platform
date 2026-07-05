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
/// Authorization and behaviour tests for <see cref="ProductModifierGroupEndpoints"/> (PLAN-0004
/// Milestone E) — attach/detach only, no list/read/update (see the endpoint's class remarks).
/// Attachment state and <c>DisplayOrder</c> are asserted directly against the database, matching
/// this file's audit-row-assertion convention, since no list endpoint exists for this join.
/// </summary>
public class ProductModifierGroupEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public ProductModifierGroupEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
        });
    }

    [Fact]
    public async Task Assign_Succeeds_AndPersistsDisplayOrder()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (product, modifierGroup) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "ASSIGN_TEST");

        var response = await client.PostAsJsonAsync(
            "/api/v1/product-modifier-groups",
            new AssignProductModifierGroupRequest(product.Id, modifierGroup.Id, 3));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProductModifierGroupResponse>();
        Assert.NotNull(created);
        Assert.Equal(product.Id, created!.ProductId);
        Assert.Equal(modifierGroup.Id, created.ModifierGroupId);
        Assert.Equal(3, created.DisplayOrder);

        await using var context = CreateDbContext();
        var persisted = await context.ProductModifierGroups.IgnoreQueryFilters().SingleAsync(l => l.Id == created.Id);
        Assert.Equal(3, persisted.DisplayOrder);
    }

    [Fact]
    public async Task Unassign_RemovesTheLink()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (product, modifierGroup) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "UNASSIGN_TEST");
        var created = await AssignAndParseAsync(client, product.Id, modifierGroup.Id, 0);

        var response = await client.DeleteAsync($"/api/v1/product-modifier-groups/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var context = CreateDbContext();
        var stillExists = await context.ProductModifierGroups.IgnoreQueryFilters().AnyAsync(l => l.Id == created.Id);
        Assert.False(stillExists);
    }

    [Fact]
    public async Task Assign_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (product, modifierGroup) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "TENANTID_TRAP");

        var response = await client.PostAsJsonAsync(
            "/api/v1/product-modifier-groups",
            new AssignProductModifierGroupRequest(product.Id, modifierGroup.Id, 0, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Assign_Fails_WithoutPermission()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(ownerClient, "OrganisationOwner");
        AuthenticateAs(ownerClient, owner);
        var (product, modifierGroup) = await CreatePrerequisitesAsync(ownerClient, owner.OrganisationId, "NO_PERMISSION_TEST");

        var staffClient = _factory.CreateClient();
        var staffCaller = await RbacTestSeeder.SeedAsync(staffClient, "Staff", owner.TenantId);
        AuthenticateAs(staffClient, staffCaller);

        var response = await staffClient.PostAsJsonAsync(
            "/api/v1/product-modifier-groups",
            new AssignProductModifierGroupRequest(product.Id, modifierGroup.Id, 0));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Assign_Rejects_ProductFromDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var (productA, _) = await CreatePrerequisitesAsync(client, callerA.OrganisationId, "ORG_A_PRODUCT");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);
        var (_, modifierGroupB) = await CreatePrerequisitesAsync(client, callerB.OrganisationId, "ORG_B_GROUP");

        AuthenticateAs(client, callerA);
        var response = await client.PostAsJsonAsync(
            "/api/v1/product-modifier-groups",
            new AssignProductModifierGroupRequest(productA.Id, modifierGroupB.Id, 0));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Assign_Rejects_ModifierGroupFromDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var (productA, modifierGroupA) = await CreatePrerequisitesAsync(client, callerA.OrganisationId, "ORG_A_BOTH");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        var response = await client.PostAsJsonAsync(
            "/api/v1/product-modifier-groups",
            new AssignProductModifierGroupRequest(productA.Id, modifierGroupA.Id, 0));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unassign_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var (productA, modifierGroupA) = await CreatePrerequisitesAsync(client, callerA.OrganisationId, "UNASSIGN_CROSS_ORG");
        var created = await AssignAndParseAsync(client, productA.Id, modifierGroupA.Id, 0);

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        var response = await client.DeleteAsync($"/api/v1/product-modifier-groups/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignAndUnassign_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (product, modifierGroup) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "AUDIT_TEST");
        var created = await AssignAndParseAsync(client, product.Id, modifierGroup.Id, 0);

        await client.DeleteAsync($"/api/v1/product-modifier-groups/{created.Id}");

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "ProductModifierGroup")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("ProductModifierGroupAssigned", eventTypes);
        Assert.Contains("ProductModifierGroupUnassigned", eventTypes);
    }

    private async Task<(ProductResponse Product, ModifierGroupResponse ModifierGroup)> CreatePrerequisitesAsync(HttpClient client, Guid organisationId, string codeSuffix)
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

        var modifierGroupResponse = await client.PostAsJsonAsync(
            "/api/v1/modifier-groups", new CreateModifierGroupRequest($"Group {codeSuffix}", organisationId, 0, 1, false));
        var modifierGroup = (await modifierGroupResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>())!;

        return (product, modifierGroup);
    }

    private static async Task<ProductModifierGroupResponse> AssignAndParseAsync(HttpClient client, Guid productId, Guid modifierGroupId, int displayOrder)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/product-modifier-groups", new AssignProductModifierGroupRequest(productId, modifierGroupId, displayOrder));
        return (await response.Content.ReadFromJsonAsync<ProductModifierGroupResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
