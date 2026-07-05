using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.DisplayOrder).IsRequired();
        builder.Property(c => c.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.Property(c => c.TenantId).IsRequired();
        builder.HasIndex(c => c.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.OrganisationId).IsRequired();
        builder.HasIndex(c => c.OrganisationId);
        builder.HasOne<Organisation>().WithMany().HasForeignKey(c => c.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }
}
