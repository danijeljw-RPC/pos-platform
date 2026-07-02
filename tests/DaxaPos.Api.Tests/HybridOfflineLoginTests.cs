using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DaxaPos.Api.Tests;

/// <summary>
/// PLAN-0003 Milestone G (plan step 39, ADR-0013/ADR-0015): the two Daxa WebAPI-native
/// authentication chains, end to end, against the local Postgres database only. Individual pieces
/// of these flows are covered by the per-milestone test files; this file deliberately re-runs each
/// chain whole, in one named place, as the proof that "local POS runtime authentication must not
/// require live cloud access" (ADR-0013) — nothing in either path calls Keycloak or any cloud
/// service. The verification environment enforces that claim rather than assuming it: locally the
/// suite runs with the <c>keycloak</c> compose service stopped, and CI (.github/workflows/ci.yml)
/// defines only a <c>postgres</c> service, so no Keycloak container even exists there.
/// </summary>
public class HybridOfflineLoginTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public HybridOfflineLoginTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
            builder.UseSetting("DeviceRegistration:RateLimitPermitLimit", "1000");
        });
    }

    [Fact]
    public async Task AdminUsernamePasswordChain_WorksEndToEnd_AgainstLocalDatabaseOnly()
    {
        // Login (inside SeedAsync) → /auth/me → permission-gated call → logout → old token dead.
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

        var me = await client.GetFromJsonAsync<AuthContextResponse>("/api/v1/auth/me");
        Assert.Equal(nameof(AuthMethod.LocalUsernamePassword), me!.AuthMethod);
        Assert.Equal(caller.UserId, me.UserId);
        Assert.Equal(caller.TenantId, me.TenantId);
        Assert.Contains("locations.manage", me.Permissions);

        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "Offline Admin Venue");
        Assert.NotEqual(Guid.Empty, location.Id);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task StaffPinChain_WorksEndToEnd_AgainstLocalDatabaseOnly()
    {
        // Registration PIN → device registration → device-token /auth/me → staff PIN login →
        // staff /auth/me → rejectStaffPin endpoint refuses the session → logout → token dead.
        var adminClient = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(adminClient, "OrganisationOwner");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);
        var location = await DeviceTestHelper.CreateLocationAsync(adminClient, caller.OrganisationId, "Offline Staff Venue");
        var pin = await DeviceTestHelper.CreatePinAsync(adminClient, location.Id);

        // Device registration is the pre-auth bootstrap step — an anonymous client, PIN-gated.
        var registration = await DeviceTestHelper.RegisterDeviceAsync(_factory.CreateClient(), pin.Pin);

        var deviceClient = _factory.CreateClient();
        DeviceTestHelper.AuthenticateWithDeviceToken(deviceClient, registration.DeviceToken);

        // Device identity alone: an authenticated context with zero user authority (ADR-0008).
        var deviceMe = await deviceClient.GetFromJsonAsync<AuthContextResponse>("/api/v1/auth/me");
        Assert.Equal(nameof(AuthMethod.DeviceToken), deviceMe!.AuthMethod);
        Assert.Empty(deviceMe.Roles);
        Assert.Empty(deviceMe.Permissions);
        Assert.Null(deviceMe.UserId);
        Assert.Null(deviceMe.StaffMemberId);

        var staff = await StaffTestHelper.CreateStaffMemberAsync(adminClient, location.Id, "OFF1", "2468");
        var login = await StaffTestHelper.StaffLoginAsync(deviceClient, location.Id, "OFF1", "2468");

        var staffClient = _factory.CreateClient();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.SessionToken);
        var staffMe = await staffClient.GetFromJsonAsync<AuthContextResponse>("/api/v1/auth/me");
        Assert.Equal(nameof(AuthMethod.LocalStaffPin), staffMe!.AuthMethod);
        Assert.Equal(staff.Id, staffMe.StaffMemberId);
        Assert.Equal(registration.DeviceId, staffMe.DeviceId);
        Assert.Equal(location.Id, staffMe.LocationId);
        Assert.Null(staffMe.UserId);

        // An operational session, never an admin one (ADR-0013 Staff ID/PIN Login Rules).
        Assert.Equal(HttpStatusCode.Forbidden, (await staffClient.GetAsync("/api/v1/staff-members")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await staffClient.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await staffClient.GetAsync("/api/v1/auth/me")).StatusCode);
    }
}
