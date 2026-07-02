using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Persistence;

public class DaxaDbContext(DbContextOptions<DaxaDbContext> options, ICurrentTenantProvider currentTenantProvider)
    : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Organisation> Organisations => Set<Organisation>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Device> Devices => Set<Device>();

    public DbSet<Terminal> Terminals => Set<Terminal>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<DeviceCredential> DeviceCredentials => Set<DeviceCredential>();

    public DbSet<DeviceRegistrationPin> DeviceRegistrationPins => Set<DeviceRegistrationPin>();

    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();

    public DbSet<StaffMemberRole> StaffMemberRoles => Set<StaffMemberRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DaxaDbContext).Assembly);

        // Tenant isolation (ADR-0015): every tenant-owned entity is filtered against the current
        // tenant context. Fails closed — a null (missing) TenantId matches zero rows, never an
        // unfiltered query. `Tenant` itself is the root and is intentionally not filtered here.
        modelBuilder.Entity<Organisation>()
            .HasQueryFilter(o => currentTenantProvider.TenantId != null && o.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<Location>()
            .HasQueryFilter(l => currentTenantProvider.TenantId != null && l.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<Device>()
            .HasQueryFilter(d => currentTenantProvider.TenantId != null && d.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<Terminal>()
            .HasQueryFilter(t => currentTenantProvider.TenantId != null && t.TenantId == currentTenantProvider.TenantId);

        // `Role`/`Permission`/`RolePermission` are system-wide catalogues, not tenant-owned — no
        // filter. `User`, `UserRole`, `AuthSession`, `AuditEvent`, `DeviceCredential`, and
        // `DeviceRegistrationPin` are tenant-owned and follow the same fail-closed pattern. A small,
        // fixed set of callers must deliberately bypass this with IgnoreQueryFilters() because they
        // run before any tenant context can exist — looking up a User by email during login, an
        // AuthSession by token hash during session validation, a DeviceCredential (and its
        // Device/Location) during device-token validation, and the registration-PIN candidate scan
        // during pre-auth device registration — each documented at its call site, per ADR-0015's
        // narrow-bootstrap-exception rule.
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => currentTenantProvider.TenantId != null && u.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<UserRole>()
            .HasQueryFilter(ur => currentTenantProvider.TenantId != null && ur.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<AuthSession>()
            .HasQueryFilter(s => currentTenantProvider.TenantId != null && s.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<AuditEvent>()
            .HasQueryFilter(a => currentTenantProvider.TenantId != null && a.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<DeviceCredential>()
            .HasQueryFilter(c => currentTenantProvider.TenantId != null && c.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<DeviceRegistrationPin>()
            .HasQueryFilter(p => currentTenantProvider.TenantId != null && p.TenantId == currentTenantProvider.TenantId);

        // StaffMember/StaffMemberRole (Milestone F) never need a bootstrap bypass: staff PIN login
        // runs after DeviceTokenAuthenticationHandler has established the device's tenant context,
        // so the staff lookup happens under this normal fail-closed filter.
        modelBuilder.Entity<StaffMember>()
            .HasQueryFilter(s => currentTenantProvider.TenantId != null && s.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<StaffMemberRole>()
            .HasQueryFilter(sr => currentTenantProvider.TenantId != null && sr.TenantId == currentTenantProvider.TenantId);
    }
}
