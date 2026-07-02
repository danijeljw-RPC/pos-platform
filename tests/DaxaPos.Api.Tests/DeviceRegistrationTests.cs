using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Domain.Entities;
using DaxaPos.Infrastructure.Security;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

/// <summary>
/// Pre-auth device registration flow tests (ADR-0008, PLAN-0003 Milestone E): a live PIN registers
/// a device exactly per its MaxUses; expired/revoked/exhausted PINs fail and are audited against
/// their tenant; an unknown PIN fails generically with no audit row (no tenant for the
/// non-nullable AuditEvent.TenantId — the Milestone C unknown-email precedent); ambiguous PIN
/// matches fail closed. Rate limiting is tested separately in
/// <c>DeviceRegistrationRateLimitTests</c> with the default permit limit.
/// </summary>
public class DeviceRegistrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public DeviceRegistrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
            builder.UseSetting("DeviceRegistration:RateLimitPermitLimit", "1000");
        });
    }

    [Fact]
    public async Task Register_WithValidPin_CreatesDeviceAndActiveCredential()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "Registration Venue");
        var pin = await DeviceTestHelper.CreatePinAsync(client, location.Id);

        var response = await DeviceTestHelper.RegisterDeviceRawAsync(client, pin.Pin, "WindowsPos", "Front Counter POS");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var registered = await response.Content.ReadFromJsonAsync<RegisterDeviceResponse>();
        Assert.NotNull(registered);
        Assert.Equal(caller.TenantId, registered!.TenantId);
        Assert.Equal(caller.OrganisationId, registered.OrganisationId);
        Assert.Equal(location.Id, registered.LocationId);
        Assert.Equal("WindowsPos", registered.DeviceType);

        // Token format: {credentialId}.{secret}, per the approved Milestone E decision 2.
        var parts = registered.DeviceToken.Split('.', 2);
        Assert.Equal(2, parts.Length);
        Assert.True(Guid.TryParse(parts[0], out var credentialId));

        await using var dbContext = CreateDbContext();
        var device = await dbContext.Devices.IgnoreQueryFilters().SingleAsync(d => d.Id == registered.DeviceId);
        Assert.Equal(caller.TenantId, device.TenantId);
        Assert.Equal(location.Id, device.LocationId);

        var credential = await dbContext.DeviceCredentials.IgnoreQueryFilters().SingleAsync(c => c.Id == credentialId);
        Assert.Equal(DeviceCredentialStatus.Active, credential.Status);
        Assert.Equal(registered.DeviceId, credential.DeviceId);

        var pinRow = await dbContext.DeviceRegistrationPins.IgnoreQueryFilters().SingleAsync(p => p.Id == pin.Id);
        Assert.Equal(1, pinRow.UsedCount);

        var auditEventTypes = await dbContext.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == registered.DeviceId && a.EntityType == "Device")
            .Select(a => a.EventType)
            .ToListAsync();
        Assert.Contains("DeviceRegistered", auditEventTypes);
    }

    [Fact]
    public async Task Register_NeverPersistsTheRawSecret()
    {
        var client = _factory.CreateClient();
        var (registered, _, _) = await RegisterDeviceAsync(client);

        var parts = registered.DeviceToken.Split('.', 2);
        var credentialId = Guid.Parse(parts[0]);
        var secret = parts[1];

        await using var dbContext = CreateDbContext();
        var credential = await dbContext.DeviceCredentials.IgnoreQueryFilters().SingleAsync(c => c.Id == credentialId);

        Assert.NotEqual(secret, credential.CredentialHash);
        Assert.NotEqual(registered.DeviceToken, credential.CredentialHash);
        Assert.True(new HmacDeviceCredentialHasher().Verify(secret, credential.CredentialHash));
    }

    [Fact]
    public async Task Register_Rejects_ClientSuppliedTenantId()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/device-registration",
            new RegisterDeviceRequest("123456", "WindowsPos", TenantId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_Rejects_UnknownDeviceType()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/device-registration",
            new RegisterDeviceRequest("123456", "Toaster"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_EnforcesMaxUses_AndAuditsExhaustion()
    {
        var client = _factory.CreateClient();
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, "MaxUses Enforcement Venue");
        var pin = await DeviceTestHelper.CreatePinAsync(client, location.Id, maxUses: 2);

        Assert.Equal(HttpStatusCode.Created, (await DeviceTestHelper.RegisterDeviceRawAsync(client, pin.Pin)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await DeviceTestHelper.RegisterDeviceRawAsync(client, pin.Pin)).StatusCode);

        var third = await DeviceTestHelper.RegisterDeviceRawAsync(client, pin.Pin);
        Assert.Equal(HttpStatusCode.Unauthorized, third.StatusCode);

        await AssertRegistrationFailedAuditAsync(pin.Id, "PinExhausted");
    }

    [Fact]
    public async Task Register_Fails_WithExpiredPin_AndAudits()
    {
        var client = _factory.CreateClient();
        var (caller, location) = await SeedCallerWithLocationAsync(client, "Expired Pin Venue");
        var (pinId, rawPin) = await SeedPinDirectlyAsync(
            caller, location.Id, expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1));

        var response = await DeviceTestHelper.RegisterDeviceRawAsync(client, rawPin);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertRegistrationFailedAuditAsync(pinId, "PinExpired");
    }

    [Fact]
    public async Task Register_Fails_WithRevokedPin_AndAudits()
    {
        var client = _factory.CreateClient();
        var (caller, location) = await SeedCallerWithLocationAsync(client, "Revoked Pin Venue");
        var (pinId, rawPin) = await SeedPinDirectlyAsync(
            caller, location.Id, revokedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1));

        var response = await DeviceTestHelper.RegisterDeviceRawAsync(client, rawPin);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertRegistrationFailedAuditAsync(pinId, "PinRevoked");
    }

    [Fact]
    public async Task Register_UnknownPin_FailsGenerically_AndWritesNoAuditRow()
    {
        var client = _factory.CreateClient();

        // Ambiguous-match rows are the only DeviceRegistrationFailed rows without an EntityId;
        // an unknown PIN must add nothing at all. Counting only EntityId-null rows keeps this
        // assertion safe against parallel test classes writing pin-specific failure rows.
        await using var dbContext = CreateDbContext();
        var before = await CountPinlessFailureRowsAsync(dbContext);

        var response = await DeviceTestHelper.RegisterDeviceRawAsync(client, RandomPin());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(before, await CountPinlessFailureRowsAsync(dbContext));
    }

    [Fact]
    public async Task Register_AmbiguousPin_AcrossTenants_FailsClosed_WithoutAudit()
    {
        var client = _factory.CreateClient();
        var (callerA, locationA) = await SeedCallerWithLocationAsync(client, "Ambiguity Venue A");
        var (callerB, locationB) = await SeedCallerWithLocationAsync(client, "Ambiguity Venue B");
        var sharedPin = RandomPin();
        await SeedPinDirectlyAsync(callerA, locationA.Id, rawPin: sharedPin);
        await SeedPinDirectlyAsync(callerB, locationB.Id, rawPin: sharedPin);

        await using var dbContext = CreateDbContext();
        var before = await CountPinlessFailureRowsAsync(dbContext);

        var response = await DeviceTestHelper.RegisterDeviceRawAsync(client, sharedPin);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        // Two tenants matched — no single tenant resolvable, so no audit row (approved decision 4).
        Assert.Equal(before, await CountPinlessFailureRowsAsync(dbContext));
    }

    [Fact]
    public async Task Register_AmbiguousPin_WithinOneTenant_FailsClosed_WithAudit()
    {
        var client = _factory.CreateClient();
        var (caller, location) = await SeedCallerWithLocationAsync(client, "Same Tenant Ambiguity Venue");
        var sharedPin = RandomPin();
        await SeedPinDirectlyAsync(caller, location.Id, rawPin: sharedPin);
        await SeedPinDirectlyAsync(caller, location.Id, rawPin: sharedPin);

        var response = await DeviceTestHelper.RegisterDeviceRawAsync(client, sharedPin);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await using var dbContext = CreateDbContext();
        var auditRow = await dbContext.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.TenantId == caller.TenantId
                && a.EventType == "DeviceRegistrationFailed"
                && a.Reason == "AmbiguousPinMatch")
            .FirstOrDefaultAsync();

        Assert.NotNull(auditRow);
    }

    private async Task<(SeededCaller Caller, LocationResponse Location)> SeedCallerWithLocationAsync(HttpClient client, string venueName)
    {
        var caller = await RbacTestSeeder.SeedAsync(client, "OrganisationOwner");
        AuthenticateAs(client, caller);
        var location = await DeviceTestHelper.CreateLocationAsync(client, caller.OrganisationId, venueName);
        return (caller, location);
    }

    private async Task<(RegisterDeviceResponse Registered, SeededCaller Caller, LocationResponse Location)> RegisterDeviceAsync(HttpClient client)
    {
        var (caller, location) = await SeedCallerWithLocationAsync(client, $"Venue {Guid.NewGuid()}");
        var pin = await DeviceTestHelper.CreatePinAsync(client, location.Id);
        var registered = await DeviceTestHelper.RegisterDeviceAsync(client, pin.Pin);
        return (registered, caller, location);
    }

    /// <summary>
    /// Seeds a PIN row directly (bypassing the endpoint) so expired/revoked/colliding states can
    /// be constructed — the issuance endpoint always creates live, random PINs.
    /// </summary>
    private static async Task<(Guid PinId, string RawPin)> SeedPinDirectlyAsync(
        SeededCaller caller,
        Guid locationId,
        DateTimeOffset? expiresAtUtc = null,
        DateTimeOffset? revokedAtUtc = null,
        string? rawPin = null)
    {
        rawPin ??= RandomPin();
        var pin = new DeviceRegistrationPin
        {
            Id = Guid.NewGuid(),
            TenantId = caller.TenantId,
            OrganisationId = caller.OrganisationId,
            LocationId = locationId,
            PinHash = new HmacDeviceCredentialHasher().Hash(rawPin),
            ExpiresAtUtc = expiresAtUtc ?? DateTimeOffset.UtcNow.AddMinutes(15),
            MaxUses = 1,
            UsedCount = 0,
            CreatedByUserId = caller.UserId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RevokedAtUtc = revokedAtUtc,
        };

        await using var dbContext = CreateDbContext();
        dbContext.DeviceRegistrationPins.Add(pin);
        await dbContext.SaveChangesAsync();

        return (pin.Id, rawPin);
    }

    private static async Task AssertRegistrationFailedAuditAsync(Guid pinId, string expectedReason)
    {
        await using var dbContext = CreateDbContext();
        var reasons = await dbContext.AuditEvents.IgnoreQueryFilters()
            .Where(a => a.EntityId == pinId && a.EventType == "DeviceRegistrationFailed")
            .Select(a => a.Reason)
            .ToListAsync();

        Assert.Contains(expectedReason, reasons);
    }

    private static Task<int> CountPinlessFailureRowsAsync(DaxaDbContext dbContext) =>
        dbContext.AuditEvents.IgnoreQueryFilters()
            .CountAsync(a => a.EventType == "DeviceRegistrationFailed" && a.EntityId == null);

    private static string RandomPin() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static void AuthenticateAs(HttpClient client, SeededCaller caller) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", caller.Token);

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
