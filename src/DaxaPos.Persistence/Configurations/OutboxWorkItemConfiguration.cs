using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class OutboxWorkItemConfiguration : IEntityTypeConfiguration<OutboxWorkItem>
{
    public void Configure(EntityTypeBuilder<OutboxWorkItem> builder)
    {
        builder.ToTable("outbox_work_items");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.WorkType).IsRequired().HasMaxLength(100);
        builder.Property(w => w.PayloadJson).IsRequired().HasColumnType("jsonb");
        builder.Property(w => w.Status).IsRequired();
        builder.Property(w => w.AttemptCount).IsRequired();
        builder.Property(w => w.MaxAttempts).IsRequired();
        builder.Property(w => w.LastError).HasMaxLength(2000);
        builder.Property(w => w.CreatedAtUtc).IsRequired();
        builder.Property(w => w.NextAttemptAtUtc).IsRequired();

        builder.Property(w => w.TenantId).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(w => w.TenantId).OnDelete(DeleteBehavior.Restrict);

        // DaxaPos.Workers polls across all tenants (it has no single tenant context of its own),
        // so the poll query itself bypasses the fail-closed filter below via IgnoreQueryFilters() —
        // a documented, approved bootstrap-style exception (see IgnoreQueryFiltersUsageTests).
        builder.HasIndex(w => new { w.Status, w.NextAttemptAtUtc });
        builder.HasIndex(w => w.TenantId);
    }
}
