using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name).IsRequired().HasMaxLength(200);
        builder.Property(v => v.PriceDelta).IsRequired().HasPrecision(18, 2);
        builder.Property(v => v.Sku).HasMaxLength(100);
        builder.Property(v => v.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(v => v.CreatedAtUtc).IsRequired();

        builder.Property(v => v.TenantId).IsRequired();
        builder.HasIndex(v => v.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(v => v.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(v => v.ProductId).IsRequired();
        builder.HasIndex(v => v.ProductId);
        builder.HasOne<Product>().WithMany().HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
