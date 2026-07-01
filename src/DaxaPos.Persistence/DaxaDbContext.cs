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
    }
}
