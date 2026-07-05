using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class OrderLineModifierConfiguration : IEntityTypeConfiguration<OrderLineModifier>
{
    public void Configure(EntityTypeBuilder<OrderLineModifier> builder)
    {
        builder.ToTable("order_line_modifiers");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.NameSnapshot).IsRequired().HasMaxLength(200);
        builder.Property(m => m.PriceDeltaSnapshot).IsRequired().HasPrecision(18, 2);
        builder.Property(m => m.CreatedAtUtc).IsRequired();

        builder.Property(m => m.TenantId).IsRequired();
        builder.HasIndex(m => m.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.OrderLineId).IsRequired();
        builder.HasIndex(m => m.OrderLineId);
        builder.HasOne<OrderLine>().WithMany().HasForeignKey(m => m.OrderLineId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.ModifierId).IsRequired();
        builder.HasIndex(m => m.ModifierId);
        builder.HasOne<Modifier>().WithMany().HasForeignKey(m => m.ModifierId).OnDelete(DeleteBehavior.Restrict);
    }
}
