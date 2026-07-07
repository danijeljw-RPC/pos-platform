using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Catalog;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Endpoints.Menus;
using DaxaPos.Api.Endpoints.Tax;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Domain.Enums;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Behaviour tests for <see cref="ResolvedMenuEndpoints"/> (PLAN-0004 Milestone G) — the plan's
/// other deliberately staff-accessible endpoint, but via <c>.RequireAuthorization()</c> only, no
/// permission code at all (approved Human Decision #1). <see cref="StaffSession_CanReadResolvedMenu"/>
/// is the load-bearing proof of that design; every other test proves the merge/availability/pricing
/// rules against an <c>OrganisationOwner</c> session for simplicity.
/// </summary>
public class ResolvedMenuEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public ResolvedMenuEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
            builder.UseSetting("DeviceRegistration:RateLimitPermitLimit", "1000");
        });
    }

    [Fact]
    public async Task StaffSession_CanReadResolvedMenu()
    {
        var scenario = await SetupScenarioAsync("Staff Access Venue");
        await CreateVenueTaxConfigurationAsync(scenario.AdminClient, scenario.Location.Id);
        var menu = await CreateMenuAsync(scenario.AdminClient, scenario.OrganisationId, "Staff Access Menu", null);
        var section = await CreateSectionAsync(scenario.AdminClient, menu.Id, "Section");
        await AssignAsync(scenario.AdminClient, section.Id, scenario.Product.Id, 0);

        var response = await scenario.StaffClient.GetAsync($"/api/v1/menus/resolved?locationId={scenario.Location.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resolved = await response.Content.ReadFromJsonAsync<ResolvedMenuResponse>();
        Assert.Contains(resolved!.Sections, s => s.Items.Any(i => i.ProductId == scenario.Product.Id));
    }

    [Fact]
    public async Task ResolvedMenu_IncludesModifierGroupsAndOptions_ForALinkedProduct()
    {
        var scenario = await SetupScenarioAsync("Modifier Menu Venue");
        await CreateVenueTaxConfigurationAsync(scenario.AdminClient, scenario.Location.Id);
        var menu = await CreateMenuAsync(scenario.AdminClient, scenario.OrganisationId, "Modifier Menu", null);
        var section = await CreateSectionAsync(scenario.AdminClient, menu.Id, "Section");
        await AssignAsync(scenario.AdminClient, section.Id, scenario.Product.Id, 0);

        var groupResponse = await scenario.AdminClient.PostAsJsonAsync(
            "/api/v1/modifier-groups",
            new CreateModifierGroupRequest("Doneness", scenario.OrganisationId, 1, 1, true));
        var group = (await groupResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>())!;
        var modifierResponse = await scenario.AdminClient.PostAsJsonAsync(
            "/api/v1/modifiers", new CreateModifierRequest("Medium Rare", group.Id, 0m));
        var modifier = (await modifierResponse.Content.ReadFromJsonAsync<ModifierResponse>())!;
        await scenario.AdminClient.PostAsJsonAsync(
            "/api/v1/product-modifier-groups", new AssignProductModifierGroupRequest(scenario.Product.Id, group.Id, 0));

        var response = await scenario.StaffClient.GetAsync($"/api/v1/menus/resolved?locationId={scenario.Location.Id}");
        var resolved = await response.Content.ReadFromJsonAsync<ResolvedMenuResponse>();

        var item = resolved!.Sections.SelectMany(s => s.Items).Single(i => i.ProductId == scenario.Product.Id);
        var resolvedGroup = Assert.Single(item.ModifierGroups);
        Assert.Equal(group.Id, resolvedGroup.Id);
        Assert.True(resolvedGroup.IsRequired);
        Assert.Equal(1, resolvedGroup.SelectionMin);
        Assert.Equal(1, resolvedGroup.SelectionMax);
        var resolvedModifier = Assert.Single(resolvedGroup.Modifiers);
        Assert.Equal(modifier.Id, resolvedModifier.Id);
    }

    [Fact]
    public async Task ResolvedMenu_ReturnsEmptyModifierGroups_ForAProductWithNoneAssigned()
    {
        var scenario = await SetupScenarioAsync("No Modifier Menu Venue");
        await CreateVenueTaxConfigurationAsync(scenario.AdminClient, scenario.Location.Id);
        var menu = await CreateMenuAsync(scenario.AdminClient, scenario.OrganisationId, "No Modifier Menu", null);
        var section = await CreateSectionAsync(scenario.AdminClient, menu.Id, "Section");
        await AssignAsync(scenario.AdminClient, section.Id, scenario.Product.Id, 0);

        var response = await scenario.AdminClient.GetAsync($"/api/v1/menus/resolved?locationId={scenario.Location.Id}");
        var resolved = await response.Content.ReadFromJsonAsync<ResolvedMenuResponse>();

        var item = resolved!.Sections.SelectMany(s => s.Items).Single(i => i.ProductId == scenario.Product.Id);
        Assert.Empty(item.ModifierGroups);
    }

    [Fact]
    public async Task ResolvedMenu_Requires_Authentication()
    {
        var scenario = await SetupScenarioAsync("Unauthenticated Venue");
        var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.GetAsync($"/api/v1/menus/resolved?locationId={scenario.Location.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResolvedMenu_ExcludesProduct_WhenSoldOut()
    {
        var scenario = await SetupScenarioAsync("Sold Out Venue");
        await CreateVenueTaxConfigurationAsync(scenario.AdminClient, scenario.Location.Id);
        var menu = await CreateMenuAsync(scenario.AdminClient, scenario.OrganisationId, "Sold Out Menu", null);
        var section = await CreateSectionAsync(scenario.AdminClient, menu.Id, "Section");
        await AssignAsync(scenario.AdminClient, section.Id, scenario.Product.Id, 0);
        await scenario.AdminClient.PostAsJsonAsync(
            $"/api/v1/products/{scenario.Product.Id}/locations/{scenario.Location.Id}/sold-out",
            new SetSoldOutRequest(true));

        var response = await scenario.AdminClient.GetAsync($"/api/v1/menus/resolved?locationId={scenario.Location.Id}");
        var resolved = await response.Content.ReadFromJsonAsync<ResolvedMenuResponse>();

        Assert.DoesNotContain(resolved!.Sections.SelectMany(s => s.Items), i => i.ProductId == scenario.Product.Id);
    }

    [Fact]
    public async Task ResolvedMenu_ExcludesProduct_WhenLocationOverrideMarksUnavailable()
    {
        var scenario = await SetupScenarioAsync("Unavailable Venue");
        await CreateVenueTaxConfigurationAsync(scenario.AdminClient, scenario.Location.Id);
        var menu = await CreateMenuAsync(scenario.AdminClient, scenario.OrganisationId, "Unavailable Menu", null);
        var section = await CreateSectionAsync(scenario.AdminClient, menu.Id, "Section");
        await AssignAsync(scenario.AdminClient, section.Id, scenario.Product.Id, 0);
        await scenario.AdminClient.PostAsJsonAsync(
            "/api/v1/product-location-overrides",
            new CreateProductLocationOverrideRequest(scenario.Product.Id, scenario.Location.Id, false, false, null));

        var response = await scenario.AdminClient.GetAsync($"/api/v1/menus/resolved?locationId={scenario.Location.Id}");
        var resolved = await response.Content.ReadFromJsonAsync<ResolvedMenuResponse>();

        Assert.DoesNotContain(resolved!.Sections.SelectMany(s => s.Items), i => i.ProductId == scenario.Product.Id);
    }

    [Fact]
    public async Task ResolvedMenu_MergesOrgWideAndLocationMenus_LocationWinsOnConflict()
    {
        var scenario = await SetupScenarioAsync("Merge Venue");
        await CreateVenueTaxConfigurationAsync(scenario.AdminClient, scenario.Location.Id);

        var orgWideMenu = await CreateMenuAsync(scenario.AdminClient, scenario.OrganisationId, "Org Wide Menu", null);
        var orgWideSection = await CreateSectionAsync(scenario.AdminClient, orgWideMenu.Id, "Org Section");
        await AssignAsync(scenario.AdminClient, orgWideSection.Id, scenario.Product.Id, 0);

        var locationMenu = await CreateMenuAsync(scenario.AdminClient, scenario.OrganisationId, "Location Menu", scenario.Location.Id);
        var locationSection = await CreateSectionAsync(scenario.AdminClient, locationMenu.Id, "Location Section");
        await AssignAsync(scenario.AdminClient, locationSection.Id, scenario.Product.Id, 9);

        var response = await scenario.AdminClient.GetAsync($"/api/v1/menus/resolved?locationId={scenario.Location.Id}");
        var resolved = await response.Content.ReadFromJsonAsync<ResolvedMenuResponse>();

        var matches = resolved!.Sections.SelectMany(s => s.Items).Where(i => i.ProductId == scenario.Product.Id).ToList();
        Assert.Single(matches);
        var section = resolved.Sections.Single(s => s.Items.Any(i => i.ProductId == scenario.Product.Id));
        Assert.Equal(locationSection.Id, section.MenuSectionId);
        Assert.Equal(9, matches[0].DisplayOrder);
    }

    [Fact]
    public async Task ResolvedMenu_HidesMenu_OutsideAvailabilityWindow()
    {
        var scenario = await SetupScenarioAsync("Availability Venue");
        await CreateVenueTaxConfigurationAsync(scenario.AdminClient, scenario.Location.Id);
        var menu = await CreateMenuAsync(scenario.AdminClient, scenario.OrganisationId, "Availability Menu", null);
        var section = await CreateSectionAsync(scenario.AdminClient, menu.Id, "Section");
        await AssignAsync(scenario.AdminClient, section.Id, scenario.Product.Id, 0);

        // A window guaranteed not to contain "now" in UTC (the location's default TimeZoneId):
        // every day-of-week, but 1 minute long, at a fixed local time from 25 hours ago is not
        // reliable across midnight — instead pick a window covering every day except today.
        var today = ToDaysOfWeekMask(DateTime.UtcNow.DayOfWeek);
        var everyOtherDay = DaysOfWeekMask.All & ~today;
        await scenario.AdminClient.PostAsJsonAsync(
            "/api/v1/menu-availability-rules",
            new CreateMenuAvailabilityRuleRequest(menu.Id, everyOtherDay, new TimeOnly(0, 0), new TimeOnly(23, 59)));

        var response = await scenario.AdminClient.GetAsync($"/api/v1/menus/resolved?locationId={scenario.Location.Id}");
        var resolved = await response.Content.ReadFromJsonAsync<ResolvedMenuResponse>();

        Assert.DoesNotContain(resolved!.Sections.SelectMany(s => s.Items), i => i.ProductId == scenario.Product.Id);
    }

    [Fact]
    public async Task ResolvedMenu_ShowsMenu_InsideAvailabilityWindow()
    {
        var scenario = await SetupScenarioAsync("Availability Inside Venue");
        await CreateVenueTaxConfigurationAsync(scenario.AdminClient, scenario.Location.Id);
        var menu = await CreateMenuAsync(scenario.AdminClient, scenario.OrganisationId, "Availability Inside Menu", null);
        var section = await CreateSectionAsync(scenario.AdminClient, menu.Id, "Section");
        await AssignAsync(scenario.AdminClient, section.Id, scenario.Product.Id, 0);

        await scenario.AdminClient.PostAsJsonAsync(
            "/api/v1/menu-availability-rules",
            new CreateMenuAvailabilityRuleRequest(menu.Id, DaysOfWeekMask.All, new TimeOnly(0, 0), new TimeOnly(23, 59)));

        var response = await scenario.AdminClient.GetAsync($"/api/v1/menus/resolved?locationId={scenario.Location.Id}");
        var resolved = await response.Content.ReadFromJsonAsync<ResolvedMenuResponse>();

        Assert.Contains(resolved!.Sections.SelectMany(s => s.Items), i => i.ProductId == scenario.Product.Id);
    }

    [Fact]
    public async Task ResolvedMenu_UsesPriceResolverBasePrice_WhenNoOverride()
    {
        var scenario = await SetupScenarioAsync("Base Price Venue");
        await CreateVenueTaxConfigurationAsync(scenario.AdminClient, scenario.Location.Id);
        var menu = await CreateMenuAsync(scenario.AdminClient, scenario.OrganisationId, "Base Price Menu", null);
        var section = await CreateSectionAsync(scenario.AdminClient, menu.Id, "Section");
        await AssignAsync(scenario.AdminClient, section.Id, scenario.Product.Id, 0);

        var response = await scenario.AdminClient.GetAsync($"/api/v1/menus/resolved?locationId={scenario.Location.Id}");
        var resolved = await response.Content.ReadFromJsonAsync<ResolvedMenuResponse>();

        var item = resolved!.Sections.SelectMany(s => s.Items).Single(i => i.ProductId == scenario.Product.Id);
        Assert.Equal(5.00m, item.Price);
        Assert.True(item.IsTaxInclusive);
    }

    [Fact]
    public async Task ResolvedMenu_UsesLocationOverridePrice_WhenSet()
    {
        var scenario = await SetupScenarioAsync("Override Price Venue");
        await CreateVenueTaxConfigurationAsync(scenario.AdminClient, scenario.Location.Id);
        var menu = await CreateMenuAsync(scenario.AdminClient, scenario.OrganisationId, "Override Price Menu", null);
        var section = await CreateSectionAsync(scenario.AdminClient, menu.Id, "Section");
        await AssignAsync(scenario.AdminClient, section.Id, scenario.Product.Id, 0);
        await scenario.AdminClient.PostAsJsonAsync(
            "/api/v1/product-location-overrides",
            new CreateProductLocationOverrideRequest(scenario.Product.Id, scenario.Location.Id, true, false, 3.30m));

        var response = await scenario.AdminClient.GetAsync($"/api/v1/menus/resolved?locationId={scenario.Location.Id}");
        var resolved = await response.Content.ReadFromJsonAsync<ResolvedMenuResponse>();

        var item = resolved!.Sections.SelectMany(s => s.Items).Single(i => i.ProductId == scenario.Product.Id);
        Assert.Equal(3.30m, item.Price);
    }

    [Fact]
    public async Task ResolvedMenu_FailsClosed_WhenVenueTaxConfigurationIsMissing()
    {
        var scenario = await SetupScenarioAsync("Missing Tax Config Venue");
        // Deliberately no VenueTaxConfiguration created for this location.

        var response = await scenario.AdminClient.GetAsync($"/api/v1/menus/resolved?locationId={scenario.Location.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ResolvedMenu_Blocked_ForDifferentOrganisation()
    {
        var scenarioA = await SetupScenarioAsync("Org A Resolved Venue");
        var adminB = _factory.CreateClient();
        var callerB = await RbacTestSeeder.SeedAsync(adminB, "OrganisationOwner", scenarioA.TenantId);
        adminB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", callerB.Token);

        var response = await adminB.GetAsync($"/api/v1/menus/resolved?locationId={scenarioA.Location.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ResolvedMenu_Blocked_ForStaffSessionAtADifferentLocation_SameOrganisation()
    {
        var scenario = await SetupScenarioAsync("Location A Resolved Venue");
        var locationB = await DeviceTestHelper.CreateLocationAsync(scenario.AdminClient, scenario.OrganisationId, "Location B Resolved Venue");

        var response = await scenario.StaffClient.GetAsync($"/api/v1/menus/resolved?locationId={locationB.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record Scenario(
        HttpClient AdminClient,
        Guid TenantId,
        Guid OrganisationId,
        LocationResponse Location,
        HttpClient StaffClient,
        ProductResponse Product);

    private async Task<Scenario> SetupScenarioAsync(string venueName)
    {
        var adminClient = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(adminClient, "OrganisationOwner");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

        var location = await DeviceTestHelper.CreateLocationAsync(adminClient, caller.OrganisationId, venueName);
        var pin = await DeviceTestHelper.CreatePinAsync(adminClient, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(adminClient, pin.Pin);

        var deviceClient = _factory.CreateClient();
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, device.DeviceToken);

        var staff = await StaffTestHelper.CreateStaffMemberAsync(adminClient, location.Id, "RM", "9753");
        var staffRoleId = await GetRoleIdAsync("Staff");
        await adminClient.PostAsJsonAsync($"/api/v1/staff-members/{staff.Id}/roles", new AssignStaffRoleRequest(staffRoleId));

        var login = await StaffTestHelper.StaffLoginAsync(deviceClient, location.Id, "RM", "9753");
        var staffClient = _factory.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.SessionToken);

        var categoryResponse = await adminClient.PostAsJsonAsync("/api/v1/product-categories", new CreateProductCategoryRequest($"Category {venueName}", 0, caller.OrganisationId));
        var category = (await categoryResponse.Content.ReadFromJsonAsync<ProductCategoryResponse>())!;
        var taxCategoryResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/tax-categories", new CreateTaxCategoryRequest($"TAXCAT_{venueName}", "Taxable", caller.OrganisationId, TaxTreatment.Taxable));
        var taxCategory = (await taxCategoryResponse.Content.ReadFromJsonAsync<TaxCategoryResponse>())!;
        var productResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductRequest($"Product {venueName}", caller.OrganisationId, category.Id, taxCategory.Id, null, null, null, 5.00m));
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;

        return new Scenario(adminClient, caller.TenantId, caller.OrganisationId, location, staffClient, product);
    }

    private static async Task<Guid> GetRoleIdAsync(string roleName)
    {
        await using var dbContext = CreateDbContext();
        return (await dbContext.Roles.SingleAsync(r => r.Name == roleName)).Id;
    }

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));

    private async Task CreateVenueTaxConfigurationAsync(HttpClient adminClient, Guid locationId) =>
        await adminClient.PostAsJsonAsync(
            "/api/v1/venue-tax-configurations",
            new CreateVenueTaxConfigurationRequest(locationId, true, TaxCalculationScope.PerLine));

    private async Task<MenuResponse> CreateMenuAsync(HttpClient adminClient, Guid organisationId, string name, Guid? locationId)
    {
        var response = await adminClient.PostAsJsonAsync("/api/v1/menus", new CreateMenuRequest(name, organisationId, locationId));
        return (await response.Content.ReadFromJsonAsync<MenuResponse>())!;
    }

    private async Task<MenuSectionResponse> CreateSectionAsync(HttpClient adminClient, Guid menuId, string name)
    {
        var response = await adminClient.PostAsJsonAsync("/api/v1/menu-sections", new CreateMenuSectionRequest(name, menuId, 0));
        return (await response.Content.ReadFromJsonAsync<MenuSectionResponse>())!;
    }

    private static Task AssignAsync(HttpClient adminClient, Guid sectionId, Guid productId, int displayOrder) =>
        adminClient.PostAsJsonAsync("/api/v1/menu-section-items", new AssignMenuSectionItemRequest(sectionId, productId, displayOrder));

    private static DaysOfWeekMask ToDaysOfWeekMask(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => DaysOfWeekMask.Monday,
        DayOfWeek.Tuesday => DaysOfWeekMask.Tuesday,
        DayOfWeek.Wednesday => DaysOfWeekMask.Wednesday,
        DayOfWeek.Thursday => DaysOfWeekMask.Thursday,
        DayOfWeek.Friday => DaysOfWeekMask.Friday,
        DayOfWeek.Saturday => DaysOfWeekMask.Saturday,
        DayOfWeek.Sunday => DaysOfWeekMask.Sunday,
        _ => DaysOfWeekMask.None,
    };
}
