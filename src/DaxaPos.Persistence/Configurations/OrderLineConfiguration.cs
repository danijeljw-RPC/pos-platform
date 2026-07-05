using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("order_lines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Quantity).IsRequired();
        builder.Property(l => l.ProductNameSnapshot).IsRequired().HasMaxLength(200);
        builder.Property(l => l.UnitPriceSnapshot).IsRequired().HasPrecision(18, 2);
        builder.Property(l => l.LineSubtotalAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(l => l.LineTotalAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(l => l.TaxCategoryCodeSnapshot).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Notes).HasMaxLength(2000);
        builder.Property(l => l.Status).IsRequired();
        builder.Property(l => l.VoidReason).HasMaxLength(2000);
        builder.Property(l => l.CreatedAtUtc).IsRequired();

        builder.Property(l => l.TenantId).IsRequired();
        builder.HasIndex(l => l.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(l => l.TenantId).OnDelete(DeleteBehavior.Restrict);

        // No OrganisationId column of its own — scoped entirely through OrderId, matching the
        // Terminal-derives-through-Location precedent.
        builder.Property(l => l.OrderId).IsRequired();
        builder.HasIndex(l => l.OrderId);
        builder.HasOne<Order>().WithMany().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.ProductId).IsRequired();
        builder.HasIndex(l => l.ProductId);
        builder.HasOne<Product>().WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.ProductVariantId);
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(l => l.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
    }
}
