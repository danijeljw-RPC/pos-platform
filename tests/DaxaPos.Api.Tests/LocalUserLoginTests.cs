using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Tests.Support;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Infrastructure.Security;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

public class LocalUserLoginTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BootstrapEmail = "bootstrap-admin@milestone-c.test";
    private const string BootstrapPassword = "Bootstrap-Test-Passw0rd!";

    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private readonly WebApplicationFactory<Program> _factory;

    public LocalUserLoginTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DaxaDb", ConnectionString);
            builder.UseSetting(BootstrapAdminSeeder.AdminEmailConfigKey, BootstrapEmail);
            builder.UseSetting(BootstrapAdminSeeder.AdminPasswordConfigKey, BootstrapPassword);
        });
    }

    [Fact]
    public async Task Login_Succeeds_WithCorrectCredentials()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/local/login", new LocalLoginRequest(BootstrapEmail, BootstrapPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LocalLoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.SessionToken));
        Assert.Contains("SystemAdmin", body.Roles);
        Assert.Contains(Permissions.OrganisationsManage, body.Permissions);
    }

    [Fact]
    public async Task Login_Fails_WithWrongPassword()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/local/login", new LocalLoginRequest(BootstrapEmail, "definitely-wrong"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_Fails_WithUnknownEmail()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/local/login",
            new LocalLoginRequest($"unknown-{Guid.NewGuid()}@example.com", "whatever"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_Fails_ForInactiveUser()
    {
        var (email, password) = await SeedTestUserAsync(isActive: false);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/local/login", new LocalLoginRequest(email, password));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_LocksOut_AfterFiveFailedAttempts_EvenWithCorrectPasswordAfterward()
    {
        var (email, password) = await SeedTestUserAsync(isActive: true);
        var client = _factory.CreateClient();

        for (var i = 0; i < LoginLockoutPolicy.MaxFailedAttempts; i++)
        {
            var failedResponse = await client.PostAsJsonAsync("/api/v1/auth/local/login", new LocalLoginRequest(email, "wrong"));
            Assert.Equal(HttpStatusCode.Unauthorized, failedResponse.StatusCode);
        }

        var responseAfterLockout = await client.PostAsJsonAsync("/api/v1/auth/local/login", new LocalLoginRequest(email, password));

        Assert.Equal(HttpStatusCode.Unauthorized, responseAfterLockout.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsIdentity_WhenAuthenticated()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client, BootstrapEmail, BootstrapPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthContextResponse>();
        Assert.NotNull(body);
        Assert.Equal("LocalUsernamePassword", body!.AuthMethod);
        Assert.Contains("SystemAdmin", body.Roles);
    }

    [Fact]
    public async Task Me_ReturnsUnauthorized_WithoutToken()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsUnauthorized_WithGarbageToken()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-session-token");

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesSession_SoOldTokenNoLongerWorks()
    {
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client, BootstrapEmail, BootstrapPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var logoutResponse = await client.PostAsync("/api/v1/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        var meResponse = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    [Fact]
    public async Task SuccessfulLogin_WritesAuditEventRow()
    {
        var (email, password) = await SeedTestUserAsync(isActive: true);
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/local/login", new LocalLoginRequest(email, password));

        await using var context = CreateDbContext();
        var user = await context.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == email);
        var auditRow = await context.AuditEvents.IgnoreQueryFilters()
            .SingleOrDefaultAsync(a => a.UserId == user.Id && a.EventType == "LocalUserLoginSucceeded");

        Assert.NotNull(auditRow);
        Assert.Equal(user.TenantId, auditRow!.TenantId);
    }

    [Fact]
    public async Task FailedLogin_WritesAuditEventRow()
    {
        var (email, _) = await SeedTestUserAsync(isActive: true);
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/local/login", new LocalLoginRequest(email, "wrong-password"));

        await using var context = CreateDbContext();
        var user = await context.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == email);
        var auditRow = await context.AuditEvents.IgnoreQueryFilters()
            .SingleOrDefaultAsync(a => a.UserId == user.Id && a.EventType == "LocalUserLoginFailed");

        Assert.NotNull(auditRow);
    }

    [Fact]
    public async Task UnknownEmail_DoesNotWriteAnyAuditEventRow()
    {
        var email = $"unknown-{Guid.NewGuid()}@example.com";
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/local/login", new LocalLoginRequest(email, "whatever"));

        await using var context = CreateDbContext();
        var anyAuditRowMentioningThisAttempt = await context.AuditEvents.IgnoreQueryFilters()
            .AnyAsync(a => a.Reason != null && a.Reason.Contains(email));

        Assert.False(anyAuditRowMentioningThisAttempt);
    }

    private static async Task<string> LoginAndGetTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/local/login", new LocalLoginRequest(email, password));
        var body = await response.Content.ReadFromJsonAsync<LocalLoginResponse>();
        return body!.SessionToken;
    }

    private async Task<(string Email, string Password)> SeedTestUserAsync(bool isActive)
    {
        var email = $"test-user-{Guid.NewGuid()}@example.com";
        const string password = "Test-User-Passw0rd!";

        await using var context = CreateDbContext();

        var tenantId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        context.Tenants.Add(new Tenant { Id = tenantId, Name = "Test Tenant", CreatedAtUtc = now });
        context.Organisations.Add(new Organisation { Id = organisationId, TenantId = tenantId, Name = "Test Org", CreatedAtUtc = now });
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrganisationId = organisationId,
            Email = email,
            DisplayName = "Test User",
            PasswordHash = new Pbkdf2PinHasher().Hash(password),
            IsActive = isActive,
            CreatedAtUtc = now,
        });

        await context.SaveChangesAsync();

        return (email, password);
    }

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
