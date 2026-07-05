using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Sku).HasMaxLength(100);
        builder.Property(p => p.Barcode).HasMaxLength(100);
        builder.Property(p => p.BasePrice).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(p => p.IsArchived).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.Property(p => p.TenantId).IsRequired();
        builder.HasIndex(p => p.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.OrganisationId).IsRequired();
        builder.HasIndex(p => p.OrganisationId);
        builder.HasOne<Organisation>().WithMany().HasForeignKey(p => p.OrganisationId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.ProductCategoryId).IsRequired();
        builder.HasIndex(p => p.ProductCategoryId);
        builder.HasOne<ProductCategory>().WithMany().HasForeignKey(p => p.ProductCategoryId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.TaxCategoryId).IsRequired();
        builder.HasIndex(p => p.TaxCategoryId);
        builder.HasOne<TaxCategory>().WithMany().HasForeignKey(p => p.TaxCategoryId).OnDelete(DeleteBehavior.Restrict);

        // Self-referencing archive-and-replace link (OI-0007) — set only on the archived row,
        // pointing at its replacement. No navigation property needed by any current caller.
        builder.HasIndex(p => p.SupersededByProductId);
        builder.HasOne<Product>().WithMany().HasForeignKey(p => p.SupersededByProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
