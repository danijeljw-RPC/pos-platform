using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("refunds");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(r => r.ReasonCode).IsRequired().HasMaxLength(100);
        builder.Property(r => r.ReasonNote).HasMaxLength(1000);
        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.RecordedAtUtc).IsRequired();
        builder.Property(r => r.ProviderReference).HasMaxLength(200);

        builder.Property(r => r.TenantId).IsRequired();
        builder.HasIndex(r => r.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);

        // No OrganisationId/LocationId column of its own — scoped entirely through PaymentId/
        // OrderId, matching Payment's own precedent of deriving organisation/location context
        // through its parent rather than denormalizing every ancestor column.
        builder.Property(r => r.PaymentId).IsRequired();
        builder.HasIndex(r => r.PaymentId);
        builder.HasOne<Payment>().WithMany().HasForeignKey(r => r.PaymentId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.OrderId).IsRequired();
        builder.HasIndex(r => r.OrderId);
        builder.HasOne<Order>().WithMany().HasForeignKey(r => r.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
