using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Menus;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Domain.Enums;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization and behaviour tests for <see cref="MenuAvailabilityRuleEndpoints"/> (PLAN-0004
/// Milestone G, approved Human Decision #7's day/time shape) — create/list/delete only, gated
/// <c>menus.manage</c> + <c>rejectStaffPin: true</c>.
/// </summary>
public class MenuAvailabilityRuleEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public MenuAvailabilityRuleEndpointsTests(WebApplicationFactory<Program> factory)
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
        var menu = await CreateMenuAsync(client, caller.OrganisationId, "RULE_CREATE_TEST");

        var response = await client.PostAsJsonAsync(
            "/api/v1/menu-availability-rules",
            new CreateMenuAvailabilityRuleRequest(menu.Id, DaysOfWeekMask.Monday | DaysOfWeekMask.Tuesday, new TimeOnly(9, 0), new TimeOnly(17, 0)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MenuAvailabilityRuleResponse>();
        Assert.NotNull(created);

        var list = await (await client.GetAsync($"/api/v1/menu-availability-rules?menuId={menu.Id}")).Content.ReadFromJsonAsync<List<MenuAvailabilityRuleResponse>>();
        Assert.Contains(list!, r => r.Id == created!.Id);
    }

    [Fact]
    public async Task Create_Rejects_NoneDaysOfWeekMask()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var menu = await CreateMenuAsync(client, caller.OrganisationId, "NONE_MASK_TEST");

        var response = await client.PostAsJsonAsync(
            "/api/v1/menu-availability-rules",
            new CreateMenuAvailabilityRuleRequest(menu.Id, DaysOfWeekMask.None, new TimeOnly(9, 0), new TimeOnly(17, 0)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Rejects_StartTimeNotBeforeEndTime()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var menu = await CreateMenuAsync(client, caller.OrganisationId, "BAD_WINDOW_TEST");

        var response = await client.PostAsJsonAsync(
            "/api/v1/menu-availability-rules",
            new CreateMenuAvailabilityRuleRequest(menu.Id, DaysOfWeekMask.Monday, new TimeOnly(17, 0), new TimeOnly(9, 0)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

        var response = await client.PostAsJsonAsync(
            "/api/v1/menu-availability-rules",
            new CreateMenuAvailabilityRuleRequest(menuA.Id, DaysOfWeekMask.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesRule()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var menu = await CreateMenuAsync(client, caller.OrganisationId, "DELETE_TEST");
        var created = await CreateRuleAndParseAsync(client, menu.Id);

        var response = await client.DeleteAsync($"/api/v1/menu-availability-rules/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var list = await (await client.GetAsync($"/api/v1/menu-availability-rules?menuId={menu.Id}")).Content.ReadFromJsonAsync<List<MenuAvailabilityRuleResponse>>();
        Assert.DoesNotContain(list!, r => r.Id == created.Id);
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

        var response = await staffClient.PostAsJsonAsync(
            "/api/v1/menu-availability-rules",
            new CreateMenuAvailabilityRuleRequest(menu.Id, DaysOfWeekMask.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndDelete_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var menu = await CreateMenuAsync(client, caller.OrganisationId, "AUDIT_TEST");
        var created = await CreateRuleAndParseAsync(client, menu.Id);
        await client.DeleteAsync($"/api/v1/menu-availability-rules/{created.Id}");

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "MenuAvailabilityRule")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("MenuAvailabilityRuleCreated", eventTypes);
        Assert.Contains("MenuAvailabilityRuleDeleted", eventTypes);
    }

    private async Task<MenuResponse> CreateMenuAsync(HttpClient client, Guid organisationId, string codeSuffix)
    {
        var response = await client.PostAsJsonAsync("/api/v1/menus", new CreateMenuRequest($"Menu {codeSuffix}", organisationId, null));
        return (await response.Content.ReadFromJsonAsync<MenuResponse>())!;
    }

    private async Task<MenuAvailabilityRuleResponse> CreateRuleAndParseAsync(HttpClient client, Guid menuId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/menu-availability-rules",
            new CreateMenuAvailabilityRuleRequest(menuId, DaysOfWeekMask.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)));
        return (await response.Content.ReadFromJsonAsync<MenuAvailabilityRuleResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
