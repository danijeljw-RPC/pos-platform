using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class ProductLocationOverrideConfiguration : IEntityTypeConfiguration<ProductLocationOverride>
{
    public void Configure(EntityTypeBuilder<ProductLocationOverride> builder)
    {
        builder.ToTable("product_location_overrides");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.IsAvailable).IsRequired().HasDefaultValue(true);
        builder.Property(o => o.IsSoldOut).IsRequired().HasDefaultValue(false);
        builder.Property(o => o.PriceOverride).HasPrecision(18, 2);
        builder.Property(o => o.CreatedAtUtc).IsRequired();

        builder.Property(o => o.TenantId).IsRequired();
        builder.HasIndex(o => o.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(o => o.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(o => o.LocationId).IsRequired();
        builder.HasOne<Location>().WithMany().HasForeignKey(o => o.LocationId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(o => o.ProductId).IsRequired();
        builder.HasOne<Product>().WithMany().HasForeignKey(o => o.ProductId).OnDelete(DeleteBehavior.Restrict);

        // One override per (Product, Location) pair — which one would even apply otherwise.
        builder.HasIndex(o => new { o.LocationId, o.ProductId }).IsUnique();
    }
}
