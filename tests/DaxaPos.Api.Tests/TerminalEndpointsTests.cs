using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Domain.Entities;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Authorization tests for the Terminal create/read/rename/deactivate/reactivate endpoints
/// (PLAN-0003 Milestone D). Terminal has no <c>OrganisationId</c> column, so the
/// <c>AuthContext.OrganisationId</c> cross-check walks <c>Terminal -&gt; Location -&gt;
/// OrganisationId</c> — see the "different organisation, same tenant" tests below, which exercise
/// that walk specifically. Every request authenticates via a LocalUsernamePassword session against
/// endpoints configured with <c>rejectStaffPin: true</c>; see <c>OrganisationEndpointsTests.cs</c>'s
/// class remarks and <c>RequirePermissionFilterTests.cs</c> for why that mechanism isn't re-tested
/// per entity.
/// </summary>
public class TerminalEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public TerminalEndpointsTests(WebApplicationFactory<Program> factory)
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
        var location = await CreateLocationAsync(client, caller.OrganisationId, "Front of House");

        var response = await client.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest("Front Counter 1", location.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TerminalResponse>();
        Assert.NotNull(created);
        Assert.Equal(caller.TenantId, created!.TenantId);
        Assert.Equal(location.Id, created.LocationId);
        Assert.True(created.IsActive);

        var list = await (await client.GetAsync("/api/v1/terminals")).Content.ReadFromJsonAsync<List<TerminalResponse>>();
        Assert.Contains(list!, t => t.Id == created.Id);
    }

    [Fact]
    public async Task Get_ReturnsTerminal_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await CreateLocationAsync(client, caller.OrganisationId, "Bar Area");
        var created = await CreateTerminalAsync(client, location.Id, "Bar POS 1");

        var response = await client.GetAsync($"/api/v1/terminals/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_RenamesTerminal_WithinSameOrganisation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await CreateLocationAsync(client, caller.OrganisationId, "Restaurant Floor");
        var created = await CreateTerminalAsync(client, location.Id, "Restaurant POS");

        var response = await client.PatchAsJsonAsync($"/api/v1/terminals/{created.Id}", new UpdateTerminalRequest("Restaurant POS Renamed"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TerminalResponse>();
        Assert.Equal("Restaurant POS Renamed", updated!.Name);
    }

    [Fact]
    public async Task Deactivate_Then_Reactivate_TogglesIsActive_AndListReflectsIt()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await CreateLocationAsync(client, caller.OrganisationId, "Drive Through");
        var created = await CreateTerminalAsync(client, location.Id, "Drive Through POS");

        var deactivateResponse = await client.PostAsync($"/api/v1/terminals/{created.Id}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.False((await deactivateResponse.Content.ReadFromJsonAsync<TerminalResponse>())!.IsActive);

        var listAfterDeactivate = await (await client.GetAsync("/api/v1/terminals")).Content.ReadFromJsonAsync<List<TerminalResponse>>();
        Assert.DoesNotContain(listAfterDeactivate!, t => t.Id == created.Id);

        var getInactiveResponse = await client.GetAsync($"/api/v1/terminals/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getInactiveResponse.StatusCode);

        var reactivateResponse = await client.PostAsync($"/api/v1/terminals/{created.Id}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
        Assert.True((await reactivateResponse.Content.ReadFromJsonAsync<TerminalResponse>())!.IsActive);

        var listAfterReactivate = await (await client.GetAsync("/api/v1/terminals")).Content.ReadFromJsonAsync<List<TerminalResponse>>();
        Assert.Contains(listAfterReactivate!, t => t.Id == created.Id);
    }

    [Fact]
    public async Task AssignDevice_Succeeds_AndAppearsOnTheTerminal()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await CreateLocationAsync(client, caller.OrganisationId, "Assign Device Location");
        var created = await CreateTerminalAsync(client, location.Id, "Assign Device Terminal");
        var pin = await DeviceTestHelper.CreatePinAsync(client, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(client, pin.Pin);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/terminals/{created.Id}/assign-device", new AssignTerminalDeviceRequest(device.DeviceId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TerminalResponse>();
        Assert.Equal(device.DeviceId, updated!.DeviceId);

        var unassignResponse = await client.PostAsJsonAsync(
            $"/api/v1/terminals/{created.Id}/assign-device", new AssignTerminalDeviceRequest(null));
        Assert.Equal(HttpStatusCode.OK, unassignResponse.StatusCode);
        Assert.Null((await unassignResponse.Content.ReadFromJsonAsync<TerminalResponse>())!.DeviceId);
    }

    [Fact]
    public async Task AssignDevice_Rejects_DeviceFromADifferentLocation()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var terminalLocation = await CreateLocationAsync(client, caller.OrganisationId, "Terminal Location");
        var deviceLocation = await CreateLocationAsync(client, caller.OrganisationId, "Device Location");
        var created = await CreateTerminalAsync(client, terminalLocation.Id, "Cross Location Terminal");
        var pin = await DeviceTestHelper.CreatePinAsync(client, deviceLocation.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(client, pin.Pin);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/terminals/{created.Id}/assign-device", new AssignTerminalDeviceRequest(device.DeviceId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignDevice_Rejects_DeviceAlreadyLinkedToADifferentTerminal()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await CreateLocationAsync(client, caller.OrganisationId, "Conflict Location");
        var terminalA = await CreateTerminalAsync(client, location.Id, "Terminal A");
        var terminalB = await CreateTerminalAsync(client, location.Id, "Terminal B");
        var pin = await DeviceTestHelper.CreatePinAsync(client, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(client, pin.Pin);

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsJsonAsync($"/api/v1/terminals/{terminalA.Id}/assign-device", new AssignTerminalDeviceRequest(device.DeviceId))).StatusCode);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/terminals/{terminalB.Id}/assign-device", new AssignTerminalDeviceRequest(device.DeviceId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    /// Milestone C.2: proves the EF model actually carries a unique index on <c>DeviceId</c> —
    /// independent of the running database, and independent of whether
    /// <see cref="AssignDevice_Rejects_DeviceAlreadyLinkedToADifferentTerminal"/>'s app-level
    /// pre-check happens to run first.
    /// </summary>
    [Fact]
    public void TerminalModel_DeviceId_HasAUniqueIndex()
    {
        using var dbContext = CreateDbContext();
        var entityType = dbContext.Model.FindEntityType(typeof(Terminal))!;
        var index = entityType.GetIndexes().Single(i => i.Properties.Select(p => p.Name).SequenceEqual(["DeviceId"]));

        Assert.True(index.IsUnique);
    }

    /// <summary>
    /// Milestone C.2 (Gap 1): proves the database itself — not just the application's
    /// check-then-act 409 pre-check — refuses two terminals the same non-null <c>DeviceId</c>.
    /// Two independent <see cref="DaxaDbContext"/> instances race the same commit a concurrent
    /// pair of <c>assign-device</c> calls would: neither sees the other's uncommitted change, so
    /// the second <c>SaveChangesAsync()</c> can only be stopped by the unique filtered index
    /// itself, exactly the scenario the endpoint's <c>catch (DbUpdateException)</c> exists for.
    /// </summary>
    [Fact]
    public async Task Database_Rejects_TwoTerminalsWithTheSameDeviceId_EvenBypassingTheAppCheck()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await CreateLocationAsync(client, caller.OrganisationId, "DB Constraint Location");
        var terminalA = await CreateTerminalAsync(client, location.Id, "DB Terminal A");
        var terminalB = await CreateTerminalAsync(client, location.Id, "DB Terminal B");
        var pin = await DeviceTestHelper.CreatePinAsync(client, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(client, pin.Pin);

        await using var contextA = CreateDbContext();
        await using var contextB = CreateDbContext();

        var terminalAInContextA = await contextA.Terminals.IgnoreQueryFilters().SingleAsync(t => t.Id == terminalA.Id);
        var terminalBInContextB = await contextB.Terminals.IgnoreQueryFilters().SingleAsync(t => t.Id == terminalB.Id);

        terminalAInContextA.DeviceId = device.DeviceId;
        terminalBInContextB.DeviceId = device.DeviceId;

        await contextA.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateException>(() => contextB.SaveChangesAsync());
    }

    /// <summary>
    /// Milestone C.2 (Gap 1): a genuine race between two concurrent <c>assign-device</c> calls for
    /// the same device against two different terminals must never surface as a 500 — whichever call
    /// loses the race is rejected cleanly (409), whether the app's pre-check or the database's
    /// unique index is what actually catches it.
    /// </summary>
    [Fact]
    public async Task AssignDevice_ConcurrentConflictingAssignments_NeverCrash_ExactlyOneSucceeds()
    {
        var adminClient = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(adminClient, "OrganisationOwner");
        AuthenticateAs(adminClient, caller);
        var location = await CreateLocationAsync(adminClient, caller.OrganisationId, "Race Location");
        var terminalA = await CreateTerminalAsync(adminClient, location.Id, "Race Terminal A");
        var terminalB = await CreateTerminalAsync(adminClient, location.Id, "Race Terminal B");
        var pin = await DeviceTestHelper.CreatePinAsync(adminClient, location.Id);
        var device = await DeviceTestHelper.RegisterDeviceAsync(adminClient, pin.Pin);

        var clientA = _factory.CreateClient();
        AuthenticateAs(clientA, caller);
        var clientB = _factory.CreateClient();
        AuthenticateAs(clientB, caller);

        var taskA = clientA.PostAsJsonAsync($"/api/v1/terminals/{terminalA.Id}/assign-device", new AssignTerminalDeviceRequest(device.DeviceId));
        var taskB = clientB.PostAsJsonAsync($"/api/v1/terminals/{terminalB.Id}/assign-device", new AssignTerminalDeviceRequest(device.DeviceId));
        var responses = await Task.WhenAll(taskA, taskB);

        Assert.All(responses, r => Assert.NotEqual(HttpStatusCode.InternalServerError, r.StatusCode));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await CreateLocationAsync(client, caller.OrganisationId, "Kiosk Area");

        var response = await client.PostAsJsonAsync(
            "/api/v1/terminals",
            new CreateTerminalRequest("Should Fail", location.Id, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await CreateLocationAsync(client, caller.OrganisationId, "Self Checkout Area");
        var created = await CreateTerminalAsync(client, location.Id, "Self Checkout 1");

        var response = await client.PatchAsJsonAsync($"/api/v1/terminals/{created.Id}", new UpdateTerminalRequest("Should Fail", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Fails_WithoutPermission()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, owner);
        var location = await CreateLocationAsync(client, owner.OrganisationId, "Support Test Location");

        // SupportAccess does not hold terminals.manage in the catalogue.
        var caller = await RbacTestSeeder.SeedAsync(client, "SupportAccess", owner.TenantId);
        AuthenticateAs(client, caller);

        var response = await client.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest("Should Fail", location.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Read_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, owner);
        var location = await CreateLocationAsync(client, owner.OrganisationId, "Owner Location");
        var created = await CreateTerminalAsync(client, location.Id, "Owner Terminal");

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, otherTenantCaller);

        var response = await client.GetAsync($"/api/v1/terminals/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAndDeactivateAndReactivate_Blocked_ForDifferentTenant()
    {
        var client = _factory.CreateClient();
        var owner = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, owner);
        var location = await CreateLocationAsync(client, owner.OrganisationId, "Owner Location Two");
        var created = await CreateTerminalAsync(client, location.Id, "Owner Terminal Two");

        var otherTenantCaller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, otherTenantCaller);

        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync($"/api/v1/terminals/{created.Id}", new UpdateTerminalRequest("Hijacked"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/terminals/{created.Id}/deactivate", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/terminals/{created.Id}/reactivate", content: null)).StatusCode);
    }

    [Fact]
    public async Task Create_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var locationA = await CreateLocationAsync(client, callerA.OrganisationId, "Org A Location");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        // callerB tries to create a terminal under a location belonging to org A — same tenant, wrong org.
        var response = await client.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest("Should Fail", locationA.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReadAndUpdateAndDeactivate_Blocked_ForDifferentOrganisation_SameTenant()
    {
        var client = _factory.CreateClient();
        var callerA = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, callerA);
        var locationA = await CreateLocationAsync(client, callerA.OrganisationId, "Org A Location Two");
        var created = await CreateTerminalAsync(client, locationA.Id, "Org A Terminal");

        var callerB = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner", callerA.TenantId);
        AuthenticateAs(client, callerB);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/terminals/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync($"/api/v1/terminals/{created.Id}", new UpdateTerminalRequest("Hijacked"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/terminals/{created.Id}/deactivate", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/terminals/{created.Id}/reactivate", content: null)).StatusCode);

        var list = await (await client.GetAsync("/api/v1/terminals")).Content.ReadFromJsonAsync<List<TerminalResponse>>();
        Assert.DoesNotContain(list!, t => t.Id == created.Id);
    }

    [Fact]
    public async Task LifecycleActions_WriteAuditEventRows()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await CreateLocationAsync(client, caller.OrganisationId, "Audited Location");
        var created = await CreateTerminalAsync(client, location.Id, "Audited Terminal");

        await client.PatchAsJsonAsync($"/api/v1/terminals/{created.Id}", new UpdateTerminalRequest("Audited Terminal Renamed"));
        await client.PostAsync($"/api/v1/terminals/{created.Id}/deactivate", content: null);
        await client.PostAsync($"/api/v1/terminals/{created.Id}/reactivate", content: null);

        await using var context = CreateDbContext();
        var eventTypes = await context.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == created.Id && a.EntityType == "Terminal")
            .Select(a => a.EventType)
            .ToListAsync();

        Assert.Contains("TerminalCreated", eventTypes);
        Assert.Contains("TerminalUpdated", eventTypes);
        Assert.Contains("TerminalDeactivated", eventTypes);
        Assert.Contains("TerminalReactivated", eventTypes);
    }

    private static async Task<LocationResponse> CreateLocationAsync(HttpClient client, Guid organisationId, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/locations", new CreateLocationRequest(name, organisationId));
        return (await response.Content.ReadFromJsonAsync<LocationResponse>())!;
    }

    private static async Task<TerminalResponse> CreateTerminalAsync(HttpClient client, Guid locationId, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/terminals", new CreateTerminalRequest(name, locationId));
        return (await response.Content.ReadFromJsonAsync<TerminalResponse>())!;
    }

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
