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

    public DbSet<Menu> Menus => Set<Menu>();

    public DbSet<MenuSection> MenuSections => Set<MenuSection>();

    public DbSet<MenuSectionItem> MenuSectionItems => Set<MenuSectionItem>();

    public DbSet<MenuAvailabilityRule> MenuAvailabilityRules => Set<MenuAvailabilityRule>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    public DbSet<OrderLineModifier> OrderLineModifiers => Set<OrderLineModifier>();

    public DbSet<OrderLineTax> OrderLineTaxes => Set<OrderLineTax>();

    public DbSet<OrderNumberCounter> OrderNumberCounters => Set<OrderNumberCounter>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PaymentLedgerEntry> PaymentLedgerEntries => Set<PaymentLedgerEntry>();

    public DbSet<Refund> Refunds => Set<Refund>();

    public DbSet<OutboxWorkItem> OutboxWorkItems => Set<OutboxWorkItem>();

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

        // Menu construction (PLAN-0004 Milestone G). MenuSection/MenuSectionItem/MenuAvailabilityRule
        // carry no OrganisationId of their own — scoped through TenantId (fail-closed here) plus a
        // Menu parent walk at the endpoint layer, matching the Terminal/ProductVariant precedent.
        modelBuilder.Entity<Menu>()
            .HasQueryFilter(m => currentTenantProvider.TenantId != null && m.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<MenuSection>()
            .HasQueryFilter(s => currentTenantProvider.TenantId != null && s.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<MenuSectionItem>()
            .HasQueryFilter(i => currentTenantProvider.TenantId != null && i.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<MenuAvailabilityRule>()
            .HasQueryFilter(r => currentTenantProvider.TenantId != null && r.TenantId == currentTenantProvider.TenantId);

        // Order foundation (PLAN-0005 Milestone A). OrderLine/OrderLineModifier/OrderLineTax carry
        // no OrganisationId of their own — scoped entirely through TenantId (fail-closed here) plus
        // an Order parent walk at the endpoint layer, matching the Terminal/ProductVariant/MenuSection
        // precedent. OrderNumberCounter is filtered too for consistency, though it is only ever
        // touched via a raw-SQL atomic upsert (see OrderEndpoints), never a filtered LINQ query.
        modelBuilder.Entity<Order>()
            .HasQueryFilter(o => currentTenantProvider.TenantId != null && o.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<OrderLine>()
            .HasQueryFilter(l => currentTenantProvider.TenantId != null && l.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<OrderLineModifier>()
            .HasQueryFilter(m => currentTenantProvider.TenantId != null && m.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<OrderLineTax>()
            .HasQueryFilter(t => currentTenantProvider.TenantId != null && t.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<OrderNumberCounter>()
            .HasQueryFilter(c => currentTenantProvider.TenantId != null && c.TenantId == currentTenantProvider.TenantId);

        // Payment foundation (PLAN-0005 Milestone B). Payment carries no OrganisationId of its
        // own — scoped entirely through OrderId, matching OrderLine's precedent. PaymentLedgerEntry
        // follows the same denormalized-TenantId pattern the plan's Milestone A deviation already
        // established for OrderLine/OrderLineModifier/OrderLineTax.
        modelBuilder.Entity<Payment>()
            .HasQueryFilter(p => currentTenantProvider.TenantId != null && p.TenantId == currentTenantProvider.TenantId);

        modelBuilder.Entity<PaymentLedgerEntry>()
            .HasQueryFilter(e => currentTenantProvider.TenantId != null && e.TenantId == currentTenantProvider.TenantId);

        // Refund service (PLAN-0005 Milestone C). Refund carries no OrganisationId/LocationId
        // column of its own — scoped entirely through PaymentId/OrderId, matching Payment's own
        // precedent. Same fail-closed pattern as every other tenant-owned entity above; no
        // bootstrap IgnoreQueryFilters() caller is needed (every Milestone C endpoint runs under an
        // already-authenticated tenant/org context).
        modelBuilder.Entity<Refund>()
            .HasQueryFilter(r => currentTenantProvider.TenantId != null && r.TenantId == currentTenantProvider.TenantId);

        // Outbox/work-item mechanism (PLAN-0005 Milestone E, ADR-0014's Handler I/O Rule). Same
        // fail-closed pattern as every other tenant-owned entity above for any request-path query.
        // DaxaPos.Workers' cross-tenant poll query is the one deliberate, documented exception —
        // it runs with no tenant context of its own (there is no HTTP request to derive one from)
        // and must see every tenant's pending rows, so it calls IgnoreQueryFilters() explicitly,
        // added to IgnoreQueryFiltersUsageTests' approved list alongside the pre-auth call sites.
        modelBuilder.Entity<OutboxWorkItem>()
            .HasQueryFilter(w => currentTenantProvider.TenantId != null && w.TenantId == currentTenantProvider.TenantId);
    }
}
