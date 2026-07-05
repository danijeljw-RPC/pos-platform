using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class OrderLineTaxConfiguration : IEntityTypeConfiguration<OrderLineTax>
{
    public void Configure(EntityTypeBuilder<OrderLineTax> builder)
    {
        builder.ToTable("order_line_taxes");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TaxNameSnapshot).IsRequired().HasMaxLength(200);
        builder.Property(t => t.RatePercentSnapshot).IsRequired().HasPrecision(9, 4);
        builder.Property(t => t.TaxableAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(t => t.TaxAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(t => t.JurisdictionNameSnapshot).IsRequired().HasMaxLength(200);
        builder.Property(t => t.JurisdictionTypeSnapshot).IsRequired();
        builder.Property(t => t.ReceiptMarkerCodeSnapshot).HasMaxLength(20);
        builder.Property(t => t.ReceiptMarkerLabelSnapshot).HasMaxLength(200);
        builder.Property(t => t.CreatedAtUtc).IsRequired();

        builder.Property(t => t.TenantId).IsRequired();
        builder.HasIndex(t => t.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.OrderLineId).IsRequired();
        builder.HasIndex(t => t.OrderLineId);
        builder.HasOne<OrderLine>().WithMany().HasForeignKey(t => t.OrderLineId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.TaxDefinitionId).IsRequired();
        builder.HasIndex(t => t.TaxDefinitionId);
        builder.HasOne<TaxDefinition>().WithMany().HasForeignKey(t => t.TaxDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}
