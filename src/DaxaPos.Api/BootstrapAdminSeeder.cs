using DaxaPos.Application.Identity;
using DaxaPos.Domain.Entities;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Api;

/// <summary>
/// Dev/local-only bootstrap seeding for the very first <c>SystemAdmin</c> user. There is no public
/// API to create the first tenant/organisation/user (every management endpoint requires already
/// being authenticated as someone with <c>users.manage</c>), so a one-time, idempotent, env-var
/// driven seed is unavoidable.
/// </summary>
/// <remarks>
/// Requires both <see cref="AdminEmailConfigKey"/> and <see cref="AdminPasswordConfigKey"/> to be
/// set — if either is missing, seeding is skipped (logged, not thrown) and the app still starts
/// normally; there is no guessable fallback credential anywhere in source. Idempotent: if a user
/// with the configured email already exists, seeding is skipped without touching (and, in
/// particular, without resetting) that user's password.
/// </remarks>
public static class BootstrapAdminSeeder
{
    public const string AdminEmailConfigKey = "DAXA_BOOTSTRAP_ADMIN_EMAIL";
    public const string AdminPasswordConfigKey = "DAXA_BOOTSTRAP_ADMIN_PASSWORD";

    public static async Task SeedAsync(IServiceProvider rootServices)
    {
        await using var scope = rootServices.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(BootstrapAdminSeeder));

        var email = configuration[AdminEmailConfigKey];
        var password = configuration[AdminPasswordConfigKey];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Bootstrap admin seeding skipped: {EmailKey}/{PasswordKey} are not both set. This " +
                "is expected once a real admin already exists. For a brand-new deployment with no " +
                "admin yet, set both (dev/local only — see deploy/.env.example). There is no " +
                "fallback default credential.",
                AdminEmailConfigKey,
                AdminPasswordConfigKey);
            return;
        }

        var dbContext = services.GetRequiredService<DaxaDbContext>();
        var passwordHasher = services.GetRequiredService<IPinHasher>();

        // Bootstrap exception (ADR-0015 §1): runs at startup, outside any request/tenant context,
        // with no client input at all — not an endpoint, not fed by anything a caller controls.
        var existingUser = await dbContext.Users.IgnoreQueryFilters().SingleOrDefaultAsync(u => u.Email == email);

        if (existingUser is not null)
        {
            logger.LogInformation("Bootstrap admin already exists ({Email}); skipping seeding.", email);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var tenantId = Guid.NewGuid();
        var organisationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        dbContext.Tenants.Add(new Tenant { Id = tenantId, Name = "Bootstrap Tenant", CreatedAtUtc = now });
        dbContext.Organisations.Add(new Organisation { Id = organisationId, TenantId = tenantId, Name = "Bootstrap Organisation", CreatedAtUtc = now });
        dbContext.Users.Add(new User
        {
            Id = userId,
            TenantId = tenantId,
            OrganisationId = organisationId,
            Email = email,
            DisplayName = "Bootstrap Admin",
            PasswordHash = passwordHasher.Hash(password),
            IsActive = true,
            CreatedAtUtc = now,
        });

        var systemAdminRole = await dbContext.Roles.SingleAsync(r => r.Name == "SystemAdmin");
        dbContext.UserRoles.Add(new UserRole { UserId = userId, RoleId = systemAdminRole.Id, TenantId = tenantId });

        await dbContext.SaveChangesAsync();

        logger.LogWarning(
            "Bootstrap admin created ({Email}) under a newly created bootstrap tenant/organisation. " +
            "This is a dev/local-only seeding path — rotate or replace this credential before any real use.",
            email);
    }
}
