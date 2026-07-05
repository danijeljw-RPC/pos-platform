using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class VenueTaxConfigurationConfiguration : IEntityTypeConfiguration<VenueTaxConfiguration>
{
    public void Configure(EntityTypeBuilder<VenueTaxConfiguration> builder)
    {
        builder.ToTable("venue_tax_configurations");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.TaxInclusivePricing).IsRequired();
        builder.Property(v => v.TaxCalculationMode).IsRequired();
        builder.Property(v => v.CreatedAtUtc).IsRequired();

        builder.Property(v => v.TenantId).IsRequired();
        builder.HasIndex(v => v.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(v => v.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(v => v.LocationId).IsRequired();
        builder.HasOne<Location>().WithMany().HasForeignKey(v => v.LocationId).OnDelete(DeleteBehavior.Restrict);

        // One row per location (plan's own text).
        builder.HasIndex(v => v.LocationId).IsUnique();
    }
}
