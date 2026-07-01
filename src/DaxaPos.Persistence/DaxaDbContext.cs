using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Persistence;

public class DaxaDbContext(DbContextOptions<DaxaDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Organisation> Organisations => Set<Organisation>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Device> Devices => Set<Device>();

    public DbSet<Terminal> Terminals => Set<Terminal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DaxaDbContext).Assembly);
    }
}
