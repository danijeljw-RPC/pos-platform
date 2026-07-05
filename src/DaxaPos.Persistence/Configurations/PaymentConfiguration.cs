using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Method).IsRequired();
        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.AmountRequested).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.AmountApproved).HasPrecision(18, 2);
        builder.Property(p => p.RecordedAtUtc).IsRequired();
        builder.Property(p => p.ProviderReference).HasMaxLength(200);

        builder.Property(p => p.TenantId).IsRequired();
        builder.HasIndex(p => p.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);

        // No OrganisationId column of its own — scoped entirely through OrderId, matching
        // OrderLine's precedent from Milestone A.
        builder.Property(p => p.OrderId).IsRequired();
        builder.HasIndex(p => p.OrderId);
        builder.HasOne<Order>().WithMany().HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.LocationId).IsRequired();
        builder.HasIndex(p => p.LocationId);
        builder.HasOne<Location>().WithMany().HasForeignKey(p => p.LocationId).OnDelete(DeleteBehavior.Restrict);

        // Idempotency (ADR-0010, plan's explicit requirement) — globally unique, not scoped per
        // order, since a client-generated idempotency key (e.g. a UUID minted by the POS terminal
        // for one payment attempt) is inherently unique on its own.
        builder.HasIndex(p => p.IdempotencyKey).IsUnique();
    }
}
