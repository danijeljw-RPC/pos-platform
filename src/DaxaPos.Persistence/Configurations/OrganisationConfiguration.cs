using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class OrganisationConfiguration : IEntityTypeConfiguration<Organisation>
{
    public void Configure(EntityTypeBuilder<Organisation> builder)
    {
        builder.ToTable("organisations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Lifecycle state (PLAN-0003 Milestone D) — deliberately not part of the tenant-isolation
        // query filter (DaxaDbContext.OnModelCreating): tenant isolation and lifecycle visibility
        // are separate concerns. An inactive organisation is still visible to its own tenant; it is
        // up to each endpoint to decide whether inactive rows should be included.
        builder.Property(o => o.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(o => o.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(o => o.TenantId);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(o => o.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
