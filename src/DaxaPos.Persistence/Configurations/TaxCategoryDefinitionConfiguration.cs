using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class TaxCategoryDefinitionConfiguration : IEntityTypeConfiguration<TaxCategoryDefinition>
{
    public void Configure(EntityTypeBuilder<TaxCategoryDefinition> builder)
    {
        builder.ToTable("tax_category_definitions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Priority).IsRequired();
        builder.Property(t => t.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(t => t.CreatedAtUtc).IsRequired();

        builder.Property(t => t.TenantId).IsRequired();
        builder.HasIndex(t => t.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.TaxCategoryId).IsRequired();
        builder.HasIndex(t => t.TaxCategoryId);
        builder.HasOne<TaxCategory>().WithMany().HasForeignKey(t => t.TaxCategoryId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.TaxDefinitionId).IsRequired();
        builder.HasIndex(t => t.TaxDefinitionId);
        builder.HasOne<TaxDefinition>().WithMany().HasForeignKey(t => t.TaxDefinitionId).OnDelete(DeleteBehavior.Restrict);

        // Null = organisation-wide mapping (no Location row to reference).
        builder.HasIndex(t => t.LocationId);
        builder.HasOne<Location>().WithMany().HasForeignKey(t => t.LocationId).OnDelete(DeleteBehavior.Restrict);
    }
}
