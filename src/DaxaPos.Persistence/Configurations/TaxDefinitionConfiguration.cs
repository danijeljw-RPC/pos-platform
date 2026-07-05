using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class TaxDefinitionConfiguration : IEntityTypeConfiguration<TaxDefinition>
{
    public void Configure(EntityTypeBuilder<TaxDefinition> builder)
    {
        builder.ToTable("tax_definitions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.CountryCode).IsRequired().HasMaxLength(10);
        builder.Property(t => t.RegionCode).HasMaxLength(10);
        builder.Property(t => t.RatePercent).IsRequired().HasPrecision(9, 4);
        builder.Property(t => t.JurisdictionName).IsRequired().HasMaxLength(200);
        builder.Property(t => t.JurisdictionType).IsRequired();
        builder.Property(t => t.IncludedInPrice).IsRequired();
        builder.Property(t => t.RoundingMode).IsRequired();
        builder.Property(t => t.RoundingPrecision).IsRequired();
        builder.Property(t => t.CalculationScope).IsRequired();
        builder.Property(t => t.ReceiptMarkerCode).HasMaxLength(10);
        builder.Property(t => t.ReceiptMarkerLabel).HasMaxLength(100);
        builder.Property(t => t.ReportingCategory).HasMaxLength(100);
        builder.Property(t => t.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(t => t.SourceTemplateCode).HasMaxLength(100);
        builder.Property(t => t.CreatedAtUtc).IsRequired();

        // Denormalized for tenant-isolation query filters (ADR-0015), matching Location's pattern.
        builder.Property(t => t.TenantId).IsRequired();
        builder.HasIndex(t => t.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.OrganisationId).IsRequired();
        builder.HasIndex(t => t.OrganisationId);
        builder.HasOne<Organisation>().WithMany().HasForeignKey(t => t.OrganisationId).OnDelete(DeleteBehavior.Restrict);

        // A tenant's own Code must be unique among its own rows, but the same Code (e.g.
        // AU_GST_10) is expected to recur across different tenants that both cloned it.
        builder.HasIndex(t => new { t.TenantId, t.Code }).IsUnique();
    }
}
