using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class PaymentLedgerEntryConfiguration : IEntityTypeConfiguration<PaymentLedgerEntry>
{
    public void Configure(EntityTypeBuilder<PaymentLedgerEntry> builder)
    {
        builder.ToTable("payment_ledger_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(e => e.OccurredAtUtc).IsRequired();
        builder.Property(e => e.Metadata).HasColumnType("jsonb");

        builder.Property(e => e.TenantId).IsRequired();
        builder.HasIndex(e => e.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.PaymentId).IsRequired();
        builder.HasIndex(e => e.PaymentId);
        builder.HasOne<Payment>().WithMany().HasForeignKey(e => e.PaymentId).OnDelete(DeleteBehavior.Restrict);
    }
}
