using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class TaxCategoryConfiguration : IEntityTypeConfiguration<TaxCategory>
{
    public void Configure(EntityTypeBuilder<TaxCategory> builder)
    {
        builder.ToTable("tax_categories");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.TaxTreatment).IsRequired();
        builder.Property(t => t.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(t => t.CreatedAtUtc).IsRequired();

        builder.Property(t => t.TenantId).IsRequired();
        builder.HasIndex(t => t.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.OrganisationId).IsRequired();
        builder.HasIndex(t => t.OrganisationId);
        builder.HasOne<Organisation>().WithMany().HasForeignKey(t => t.OrganisationId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.TenantId, t.Code }).IsUnique();
    }
}
