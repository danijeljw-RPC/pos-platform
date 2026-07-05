using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Catalog;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Endpoints.Tax;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;
using DaxaPos.Infrastructure.Security;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization tests for <see cref="ProductSoldOutEndpoints"/> (PLAN-0004 Milestone F) — the
/// plan's first genuinely staff-accessible catalogue write. Confirmed against the plan's exact
/// permission table before implementation: <c>catalog.sold-out-toggle</c>, <c>rejectStaffPin: false</c>
/// (the deliberate opposite of every other Milestone F/C/D/E endpoint). The
/// <see cref="StaffSession_CanToggleSoldOut_ButStillRejectedFromPriceOverridePatch"/> pair is the
/// concrete proof the asymmetry works as designed, matching the plan's own required test shape.
/// </summary>
public class ProductSoldOutEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public ProductSoldOutEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
            builder.UseSetting("DeviceRegistration:RateLimitPermitLimit", "1000");
        });
    }

    [Fact]
    public async Task StaffSession_WithCatalogSoldOutToggle_CanSetSoldOut()
    {
        var scenario = await SetupStaffScenarioAsync("Sold Out Success Venue");

        var response = await scenario.StaffClient.PostAsJsonAsync(
            $"/api/v1/products/{scenario.Product.Id}/locations/{scenario.Location.Id}/sold-out",
            new SetSoldOutRequest(true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProductLocationOverrideResponse>();
        Assert.True(updated!.IsSoldOut);
    }

    [Fact]
    public async Task StaffSession_CanToggleSoldOut_ButStillRejectedFromPriceOverridePatch()
    {
        var scenario = await SetupStaffScenarioAsync("Asymmetry Proof Venue");

        var soldOutResponse = await scenario.StaffClient.PostAsJsonAsync(
            $"/api/v1/products/{scenario.Product.Id}/locations/{scenario.Location.Id}/sold-out",
            new SetSoldOutRequest(true));
        Assert.Equal(HttpStatusCode.OK, soldOutResponse.StatusCode);

        var overrideId = (await soldOutResponse.Content.ReadFromJsonAsync<ProductLocationOverrideResponse>())!.Id;
        var patchResponse = await scenario.StaffClient.PatchAsJsonAsync(
            $"/api/v1/product-location-overrides/{overrideId}",
            new UpdateProductLocationOverrideRequest(true, true, 99.00m));

        Assert.Equal(HttpStatusCode.Forbidden, patchResponse.StatusCode);
    }

    [Fact]
    public async Task SoldOutToggle_CreatesOverrideRow_WhenNoneExists_WithSafeDefaults()
    {
        var scenario = await SetupStaffScenarioAsync("Upsert Create Venue");

        var response = await scenario.StaffClient.PostAsJsonAsync(
            $"/api/v1/products/{scenario.Product.Id}/locations/{scenario.Location.Id}/sold-out",
            new SetSoldOutRequest(true));

        var created = await response.Content.ReadFromJsonAsync<ProductLocationOverrideResponse>();
        Assert.True(created!.IsAvailable);
        Assert.Null(created.PriceOverride);
    }

    [Fact]
    public async Task SoldOutToggle_UpdatesExistingOverride_WithoutTouchingPriceOverrideOrAvailability()
    {
        var scenario = await SetupStaffScenarioAsync("Upsert Update Venue");

        // pricing.manage creates a full override with a price first.
        var createResponse = await scenario.AdminClient.PostAsJsonAsync(
            "/api/v1/product-location-overrides",
            new CreateProductLocationOverrideRequest(scenario.Product.Id, scenario.Location.Id, true, false, 12.34m));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var response = await scenario.StaffClient.PostAsJsonAsync(
            $"/api/v1/products/{scenario.Product.Id}/locations/{scenario.Location.Id}/sold-out",
            new SetSoldOutRequest(true));

        var updated = await response.Content.ReadFromJsonAsync<ProductLocationOverrideResponse>();
        Assert.True(updated!.IsSoldOut);
        Assert.True(updated.IsAvailable);
        Assert.Equal(12.34m, updated.PriceOverride);
    }

    [Fact]
    public async Task SoldOutToggle_Rejects_SessionWithoutCatalogSoldOutToggle()
    {
        var scenario = await SetupStaffScenarioAsync("No Permission Venue");

        // Every currently-seeded role includes catalog.sold-out-toggle (Milestone A), so a session
        // genuinely lacking it must be seeded directly, matching StaffPinLoginTests'
        // misconfigured-session precedent for proving the endpoint-level guard independently.
        var tokenService = new RandomSessionTokenService();
        var rawToken = tokenService.GenerateToken();
        var now = DateTimeOffset.UtcNow;

        await using (var dbContext = CreateDbContext())
        {
            dbContext.AuthSessions.Add(new AuthSession
            {
                Id = Guid.NewGuid(),
                TenantId = scenario.Caller.TenantId,
                OrganisationId = scenario.Caller.OrganisationId,
                AuthMethod = AuthMethod.LocalUsernamePassword,
                RoleSnapshot = [],
                PermissionSnapshot = [],
                SessionTokenHash = tokenService.Hash(rawToken),
                IssuedAtUtc = now,
                ExpiresAtUtc = now.AddHours(1),
                LastActivityAtUtc = now,
            });
            await dbContext.SaveChangesAsync();
        }

        var noPermissionClient = _factory.CreateClient();
        noPermissionClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);

        var response = await noPermissionClient.PostAsJsonAsync(
            $"/api/v1/products/{scenario.Product.Id}/locations/{scenario.Location.Id}/sold-out",
            new SetSoldOutRequest(true));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SoldOutToggle_Blocked_ForDifferentOrganisation()
    {
        var scenarioA = await SetupStaffScenarioAsync("Org A Sold Out Venue");
        var ownerB = await RbacTestSeeder.SeedAsync(_factory.CreateClient(), "OrganisationOwner", scenarioA.Caller.TenantId);
        var adminB = _factory.CreateClient();
        adminB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerB.Token);
        var locationB = await DeviceTestHelper.CreateLocationAsync(adminB, ownerB.OrganisationId, "Org B Venue");

        // scenarioA's staff session tries to toggle a product it owns, but at Org B's location.
        var response = await scenarioA.StaffClient.PostAsJsonAsync(
            $"/api/v1/products/{scenarioA.Product.Id}/locations/{locationB.Id}/sold-out",
            new SetSoldOutRequest(true));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SoldOutToggle_Blocked_ForStaffSessionAtADifferentLocation_SameOrganisation()
    {
        var scenarioA = await SetupStaffScenarioAsync("Location A Venue");
        var locationB = await DeviceTestHelper.CreateLocationAsync(scenarioA.AdminClient, scenarioA.Caller.OrganisationId, "Location B Venue");

        // scenarioA's staff session is bound to Location A's device — it must not toggle
        // sold-out for the same organisation's Location B, even though the product is shared.
        var response = await scenarioA.StaffClient.PostAsJsonAsync(
            $"/api/v1/products/{scenarioA.Product.Id}/locations/{locationB.Id}/sold-out",
            new SetSoldOutRequest(true));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SoldOutToggle_WritesAuditEventRow()
    {
        var scenario = await SetupStaffScenarioAsync("Audit Test Venue");

        var response = await scenario.StaffClient.PostAsJsonAsync(
            $"/api/v1/products/{scenario.Product.Id}/locations/{scenario.Location.Id}/sold-out",
            new SetSoldOutRequest(true));
        var overrideId = (await response.Content.ReadFromJsonAsync<ProductLocationOverrideResponse>())!.Id;

        await using var context = CreateDbContext();
        var auditRow = await context.AuditEvents.IgnoreQueryFilters()
            .SingleOrDefaultAsync(a => a.EntityId == overrideId && a.EventType == "ProductLocationOverrideSoldOutToggled");

        Assert.NotNull(auditRow);
        Assert.Equal(scenario.StaffMemberId, auditRow!.StaffMemberId);
        Assert.Null(auditRow.UserId);
    }

    private sealed record StaffScenario(
        HttpClient AdminClient,
        SeededCaller Caller,
        LocationResponse Location,
        HttpClient StaffClient,
        Guid StaffMemberId,
        ProductResponse Product);

    private async Task<StaffScenario> SetupStaffScenarioAsync(string venueName)
    {
        var adminClient = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(adminClient, "OrganisationOwner");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

        var location = await DeviceTestHelper.CreateLocationAsync(adminClient, caller.OrganisationId, venueName);
        var pin = await DeviceTestHelper.CreatePinAsync(adminClient, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(adminClient, pin.Pin);

        var deviceClient = _factory.CreateClient();
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, device.DeviceToken);

        var staff = await StaffTestHelper.CreateStaffMemberAsync(adminClient, location.Id, "SO", "1357");
        var staffRoleId = await GetRoleIdAsync("Staff");
        await adminClient.PostAsJsonAsync($"/api/v1/staff-members/{staff.Id}/roles", new AssignStaffRoleRequest(staffRoleId));

        var login = await StaffTestHelper.StaffLoginAsync(deviceClient, location.Id, "SO", "1357");
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

        return new StaffScenario(adminClient, caller, location, staffClient, staff.Id, product);
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
}
