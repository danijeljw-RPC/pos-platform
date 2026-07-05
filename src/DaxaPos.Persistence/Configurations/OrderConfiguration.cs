using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber).IsRequired();
        builder.Property(o => o.Status).IsRequired();
        builder.Property(o => o.OpenedAtUtc).IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(2000);
        builder.Property(o => o.IsTaxInclusivePricing).IsRequired();
        builder.Property(o => o.SubtotalAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.TotalTaxAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.GrandTotalAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.CreatedAtUtc).IsRequired();

        builder.Property(o => o.TenantId).IsRequired();
        builder.HasIndex(o => o.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(o => o.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(o => o.OrganisationId).IsRequired();
        builder.HasIndex(o => o.OrganisationId);
        builder.HasOne<Organisation>().WithMany().HasForeignKey(o => o.OrganisationId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(o => o.LocationId).IsRequired();
        builder.HasIndex(o => o.LocationId);
        builder.HasOne<Location>().WithMany().HasForeignKey(o => o.LocationId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(o => o.TerminalId).IsRequired();
        builder.HasIndex(o => o.TerminalId);
        builder.HasOne<Terminal>().WithMany().HasForeignKey(o => o.TerminalId).OnDelete(DeleteBehavior.Restrict);

        // Location-scoped display sequence (approved Human Decision #2) — not globally unique, only
        // unique per location, matching how staff read/quote order numbers at the counter.
        builder.HasIndex(o => new { o.LocationId, o.OrderNumber }).IsUnique();
    }
}
