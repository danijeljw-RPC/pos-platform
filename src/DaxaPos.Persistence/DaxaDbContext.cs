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

    public DbSet<TaxDefinitionTemplate> TaxDefinitionTemplates => Set<TaxDefinitionTemplate>();

    public DbSet<TaxDefinition> TaxDefinitions => Set<TaxDefinition>();

    public DbSet<TaxCategory> TaxCategories => Set<TaxCategory>();

    public DbSet<TaxCategoryDefinition> TaxCategoryDefinitions => Set<TaxCategoryDefinition>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ModifierGroup> ModifierGroups => Set<ModifierGroup>();

    public DbSet<Modifier> Modifiers => Set<Modifier>();

    public DbSet<ProductModifierGroup> ProductModifierGroups => Set<ProductModifierGroup>();

    public DbSet<ProductLocationOverride> ProductLocationOverrides => Set<ProductLocationOverride>();

    public DbSet<VenueTaxConfiguration> VenueTaxConfigurations => Set<VenueTaxConfiguration>();

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

        // Tax foundation (PLAN-0004 Milestone B). `TaxDefinitionTemplate` is a system-wide,
        // unfiltered reference catalogue — same status as Role/Permission — never tenant-edited.
        // `TaxDefinition`/`TaxCategory`/`TaxCategoryDefinition` are tenant-owned and follow the
        // same fail-closed pattern as every other tenant-owned entity above; no bootstrap
        // IgnoreQueryFilters() caller is needed for any of them (every Milestone C+ endpoint runs
        // under an already-authenticated tenant/org context, unlike PLAN-0003's pre-auth flows).
        modelBuilder.Entity<TaxDefinition>()
            .HasQueryFilter(t => currentTenantProvider.TenantId != null && t.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<TaxCategory>()
            .HasQueryFilter(t => currentTenantProvider.TenantId != null && t.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<TaxCategoryDefinition>()
            .HasQueryFilter(t => currentTenantProvider.TenantId != null && t.TenantId == currentTenantProvider.TenantId);

        // Product catalogue foundation (PLAN-0004 Milestone D). Both entities are tenant-owned and
        // follow the same fail-closed pattern as every other tenant-owned entity above; no
        // bootstrap IgnoreQueryFilters() caller is needed (every Milestone D endpoint runs under an
        // already-authenticated tenant/org context).
        modelBuilder.Entity<ProductCategory>()
            .HasQueryFilter(c => currentTenantProvider.TenantId != null && c.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<Product>()
            .HasQueryFilter(p => currentTenantProvider.TenantId != null && p.TenantId == currentTenantProvider.TenantId);

        // Variants and modifiers (PLAN-0004 Milestone E). ProductVariant/Modifier/ProductModifierGroup
        // carry no OrganisationId of their own — scoped entirely through TenantId (fail-closed here)
        // plus a Product/ModifierGroup parent walk at the endpoint layer, matching the Terminal
        // precedent from PLAN-0003 Milestone D.
        modelBuilder.Entity<ProductVariant>()
            .HasQueryFilter(v => currentTenantProvider.TenantId != null && v.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<ModifierGroup>()
            .HasQueryFilter(g => currentTenantProvider.TenantId != null && g.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<Modifier>()
            .HasQueryFilter(m => currentTenantProvider.TenantId != null && m.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<ProductModifierGroup>()
            .HasQueryFilter(pmg => currentTenantProvider.TenantId != null && pmg.TenantId == currentTenantProvider.TenantId);

        // Location-level catalog overrides and venue tax configuration (PLAN-0004 Milestone F).
        // Neither carries an OrganisationId of its own — scoped through LocationId, matching the
        // Terminal precedent from PLAN-0003 Milestone D.
        modelBuilder.Entity<ProductLocationOverride>()
            .HasQueryFilter(o => currentTenantProvider.TenantId != null && o.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<VenueTaxConfiguration>()
            .HasQueryFilter(v => currentTenantProvider.TenantId != null && v.TenantId == currentTenantProvider.TenantId);
    }
}
