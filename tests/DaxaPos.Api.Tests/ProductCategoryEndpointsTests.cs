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
/// Authorization and behaviour tests for <see cref="ProductCategoryEndpoints"/> (PLAN-0004
/// Milestone D). See <see cref="TaxDefinitionEndpointsTests"/>'s class remarks for why staff-PIN
/// rejection and the permission filter's unit-level behaviour aren't re-tested per entity here.
/// </summary>
public class ProductCategoryEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public ProductCategoryEndpointsTests(WebApplicationFactory<Program> factory)
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

        var response = await CreateAsync(client, caller.OrganisationId, "Beverages");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProductCategoryResponse>();
        Assert.NotNull(created);
        Assert.Equal(caller.TenantId, created!.TenantId);
        Assert.True(created.IsActive);

        var list = await (await client.GetAsync("/api/v1/product-categories")).Content.ReadFromJsonAsync<List<ProductCategoryResponse>>();
        Assert.Contains(list!, c => c.Id == created.Id);
    }

    [Fact]
    public async Task Create_AllowsDuplicateName_NoUniquenessConstraint()
    {
        // ProductCategory has no Code field — Name-only, matching the Location/Organisation
        // precedent of not enforcing name uniqueness.
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);

        var first = await CreateAsync(client, caller.OrganisationId, "Duplicate Name");
        var second = await CreateAsync(client, caller.OrganisationId, "Duplicate Name");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsProductCategory_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "Get Test");

        var response = await client.GetAsync($"/api/v1/product-categories/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesNameAndDisplayOrder_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "Update Test");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/product-categories/{created.Id}",
            new UpdateProductCategoryRequest("Renamed Category", 5));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProductCategoryResponse>();
        Assert.Equal("Renamed Category", updated!.Name);
        Assert.Equal(5, updated.DisplayOrder);
    }

    [Fact]
    public async Task Deactivate_Then_Reactivate_TogglesIsActive_AndListReflectsIt()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "Toggle Test");

        var deactivateResponse = await client.PostAsync($"/api/v1/product-categories/{created.Id}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.False((await deactivateResponse.Content.ReadFromJsonAsync<ProductCategoryResponse>())!.IsActive);

        var listAfterDeactivate = await (await client.GetAsync("/api/v1/product-categories")).Content.ReadFromJsonAsync<List<ProductCategoryResponse>>();
        Assert.DoesNotContain(listAfterDeactivate!, c => c.Id == created.Id);

        var reactivateResponse = await client.PostAsync($"/api/v1/product-categories/{created.Id}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
        Assert.True((await reactivateResponse.Content.ReadFromJsonAsync<ProductCategoryResponse>())!.IsActive);
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);

        var response = await client.PostAsJsonAsync(
            "/api/v1/product-categories",
            new CreateProductCategoryRequest("Should Fail", 0, caller.OrganisationId, Guid.NewGuid()));

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

        var response = await client.GetAsync($"/api/v1/product-categories/{created.Id}");
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
    public async Task ReadAndUpdateAndDeactivate_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var created = await CreateAndParseAsync(client, callerA.OrganisationId, "Cross Org RW Test");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/product-categories/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/product-categories/{created.Id}/deactivate", content: null)).StatusCode);

        var list = await (await client.GetAsync("/api/v1/product-categories")).Content.ReadFromJsonAsync<List<ProductCategoryResponse>>();
        Assert.DoesNotContain(list!, c => c.Id == created.Id);
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateAndParseAsync(client, caller.OrganisationId, "Audit Test");

        await client.PatchAsJsonAsync($"/api/v1/product-categories/{created.Id}", new UpdateProductCategoryRequest("Audit Renamed", 1));
        await client.PostAsync($"/api/v1/product-categories/{created.Id}/deactivate", content: null);
        await client.PostAsync($"/api/v1/product-categories/{created.Id}/reactivate", content: null);

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "ProductCategory")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("ProductCategoryCreated", eventTypes);
        Assert.Contains("ProductCategoryUpdated", eventTypes);
        Assert.Contains("ProductCategoryDeactivated", eventTypes);
        Assert.Contains("ProductCategoryReactivated", eventTypes);
    }

    private static Task<HttpResponseMessage> CreateAsync(HttpClient client, Guid organisationId, string name) =>
        client.PostAsJsonAsync("/api/v1/product-categories", new CreateProductCategoryRequest(name, 0, organisationId));

    private static async Task<ProductCategoryResponse> CreateAndParseAsync(HttpClient client, Guid organisationId, string name)
    {
        var response = await CreateAsync(client, organisationId, name);
        return (await response.Content.ReadFromJsonAsync<ProductCategoryResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
