using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Lifecycle state (PLAN-0003 Milestone D) — not part of the tenant-isolation query filter;
        // see OrganisationConfiguration for why the two concerns are kept separate.
        builder.Property(l => l.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(l => l.CreatedAtUtc)
            .IsRequired();

        // PLAN-0004 Milestone G — added to the existing PLAN-0003 entity because
        // MenuAvailabilityRule evaluation requires the location's own local time.
        builder.Property(l => l.TimeZoneId)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("UTC");

        builder.HasIndex(l => l.OrganisationId);

        builder.HasOne<Organisation>()
            .WithMany()
            .HasForeignKey(l => l.OrganisationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Denormalized for tenant-isolation query filters (ADR-0015) — not derived via a join to
        // Organisation, so DaxaDbContext's global query filter stays a single indexed comparison.
        builder.Property(l => l.TenantId).IsRequired();

        builder.HasIndex(l => l.TenantId);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(l => l.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
