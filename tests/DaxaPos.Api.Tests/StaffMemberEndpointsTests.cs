using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Infrastructure.Security;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Staff-member management endpoint tests (PLAN-0003 Milestone F): create/list/get with
/// tenant + organisation scoping, StaffCode/PIN format and uniqueness rules, server-generated
/// PIN reset (raw PIN returned once, lockout cleared, sessions revoked), role assignment,
/// emergency disable, permission gating, and audit rows. Staff PIN <i>login</i> behaviour lives
/// in <c>StaffPinLoginTests</c>.
/// </summary>
public class StaffMemberEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public StaffMemberEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
            builder.UseSetting("DeviceRegistration:RateLimitPermitLimit", "1000");
        });
    }

    [Fact]
    public async Task Create_ReturnsCreatedStaffMember_WithUppercaseNormalisedStaffCode()
    {
        var (client, caller, location) = await SeedCallerWithLocationAsync("Create Staff Venue");

        var response = await client.PostAsJsonAsync(
            "/api/v1/staff-members",
            new CreateStaffMemberRequest("Dani J", "bar01", "4321", location.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var staff = await response.Content.ReadFromJsonAsync<StaffMemberResponse>();
        Assert.NotNull(staff);
        Assert.Equal("BAR01", staff!.StaffCode);
        Assert.Equal("Dani J", staff.DisplayName);
        Assert.Equal(caller.TenantId, staff.TenantId);
        Assert.Equal(caller.OrganisationId, staff.OrganisationId);
        Assert.Equal(location.Id, staff.LocationId);
        Assert.True(staff.IsActive);
    }

    [Fact]
    public async Task Create_NeverStoresTheRawPin()
    {
        var (client, _, location) = await SeedCallerWithLocationAsync("Raw Pin Venue");

        var staff = await StaffTestHelper.CreateStaffMemberAsync(client, location.Id, "DJ", "987654");

        await using var dbContext = CreateDbContext();
        var row = await dbContext.StaffMembers.IgnoreQueryFilters().SingleAsync(s => s.Id == staff.Id);
        Assert.NotEqual("987654", row.PinHash);
        Assert.DoesNotContain("987654", row.PinHash);
        Assert.True(new Pbkdf2PinHasher().Verify("987654", row.PinHash));
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var (client, _, location) = await SeedCallerWithLocationAsync("TenantId Trap Venue");

        var response = await client.PostAsJsonAsync(
            "/api/v1/staff-members",
            new CreateStaffMemberRequest("Staff", "MGR1", "4321", location.Id, TenantId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("A")] // too short
    [InlineData("MGR 1")] // space
    [InlineData("DJ!")] // symbol
    public async Task Create_Rejects_InvalidStaffCodeFormat(string staffCode)
    {
        var (client, _, location) = await SeedCallerWithLocationAsync($"Bad Code Venue {Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync(
            "/api/v1/staff-members",
            new CreateStaffMemberRequest("Staff", staffCode, "4321", location.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("123")] // too short
    [InlineData("12345678901")] // too long
    [InlineData("12a4")] // non-digit
    public async Task Create_Rejects_InvalidPinFormat(string pin)
    {
        var (client, _, location) = await SeedCallerWithLocationAsync($"Bad Pin Venue {Guid.NewGuid()}");

        var response = await client.PostAsJsonAsync(
            "/api/v1/staff-members",
            new CreateStaffMemberRequest("Staff", "MGR1", pin, location.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Rejects_DuplicateStaffCode_InSameOrganisation_CaseInsensitively()
    {
        var (client, _, location) = await SeedCallerWithLocationAsync("Duplicate Code Venue");
        await StaffTestHelper.CreateStaffMemberAsync(client, location.Id, "DJ01");

        var response = await client.PostAsJsonAsync(
            "/api/v1/staff-members",
            new CreateStaffMemberRequest("Second Staff", "dj01", "4321", location.Id));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_Allows_SameStaffCode_InADifferentOrganisation()
    {
        var (clientA, _, locationA) = await SeedCallerWithLocationAsync("Shared Code Venue A");
        var (clientB, _, locationB) = await SeedCallerWithLocationAsync("Shared Code Venue B");

        await StaffTestHelper.CreateStaffMemberAsync(clientA, locationA.Id, "MGR1");
        var staffB = await StaffTestHelper.CreateStaffMemberAsync(clientB, locationB.Id, "MGR1");

        Assert.Equal("MGR1", staffB.StaffCode);
    }

    [Fact]
    public async Task Create_ForLocationInAnotherOrganisation_ReturnsNotFound()
    {
        var (clientA, callerA, _) = await SeedCallerWithLocationAsync("Cross Org Venue A");

        // Second organisation under the same tenant — exercises the AuthContext.OrganisationId
        // cross-check independently of the tenant filter.
        var clientB = _factory.CreateClient();
        var callerB = await RbacTestSeeder.SeedAsync(clientB, "OrganisationOwner", existingTenantId: callerA.TenantId);
        AuthenticateAs(clientB, callerB);
        var locationB = await DeviceTestHelper.CreateLocationAsync(clientB, callerB.OrganisationId, "Cross Org Venue B");

        var response = await clientA.PostAsJsonAsync(
            "/api/v1/staff-members",
            new CreateStaffMemberRequest("Staff", "MGR1", "4321", locationB.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsStaffMember()
    {
        var (client, _, location) = await SeedCallerWithLocationAsync("Get Staff Venue");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(client, location.Id, "DJ02");

        var response = await client.GetAsync($"/api/v1/staff-members/{staff.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<StaffMemberResponse>();
        Assert.Equal(staff.Id, fetched!.Id);
    }

    [Fact]
    public async Task Get_ForAnotherTenantsStaffMember_ReturnsNotFound()
    {
        var (clientA, _, locationA) = await SeedCallerWithLocationAsync("Tenant Isolation Venue A");
        var staffA = await StaffTestHelper.CreateStaffMemberAsync(clientA, locationA.Id, "DJ03");

        var (clientB, _, _) = await SeedCallerWithLocationAsync("Tenant Isolation Venue B");

        var response = await clientB.GetAsync($"/api/v1/staff-members/{staffA.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ForAnotherOrganisationInTheSameTenant_ReturnsNotFound()
    {
        var (clientA, callerA, locationA) = await SeedCallerWithLocationAsync("Same Tenant Org A");
        var staffA = await StaffTestHelper.CreateStaffMemberAsync(clientA, locationA.Id, "DJ04");

        var clientB = _factory.CreateClient();
        var callerB = await RbacTestSeeder.SeedAsync(clientB, "OrganisationOwner", existingTenantId: callerA.TenantId);
        AuthenticateAs(clientB, callerB);

        var response = await clientB.GetAsync($"/api/v1/staff-members/{staffA.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOrganisationStaff_AndFiltersByLocation()
    {
        var (client, caller, locationA) = await SeedCallerWithLocationAsync("List Staff Venue A");
        var locationB = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "List Staff Venue B");
        var staffA = await StaffTestHelper.CreateStaffMemberAsync(client, locationA.Id, "AAA1");
        var staffB = await StaffTestHelper.CreateStaffMemberAsync(client, locationB.Id, "BBB1");

        var all = await client.GetFromJsonAsync<List<StaffMemberResponse>>("/api/v1/staff-members");
        Assert.Contains(all!, s => s.Id == staffA.Id);
        Assert.Contains(all!, s => s.Id == staffB.Id);

        var filtered = await client.GetFromJsonAsync<List<StaffMemberResponse>>($"/api/v1/staff-members?locationId={locationB.Id}");
        Assert.Single(filtered!);
        Assert.Equal(staffB.Id, filtered![0].Id);
    }

    [Fact]
    public async Task List_HidesDisabledStaff_ButGetStillFindsThem()
    {
        var (client, _, location) = await SeedCallerWithLocationAsync("Disabled List Venue");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(client, location.Id, "GONE1");

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/staff-members/{staff.Id}/disable", null)).StatusCode);

        var list = await client.GetFromJsonAsync<List<StaffMemberResponse>>("/api/v1/staff-members");
        Assert.DoesNotContain(list!, s => s.Id == staff.Id);

        var fetched = await client.GetFromJsonAsync<StaffMemberResponse>($"/api/v1/staff-members/{staff.Id}");
        Assert.False(fetched!.IsActive);
    }

    [Fact]
    public async Task Endpoints_Return403_WithoutStaffManagePermission()
    {
        // SupportAccess holds devices.manage/sessions.manage but not staff.manage.
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "SupportAccess");
        AuthenticateAs(client, caller);

        var create = await client.PostAsJsonAsync(
            "/api/v1/staff-members",
            new CreateStaffMemberRequest("Staff", "MGR1", "4321", Guid.NewGuid()));
        var list = await client.GetAsync("/api/v1/staff-members");
        var get = await client.GetAsync($"/api/v1/staff-members/{Guid.NewGuid()}");
        var reset = await client.PostAsync($"/api/v1/staff-members/{Guid.NewGuid()}/reset-pin", null);
        var disable = await client.PostAsync($"/api/v1/staff-members/{Guid.NewGuid()}/disable", null);

        Assert.All(
            new[] { create.StatusCode, list.StatusCode, get.StatusCode, reset.StatusCode, disable.StatusCode },
            status => Assert.Equal(HttpStatusCode.Forbidden, status));
    }

    [Fact]
    public async Task ResetPin_ReturnsGeneratedPin_StoresOnlyTheHash_AndClearsLockout()
    {
        var (client, _, location) = await SeedCallerWithLocationAsync("Reset Pin Venue");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(client, location.Id, "RST1", "1111");

        // Simulate a locked-out staff member directly — the state reset-pin must clear.
        await using (var dbContext = CreateDbContext())
        {
            var row = await dbContext.StaffMembers.IgnoreQueryFilters().SingleAsync(s => s.Id == staff.Id);
            row.FailedPinAttempts = 3;
            row.LockedOutUntilUtc = DateTimeOffset.UtcNow.AddMinutes(10);
            await dbContext.SaveChangesAsync();
        }

        var response = await client.PostAsync($"/api/v1/staff-members/{staff.Id}/reset-pin", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reset = await response.Content.ReadFromJsonAsync<ResetStaffPinResponse>();
        Assert.NotNull(reset);
        Assert.Equal(staff.Id, reset!.StaffMemberId);
        Assert.Equal(6, reset.Pin.Length);
        Assert.All(reset.Pin, c => Assert.InRange(c, '0', '9'));

        await using var verifyContext = CreateDbContext();
        var updated = await verifyContext.StaffMembers.IgnoreQueryFilters().SingleAsync(s => s.Id == staff.Id);
        Assert.NotEqual(reset.Pin, updated.PinHash);
        Assert.DoesNotContain(reset.Pin, updated.PinHash);
        Assert.True(new Pbkdf2PinHasher().Verify(reset.Pin, updated.PinHash));
        Assert.False(new Pbkdf2PinHasher().Verify("1111", updated.PinHash));
        Assert.Equal(0, updated.FailedPinAttempts);
        Assert.Null(updated.LockedOutUntilUtc);
    }

    [Fact]
    public async Task AssignRole_Succeeds_AndDuplicateAssignmentConflicts()
    {
        var (client, _, location) = await SeedCallerWithLocationAsync("Role Assign Venue");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(client, location.Id, "ROLE1");
        var staffRoleId = await GetRoleIdAsync("Staff");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/staff-members/{staff.Id}/roles", new AssignStaffRoleRequest(staffRoleId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var assigned = await response.Content.ReadFromJsonAsync<StaffRoleAssignedResponse>();
        Assert.Equal("Staff", assigned!.RoleName);

        var duplicate = await client.PostAsJsonAsync(
            $"/api/v1/staff-members/{staff.Id}/roles", new AssignStaffRoleRequest(staffRoleId));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var unknownRole = await client.PostAsJsonAsync(
            $"/api/v1/staff-members/{staff.Id}/roles", new AssignStaffRoleRequest(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, unknownRole.StatusCode);
    }

    [Fact]
    public async Task AuditRows_AreWrittenFor_Create_PinReset_RoleAssigned_Disable()
    {
        var (client, caller, location) = await SeedCallerWithLocationAsync("Audit Venue");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(client, location.Id, "AUD1");
        var staffRoleId = await GetRoleIdAsync("Staff");

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/staff-members/{staff.Id}/reset-pin", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/staff-members/{staff.Id}/roles", new AssignStaffRoleRequest(staffRoleId))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/staff-members/{staff.Id}/disable", null)).StatusCode);

        await using var dbContext = CreateDbContext();
        var eventTypes = await dbContext.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == staff.Id && a.EntityType == "StaffMember")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("StaffMemberCreated", eventTypes);
        Assert.Contains("StaffMemberPinReset", eventTypes);
        Assert.Contains("StaffMemberRoleAssigned", eventTypes);
        Assert.Contains("StaffMemberDisabled", eventTypes);

        var created = await dbContext.AuditEvents.IgnoreQueryFilters()
            .SingleAsync(a => a.EntityId == staff.Id && a.EventType == "StaffMemberCreated");
        Assert.Equal(caller.TenantId, created.TenantId);
        Assert.Equal(caller.UserId, created.UserId);
    }

    private async Task<(HttpClient Client, SeededCaller Caller, LocationResponse Location)> SeedCallerWithLocationAsync(string venueName)
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, venueName);
        return (client, caller, location);
    }

    private static async Task<Guid> GetRoleIdAsync(string roleName)
    {
        await using var dbContext = CreateDbContext();
        return (await dbContext.Roles.SingleAsync(r => r.Name == roleName)).Id;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
