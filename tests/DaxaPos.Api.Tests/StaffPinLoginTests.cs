using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Identity;
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
/// Staff PIN login tests (PLAN-0003 Milestone F, ADR-0013): login requires a trusted DeviceToken
/// context first, scope comes only from the device's AuthContext, every failure is a generic 401
/// but audited with its specific reason (tenant known from the device — including unknown staff
/// codes, unlike the unknown-email/unknown-PIN precedents), lockout after five wrong PINs, staff
/// sessions get the shorter 8h/30min expiry, and a LocalStaffPin session is refused by every
/// rejectStaffPin endpoint. Runs with Keycloak stopped — the whole path is local Postgres only.
/// </summary>
public class StaffPinLoginTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public StaffPinLoginTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
            builder.UseSetting("DeviceRegistration:RateLimitPermitLimit", "1000");
        });
    }

    [Fact]
    public async Task Login_WithCorrectCodeAndPin_OnTrustedDevice_IssuesAStaffSession()
    {
        var scenario = await SetupAsync("Happy Path Venue");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");

        var response = await StaffTestHelper.StaffLoginRawAsync(scenario.DeviceClient, scenario.Location.Id, "DJ", "2468");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<StaffPinLoginResponse>();
        Assert.NotNull(login);
        Assert.Equal(staff.Id, login!.StaffMemberId);
        Assert.Equal("Test Staff", login.DisplayName);
        Assert.NotEmpty(login.SessionToken);

        // The session token works as a normal Bearer token and carries the staff/device context.
        var staffClient = _factory.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.SessionToken);
        var me = await staffClient.GetFromJsonAsync<AuthContextResponse>("/api/v1/auth/me");
        Assert.Equal(nameof(AuthMethod.LocalStaffPin), me!.AuthMethod);
        Assert.Equal(staff.Id, me.StaffMemberId);
        Assert.Equal(scenario.Device.DeviceId, me.DeviceId);
        Assert.Equal(scenario.Location.Id, me.LocationId);
        Assert.Null(me.UserId);
        Assert.Null(me.TerminalId);

        await using var dbContext = CreateDbContext();
        var session = await dbContext.AuthSessions.IgnoreQueryFilters()
            .SingleAsync(s => s.StaffMemberId == staff.Id && s.RevokedAtUtc == null);
        Assert.Equal(AuthMethod.LocalStaffPin, session.AuthMethod);
        Assert.NotEqual(login.SessionToken, session.SessionTokenHash);

        // Staff sessions use the shorter POS expiry policy (Decision 6): 8h absolute, not 12h.
        Assert.Equal(StaffSessionExpiryPolicy.AbsoluteLifetime, session.ExpiresAtUtc - session.IssuedAtUtc);

        var auditEventTypes = await dbContext.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == session.Id && a.EventType == "StaffPinLoginSucceeded")
            .ToListAsync();
        Assert.Single(auditEventTypes);
    }

    [Fact]
    public async Task Login_WithLowercaseStaffCode_Succeeds_ViaNormalisation()
    {
        var scenario = await SetupAsync("Lowercase Code Venue");
        await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "BAR01", "2468");

        var response = await StaffTestHelper.StaffLoginRawAsync(scenario.DeviceClient, scenario.Location.Id, "bar01", "2468");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPin_FailsGenerically_IncrementsFailedAttempts_AndAudits()
    {
        var scenario = await SetupAsync("Wrong Pin Venue");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");

        var response = await StaffTestHelper.StaffLoginRawAsync(scenario.DeviceClient, scenario.Location.Id, "DJ", "9999");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await using var dbContext = CreateDbContext();
        var row = await dbContext.StaffMembers.IgnoreQueryFilters().SingleAsync(s => s.Id == staff.Id);
        Assert.Equal(1, row.FailedPinAttempts);
        Assert.Null(row.LockedOutUntilUtc);

        await AssertLoginFailureAuditedAsync(staff.Id, "InvalidPin");
    }

    [Fact]
    public async Task Login_AfterFiveWrongPins_LocksOut_AndCorrectPinIsRejectedWhileLocked()
    {
        var scenario = await SetupAsync("Lockout Venue");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");

        for (var attempt = 0; attempt < LoginLockoutPolicy.MaxFailedAttempts; attempt++)
        {
            var wrong = await StaffTestHelper.StaffLoginRawAsync(scenario.DeviceClient, scenario.Location.Id, "DJ", "9999");
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }

        await using (var dbContext = CreateDbContext())
        {
            var row = await dbContext.StaffMembers.IgnoreQueryFilters().SingleAsync(s => s.Id == staff.Id);
            Assert.NotNull(row.LockedOutUntilUtc);
            Assert.True(row.LockedOutUntilUtc > DateTimeOffset.UtcNow);
        }

        var lockedOut = await StaffTestHelper.StaffLoginRawAsync(scenario.DeviceClient, scenario.Location.Id, "DJ", "2468");
        Assert.Equal(HttpStatusCode.Unauthorized, lockedOut.StatusCode);
        await AssertLoginFailureAuditedAsync(staff.Id, "LockedOut");
    }

    [Fact]
    public async Task Login_WithUnknownStaffCode_FailsGenerically_AndIsAuditedWithDeviceContext()
    {
        var scenario = await SetupAsync("Unknown Code Venue");

        var response = await StaffTestHelper.StaffLoginRawAsync(scenario.DeviceClient, scenario.Location.Id, "GHOST", "2468");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Unlike unknown emails/registration PINs, this IS audited: the trusted device context
        // supplies the tenant (approved Milestone F decision 12).
        await using var dbContext = CreateDbContext();
        var auditRow = await dbContext.AuditEvents.IgnoreQueryFilters()
            .SingleOrDefaultAsync(a => a.EventType == "StaffPinLoginFailed"
                && a.Reason == "UnknownStaffCode"
                && a.DeviceId == scenario.Device.DeviceId);

        Assert.NotNull(auditRow);
        Assert.Equal(scenario.Caller.TenantId, auditRow!.TenantId);
        Assert.Null(auditRow.StaffMemberId);
    }

    [Fact]
    public async Task Login_WhenStaffHomeLocationDiffers_IsRejected()
    {
        var scenario = await SetupAsync("Home Location Venue A");
        var locationB = await DeviceTestHelper.CreateLocationAsync(scenario.AdminClient, scenario.Caller.OrganisationId, "Home Location Venue B");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, locationB.Id, "DJ", "2468");

        // The device is registered at location A; the staff member's home is location B.
        var response = await StaffTestHelper.StaffLoginRawAsync(scenario.DeviceClient, scenario.Location.Id, "DJ", "2468");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertLoginFailureAuditedAsync(staff.Id, "HomeLocationMismatch");
    }

    [Fact]
    public async Task Login_WithBodyLocationMismatchingTheDevice_IsRejected()
    {
        var scenario = await SetupAsync("Body Location Venue A");
        var locationB = await DeviceTestHelper.CreateLocationAsync(scenario.AdminClient, scenario.Caller.OrganisationId, "Body Location Venue B");
        await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");

        // Correct staff, correct PIN — but the body claims a different location than the device's.
        var response = await StaffTestHelper.StaffLoginRawAsync(scenario.DeviceClient, locationB.Id, "DJ", "2468");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await using var dbContext = CreateDbContext();
        var audited = await dbContext.AuditEvents.IgnoreQueryFilters()
            .AnyAsync(a => a.EventType == "StaffPinLoginFailed"
                && a.Reason == "LocationMismatch"
                && a.DeviceId == scenario.Device.DeviceId);
        Assert.True(audited);
    }

    [Fact]
    public async Task Login_WithoutAnyAuthentication_Returns401()
    {
        var scenario = await SetupAsync("Anonymous Venue");
        await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");

        var anonymousClient = _factory.CreateClient();
        var response = await StaffTestHelper.StaffLoginRawAsync(anonymousClient, scenario.Location.Id, "DJ", "2468");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithABearerAdminSession_InsteadOfADeviceToken_Returns403()
    {
        var scenario = await SetupAsync("Bearer Session Venue");
        await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");

        // The admin client is authenticated — but with a Session token, not trusted device identity.
        var response = await StaffTestHelper.StaffLoginRawAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_Rejects_ClientSuppliedTenantId()
    {
        var scenario = await SetupAsync("TenantId Trap Venue");
        await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");

        var response = await scenario.DeviceClient.PostAsJsonAsync(
            "/api/v1/auth/staff-pin/login",
            new StaffPinLoginRequest(scenario.Location.Id, "DJ", "2468", TenantId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ForADisabledStaffMember_IsRejected()
    {
        var scenario = await SetupAsync("Inactive Staff Venue");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");
        Assert.Equal(HttpStatusCode.OK, (await scenario.AdminClient.PostAsync($"/api/v1/staff-members/{staff.Id}/disable", null)).StatusCode);

        var response = await StaffTestHelper.StaffLoginRawAsync(scenario.DeviceClient, scenario.Location.Id, "DJ", "2468");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertLoginFailureAuditedAsync(staff.Id, "StaffInactive");
    }

    [Fact]
    public async Task Disable_ImmediatelyRevokesAnActiveStaffSession()
    {
        var scenario = await SetupAsync("Emergency Disable Venue");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");
        var login = await StaffTestHelper.StaffLoginAsync(scenario.DeviceClient, scenario.Location.Id, "DJ", "2468");

        var staffClient = _factory.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.SessionToken);
        Assert.Equal(HttpStatusCode.OK, (await staffClient.GetAsync("/api/v1/auth/me")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await scenario.AdminClient.PostAsync($"/api/v1/staff-members/{staff.Id}/disable", null)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await staffClient.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task ResetPin_RevokesTheActiveSession_AndOnlyTheNewPinWorks()
    {
        var scenario = await SetupAsync("Reset Pin Login Venue");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");
        var login = await StaffTestHelper.StaffLoginAsync(scenario.DeviceClient, scenario.Location.Id, "DJ", "2468");

        var staffClient = _factory.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.SessionToken);
        Assert.Equal(HttpStatusCode.OK, (await staffClient.GetAsync("/api/v1/auth/me")).StatusCode);

        var resetResponse = await scenario.AdminClient.PostAsync($"/api/v1/staff-members/{staff.Id}/reset-pin", null);
        var reset = await resetResponse.Content.ReadFromJsonAsync<ResetStaffPinResponse>();

        // Old session dead, old PIN dead, new PIN works.
        Assert.Equal(HttpStatusCode.Unauthorized, (await staffClient.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await StaffTestHelper.StaffLoginRawAsync(scenario.DeviceClient, scenario.Location.Id, "DJ", "2468")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await StaffTestHelper.StaffLoginRawAsync(scenario.DeviceClient, scenario.Location.Id, "DJ", reset!.Pin)).StatusCode);
    }

    [Fact]
    public async Task RoleAssignment_IsCapturedInTheSnapshot_AtTheNextLogin()
    {
        var scenario = await SetupAsync("Snapshot Venue");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");

        var firstLogin = await StaffTestHelper.StaffLoginAsync(scenario.DeviceClient, scenario.Location.Id, "DJ", "2468");
        Assert.Empty(firstLogin.Roles);

        var staffRoleId = await GetRoleIdAsync("Staff");
        var assign = await scenario.AdminClient.PostAsJsonAsync(
            $"/api/v1/staff-members/{staff.Id}/roles", new AssignStaffRoleRequest(staffRoleId));
        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);

        var secondLogin = await StaffTestHelper.StaffLoginAsync(scenario.DeviceClient, scenario.Location.Id, "DJ", "2468");
        Assert.Contains("Staff", secondLogin.Roles);

        // The first session's snapshot is unchanged — captured at issue time, never re-derived.
        await using var dbContext = CreateDbContext();
        var firstSessionHash = new RandomSessionTokenService().Hash(firstLogin.SessionToken);
        var firstSession = await dbContext.AuthSessions.IgnoreQueryFilters()
            .SingleAsync(s => s.SessionTokenHash == firstSessionHash);
        Assert.Empty(firstSession.RoleSnapshot);
    }

    [Fact]
    public async Task Login_WhenAssignedRoleGrantsSensitivePermissions_IsRejectedAndAudited()
    {
        var scenario = await SetupAsync("Sensitive Role Venue");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");

        // VenueManager holds staff.manage/devices.manage/etc. — admin-sensitive codes a staff PIN
        // session must never carry, even though the role assignment itself is legal schema-wise.
        var venueManagerRoleId = await GetRoleIdAsync("VenueManager");
        Assert.Equal(HttpStatusCode.OK, (await scenario.AdminClient.PostAsJsonAsync(
            $"/api/v1/staff-members/{staff.Id}/roles", new AssignStaffRoleRequest(venueManagerRoleId))).StatusCode);

        var response = await StaffTestHelper.StaffLoginRawAsync(scenario.DeviceClient, scenario.Location.Id, "DJ", "2468");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertLoginFailureAuditedAsync(staff.Id, "RoleGrantsSensitivePermissions");
    }

    [Fact]
    public async Task Login_WhenAssignedRoleGrantsOnlyOperationalPermissions_Succeeds()
    {
        var scenario = await SetupAsync("Operational Permission Venue");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");

        // Staff previously held zero permissions (RolePermissionConfiguration, PLAN-0003).
        // catalog.sold-out-toggle (Operational) is the first permission ever granted to it
        // (PLAN-0004 Milestone A) — proves Permission.Category, not a hard-coded list, now
        // decides staff-PIN eligibility (OI-0015).
        var staffRoleId = await GetRoleIdAsync("Staff");
        Assert.Equal(HttpStatusCode.OK, (await scenario.AdminClient.PostAsJsonAsync(
            $"/api/v1/staff-members/{staff.Id}/roles", new AssignStaffRoleRequest(staffRoleId))).StatusCode);

        var response = await StaffTestHelper.StaffLoginRawAsync(scenario.DeviceClient, scenario.Location.Id, "DJ", "2468");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<StaffPinLoginResponse>();
        Assert.Contains(Permissions.CatalogSoldOutToggle, login!.Permissions);
    }

    [Fact]
    public async Task PermissionCatalogue_ClassifiesPLAN0004MilestoneAPermissions_ByCategory()
    {
        await using var dbContext = CreateDbContext();
        var codes = new[]
        {
            Permissions.OrganisationsManage,
            Permissions.CatalogManage,
            Permissions.PricingManage,
            Permissions.MenusManage,
            Permissions.CatalogSoldOutToggle,
        };

        var categoriesByCode = await dbContext.Permissions
            .Where(p => codes.Contains(p.Code))
            .ToDictionaryAsync(p => p.Code, p => p.Category);

        Assert.Equal(PermissionCategory.AdminSensitive, categoriesByCode[Permissions.OrganisationsManage]);
        Assert.Equal(PermissionCategory.AdminSensitive, categoriesByCode[Permissions.CatalogManage]);
        Assert.Equal(PermissionCategory.AdminSensitive, categoriesByCode[Permissions.PricingManage]);
        Assert.Equal(PermissionCategory.AdminSensitive, categoriesByCode[Permissions.MenusManage]);
        Assert.Equal(PermissionCategory.Operational, categoriesByCode[Permissions.CatalogSoldOutToggle]);
    }

    [Fact]
    public async Task StaffSession_Receives403_FromEveryRejectStaffPinEndpoint()
    {
        var scenario = await SetupAsync("Reject Staff Pin Venue");
        await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");
        var login = await StaffTestHelper.StaffLoginAsync(scenario.DeviceClient, scenario.Location.Id, "DJ", "2468");

        var staffClient = _factory.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.SessionToken);

        // The session is valid (auth/me works — no permission gate there)…
        Assert.Equal(HttpStatusCode.OK, (await staffClient.GetAsync("/api/v1/auth/me")).StatusCode);

        // …but every sensitive endpoint built in Milestones C–F refuses it.
        await AssertAllSensitiveEndpointsForbiddenAsync(staffClient, scenario.Location.Id);
    }

    [Fact]
    public async Task StaffSession_MisconfiguredWithSensitivePermissions_IsStillRejectedByRejectStaffPinEndpoints()
    {
        // The login-time guard makes this state impossible through the API, so seed the session
        // row directly — proving the endpoint-level rejectStaffPin net holds independently.
        var scenario = await SetupAsync("Misconfigured Snapshot Venue");
        var staff = await StaffTestHelper.CreateStaffMemberAsync(scenario.AdminClient, scenario.Location.Id, "DJ", "2468");

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
                LocationId = scenario.Location.Id,
                DeviceId = scenario.Device.DeviceId,
                StaffMemberId = staff.Id,
                AuthMethod = AuthMethod.LocalStaffPin,
                RoleSnapshot = ["VenueManager"],
                PermissionSnapshot =
                [
                    Permissions.OrganisationsManage, Permissions.LocationsManage, Permissions.TerminalsManage,
                    Permissions.DevicesManage, Permissions.DevicesRegister, Permissions.StaffManage,
                    Permissions.UsersManage, Permissions.SessionsManage,
                ],
                SessionTokenHash = tokenService.Hash(rawToken),
                IssuedAtUtc = now,
                ExpiresAtUtc = now.Add(StaffSessionExpiryPolicy.AbsoluteLifetime),
                LastActivityAtUtc = now,
            });
            await dbContext.SaveChangesAsync();
        }

        var staffClient = _factory.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);

        await AssertAllSensitiveEndpointsForbiddenAsync(staffClient, scenario.Location.Id);
    }

    private static async Task AssertAllSensitiveEndpointsForbiddenAsync(HttpClient staffClient, Guid locationId)
    {
        var attempts = new[]
        {
            await staffClient.GetAsync("/api/v1/organisations"),
            await staffClient.PostAsJsonAsync("/api/v1/organisations", new CreateOrganisationRequest("Nope")),
            await staffClient.GetAsync("/api/v1/locations"),
            await staffClient.GetAsync("/api/v1/terminals"),
            await staffClient.GetAsync("/api/v1/devices"),
            await staffClient.PostAsJsonAsync("/api/v1/device-registration-pins", new CreateDeviceRegistrationPinRequest(locationId, null)),
            await staffClient.GetAsync("/api/v1/staff-members"),
            await staffClient.PostAsJsonAsync("/api/v1/staff-members", new CreateStaffMemberRequest("Nope", "XX99", "4321", locationId)),
        };

        Assert.All(attempts, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
    }

    private sealed record Scenario(
        HttpClient AdminClient,
        SeededCaller Caller,
        LocationResponse Location,
        RegisterDeviceResponse Device,
        HttpClient DeviceClient);

    /// <summary>
    /// Seeds the full trusted-device chain: OrganisationOwner admin → location → registration
    /// PIN → registered device → a client authenticating with the device token.
    /// </summary>
    private async Task<Scenario> SetupAsync(string venueName)
    {
        var adminClient = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(adminClient, "OrganisationOwner");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);
        var location = await DeviceTestHelper.CreateLocationAsync(adminClient, caller.OrganisationId, venueName);
        var pin = await DeviceTestHelper.CreatePinAsync(adminClient, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(adminClient, pin.Pin);

        var deviceClient = _factory.CreateClient();
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, device.DeviceToken);

        return new Scenario(adminClient, caller, location, device, deviceClient);
    }

    private static async Task AssertLoginFailureAuditedAsync(Guid staffMemberId, string expectedReason)
    {
        await using var dbContext = CreateDbContext();
        var reasons = await dbContext.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.StaffMemberId == staffMemberId && a.EventType == "StaffPinLoginFailed")
            .Select(a => a.Reason)
            .ToListAsync();

        Assert.Contains(expectedReason, reasons);
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
