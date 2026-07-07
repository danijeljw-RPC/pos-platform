using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class TerminalConfiguration : IEntityTypeConfiguration<Terminal>
{
    public void Configure(EntityTypeBuilder<Terminal> builder)
    {
        builder.ToTable("terminals");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Lifecycle state (PLAN-0003 Milestone D) — not part of the tenant-isolation query filter;
        // see OrganisationConfiguration for why the two concerns are kept separate.
        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(t => t.LocationId);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(t => t.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Denormalized for tenant-isolation query filters (ADR-0015).
        builder.Property(t => t.TenantId).IsRequired();

        builder.HasIndex(t => t.TenantId);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // A terminal may optionally be associated with a device; a device is not always a terminal.
        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(t => t.DeviceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Milestone C.2: a device may be assigned to at most one terminal, enforced at the database
        // level (not only by TerminalEndpoints.AssignDeviceAsync's application-level 409 check,
        // which is a check-then-act race under concurrent requests). A plain unique index on a
        // nullable column would still reject a second NULL against a first NULL in most databases,
        // but Postgres unique indexes already treat NULL as distinct from NULL (never conflicting)
        // — the explicit partial filter here documents that behaviour rather than relying on it
        // silently, and keeps the index itself smaller (only rows with an actual assignment).
        builder.HasIndex(t => t.DeviceId)
            .IsUnique()
            .HasFilter("\"DeviceId\" IS NOT NULL");
    }
}
