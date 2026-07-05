using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Menus;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization and behaviour tests for <see cref="MenuSectionEndpoints"/> (PLAN-0004 Milestone
/// G) — gated <c>menus.manage</c> + <c>rejectStaffPin: true</c>. No <c>OrganisationId</c> column of
/// its own; every cross-organisation check walks <c>MenuSection -&gt; Menu -&gt; OrganisationId</c>.
/// </summary>
public class MenuSectionEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public MenuSectionEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
        });
    }

    [Fact]
    public async Task Create_Succeeds_AndAppearsInList()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var menu = await CreateMenuAsync(client, caller.OrganisationId, "SECTION_CREATE_TEST");

        var response = await client.PostAsJsonAsync("/api/v1/menu-sections", new CreateMenuSectionRequest("Hot Drinks", menu.Id, 0));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MenuSectionResponse>();
        Assert.NotNull(created);
        Assert.Equal("Hot Drinks", created!.Name);

        var list = await (await client.GetAsync($"/api/v1/menu-sections?menuId={menu.Id}")).Content.ReadFromJsonAsync<List<MenuSectionResponse>>();
        Assert.Contains(list!, s => s.Id == created.Id);
    }

    [Fact]
    public async Task Create_Rejects_MenuFromDifferentOrganisation()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var menuA = await CreateMenuAsync(client, callerA.OrganisationId, "ORG_A_MENU");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        var response = await client.PostAsJsonAsync("/api/v1/menu-sections", new CreateMenuSectionRequest("Nope", menuA.Id, 0));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsSection_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateSectionAsync(client, caller.OrganisationId, "GET_TEST");

        var response = await client.GetAsync($"/api/v1/menu-sections/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesFields_IncludingIsActive()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateSectionAsync(client, caller.OrganisationId, "UPDATE_TEST");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/menu-sections/{created.Id}",
            new UpdateMenuSectionRequest("Renamed Section", 5, false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<MenuSectionResponse>();
        Assert.Equal("Renamed Section", updated!.Name);
        Assert.Equal(5, updated.DisplayOrder);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var ownerClient = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(ownerClient, "OrganisationOwner");
        AuthenticateAs(ownerClient, owner);
        var menu = await CreateMenuAsync(ownerClient, owner.OrganisationId, "NO_PERMISSION_TEST");

        var staffClient = _factory.CreateClient();
        var staffCaller = await RbacTestSeeder.SeedAsync(staffClient, "Staff", owner.TenantId);
        AuthenticateAs(staffClient, staffCaller);

        var response = await staffClient.PostAsJsonAsync("/api/v1/menu-sections", new CreateMenuSectionRequest("Nope", menu.Id, 0));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReadAndUpdate_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var created = await CreateSectionAsync(client, callerA.OrganisationId, "CROSS_ORG_TEST");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/menu-sections/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync(
            $"/api/v1/menu-sections/{created.Id}", new UpdateMenuSectionRequest("Nope", 0, true))).StatusCode);
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var created = await CreateSectionAsync(client, caller.OrganisationId, "AUDIT_TEST");

        await client.PatchAsJsonAsync($"/api/v1/menu-sections/{created.Id}", new UpdateMenuSectionRequest("Renamed", 1, true));

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "MenuSection")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("MenuSectionCreated", eventTypes);
        Assert.Contains("MenuSectionUpdated", eventTypes);
    }

    private async Task<MenuResponse> CreateMenuAsync(HttpClient client, Guid organisationId, string codeSuffix)
    {
        var response = await client.PostAsJsonAsync("/api/v1/menus", new CreateMenuRequest($"Menu {codeSuffix}", organisationId, null));
        return (await response.Content.ReadFromJsonAsync<MenuResponse>())!;
    }

    private async Task<MenuSectionResponse> CreateSectionAsync(HttpClient client, Guid organisationId, string codeSuffix)
    {
        var menu = await CreateMenuAsync(client, organisationId, codeSuffix);
        var response = await client.PostAsJsonAsync("/api/v1/menu-sections", new CreateMenuSectionRequest($"Section {codeSuffix}", menu.Id, 0));
        return (await response.Content.ReadFromJsonAsync<MenuSectionResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
