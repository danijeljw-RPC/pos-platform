using System.Net.Http.Json;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Infrastructure.Security;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests.Support;

/// <summary>
/// Result of <see cref="RbacTestSeeder.SeedAsync"/> — everything a Milestone D authorization test
/// needs to act as a logged-in caller with a specific seeded role.
/// </summary>
public sealed record SeededCaller(Guid TenantId, Guid OrganisationId, Guid UserId, string Email, string Token);

/// <summary>
/// Shared test helper (PLAN-0003 Milestone D) that seeds a <see cref="Tenant"/> +
/// <see cref="Organisation"/> + <see cref="User"/>, assigns a named seeded <see cref="Role"/> (looked
/// up by <see cref="Role.Name"/> from the Milestone C <c>HasData</c> catalogue — not
/// <c>RbacSeedIds</c> directly, since that class is <c>internal</c> to <c>DaxaPos.Persistence</c>),
/// and logs in via <c>POST /api/v1/auth/local/login</c> to obtain a bearer token. Avoids
/// near-duplicating <c>LocalUserLoginTests.SeedTestUserAsync</c> three times across the new
/// Organisation/Location/Terminal test files, none of which need a role/permission assignment.
/// </summary>
public static class RbacTestSeeder
{
    private const string Password = "Rbac-Test-Passw0rd!";

    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    /// <summary>
    /// Seeds a caller with <paramref name="roleName"/> (e.g. <c>"SystemAdmin"</c>,
    /// <c>"OrganisationOwner"</c>, <c>"VenueManager"</c>). Pass <paramref name="existingTenantId"/>
    /// to seed a second organisation under an already-seeded tenant — the specific shape needed by
    /// the "same tenant, different organisation" authorization tests.
    /// </summary>
    public static async Task<SeededCaller> SeedAsync(HttpClient client, string roleName, Guid? existingTenantId = null)
    {
        var tenantId = existingTenantId ?? Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var email = $"rbac-test-{Guid.NewGuid()}@example.com";
        var now = DateTimeOffset.UtcNow;

        await using (var dbContext = CreateDbContext())
        {
            var role = await dbContext.Roles.SingleAsync(r => r.Name == roleName);

            if (existingTenantId is null)
            {
                dbContext.Tenants.Add(new Tenant { Id = tenantId, Name = $"Tenant {tenantId}", CreatedAtUtc = now });
            }

            dbContext.Organisations.Add(new Organisation
            {
                Id = organisationId,
                TenantId = tenantId,
                Name = $"Org {organisationId}",
                IsActive = true,
                CreatedAtUtc = now,
            });

            dbContext.Users.Add(new User
            {
                Id = userId,
                TenantId = tenantId,
                OrganisationId = organisationId,
                Email = email,
                DisplayName = "RBAC Test User",
                PasswordHash = new Pbkdf2PinHasher().Hash(Password),
                IsActive = true,
                CreatedAtUtc = now,
            });

            dbContext.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id, TenantId = tenantId });

            await dbContext.SaveChangesAsync();
        }

        var token = await LoginAsync(client, email);

        return new SeededCaller(tenantId, organisationId, userId, email, token);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/local/login", new LocalLoginRequest(email, Password));
        var body = await response.Content.ReadFromJsonAsync<LocalLoginResponse>();
        return body!.SessionToken;
    }

    private static DaxaDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(null));
}
