using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Catalog;
using DaxaPos.Api.Endpoints.Menus;
using DaxaPos.Api.Endpoints.Tax;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Domain.Enums;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization and behaviour tests for <see cref="MenuSectionItemEndpoints"/> (PLAN-0004
/// Milestone G) — assign/unassign only, gated <c>menus.manage</c> + <c>rejectStaffPin: true</c>,
/// matching <see cref="ProductModifierGroupEndpoints"/>'s no-lifecycle-beyond-assign shape.
/// </summary>
public class MenuSectionItemEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public MenuSectionItemEndpointsTests(WebApplicationFactory<Program> factory)
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
        var (section, product) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "ASSIGN_TEST");

        var response = await client.PostAsJsonAsync(
            "/api/v1/menu-section-items",
            new AssignMenuSectionItemRequest(section.Id, product.Id, 3));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MenuSectionItemResponse>();
        Assert.NotNull(created);
        Assert.Equal(3, created!.DisplayOrder);

        await using var context = CreateDbContext();
        var persisted = await context.MenuSectionItems.IgnoreQueryFilters().SingleAsync(i => i.Id == created.Id);
        Assert.Equal(3, persisted.DisplayOrder);
    }

    [Fact]
    public async Task Assign_Rejects_ArchivedOrInactiveProduct()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (section, product) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "INACTIVE_TEST");
        await client.PostAsync($"/api/v1/products/{product.Id}/deactivate", null);

        var response = await client.PostAsJsonAsync(
            "/api/v1/menu-section-items",
            new AssignMenuSectionItemRequest(section.Id, product.Id, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Assign_Rejects_ProductFromDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var (sectionA, _) = await CreatePrerequisitesAsync(client, callerA.OrganisationId, "ORG_A_SECTION");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);
        var (_, productB) = await CreatePrerequisitesAsync(client, callerB.OrganisationId, "ORG_B_PRODUCT");

        AuthenticateAs(client, callerA);
        var response = await client.PostAsJsonAsync(
            "/api/v1/menu-section-items",
            new AssignMenuSectionItemRequest(sectionA.Id, productB.Id, 0));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Assign_Rejects_SectionFromDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var (_, productA) = await CreatePrerequisitesAsync(client, callerA.OrganisationId, "ORG_A_PRODUCT");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);
        var (sectionB, _) = await CreatePrerequisitesAsync(client, callerB.OrganisationId, "ORG_B_SECTION");

        AuthenticateAs(client, callerA);
        var response = await client.PostAsJsonAsync(
            "/api/v1/menu-section-items",
            new AssignMenuSectionItemRequest(sectionB.Id, productA.Id, 0));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unassign_RemovesItem()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (section, product) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "UNASSIGN_TEST");
        var created = await AssignAndParseAsync(client, section.Id, product.Id);

        var response = await client.DeleteAsync($"/api/v1/menu-section-items/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var context = CreateDbContext();
        Assert.Null(await context.MenuSectionItems.IgnoreQueryFilters().SingleOrDefaultAsync(i => i.Id == created.Id));
    }

    [Fact]
    public async Task Assign_Fails_WithoutPermission()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(ownerClient, "OrganisationOwner");
        AuthenticateAs(ownerClient, owner);
        var (section, product) = await CreatePrerequisitesAsync(ownerClient, owner.OrganisationId, "NO_PERMISSION_TEST");

        var staffClient = _factory.CreateClient();
        var staffCaller = await RbacTestSeeder.SeedAsync(staffClient, "Staff", owner.TenantId);
        AuthenticateAs(staffClient, staffCaller);

        var response = await staffClient.PostAsJsonAsync(
            "/api/v1/menu-section-items",
            new AssignMenuSectionItemRequest(section.Id, product.Id, 0));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssignAndUnassign_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var (section, product) = await CreatePrerequisitesAsync(client, caller.OrganisationId, "AUDIT_TEST");
        var created = await AssignAndParseAsync(client, section.Id, product.Id);
        await client.DeleteAsync($"/api/v1/menu-section-items/{created.Id}");

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "MenuSectionItem")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("MenuSectionItemAssigned", eventTypes);
        Assert.Contains("MenuSectionItemUnassigned", eventTypes);
    }

    private async Task<(MenuSectionResponse Section, ProductResponse Product)> CreatePrerequisitesAsync(HttpClient client, Guid organisationId, string codeSuffix)
    {
        var menuResponse = await client.PostAsJsonAsync("/api/v1/menus", new CreateMenuRequest($"Menu {codeSuffix}", organisationId, null));
        var menu = (await menuResponse.Content.ReadFromJsonAsync<MenuResponse>())!;

        var sectionResponse = await client.PostAsJsonAsync("/api/v1/menu-sections", new CreateMenuSectionRequest($"Section {codeSuffix}", menu.Id, 0));
        var section = (await sectionResponse.Content.ReadFromJsonAsync<MenuSectionResponse>())!;

        var categoryResponse = await client.PostAsJsonAsync("/api/v1/product-categories", new CreateProductCategoryRequest($"Category {codeSuffix}", 0, organisationId));
        var category = (await categoryResponse.Content.ReadFromJsonAsync<ProductCategoryResponse>())!;

        var taxCategoryResponse = await client.PostAsJsonAsync(
            "/api/v1/tax-categories", new CreateTaxCategoryRequest($"TAXCAT_{codeSuffix}", "Taxable", organisationId, TaxTreatment.Taxable));
        var taxCategory = (await taxCategoryResponse.Content.ReadFromJsonAsync<TaxCategoryResponse>())!;

        var productResponse = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductRequest($"Product {codeSuffix}", organisationId, category.Id, taxCategory.Id, null, null, null, 5.00m));
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;

        return (section, product);
    }

    private async Task<MenuSectionItemResponse> AssignAndParseAsync(HttpClient client, Guid sectionId, Guid productId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/menu-section-items", new AssignMenuSectionItemRequest(sectionId, productId, 0));
        return (await response.Content.ReadFromJsonAsync<MenuSectionItemResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
