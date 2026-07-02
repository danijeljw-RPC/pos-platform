using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        builder.ToTable("staff_members");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StaffCode).IsRequired().HasMaxLength(20);

        // StaffCode is unique per organisation, not globally — the same code may exist under
        // different organisations. Stored uppercase-normalised (StaffCodePolicy).
        builder.HasIndex(s => new { s.OrganisationId, s.StaffCode }).IsUnique();

        builder.Property(s => s.DisplayName).IsRequired().HasMaxLength(200);

        builder.Property(s => s.PinHash).IsRequired().HasMaxLength(500);

        builder.Property(s => s.IsActive).IsRequired();

        builder.Property(s => s.FailedPinAttempts).IsRequired();

        builder.Property(s => s.CreatedAtUtc).IsRequired();

        builder.Property(s => s.TenantId).IsRequired();
        builder.HasIndex(s => s.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.OrganisationId).IsRequired();
        builder.HasOne<Organisation>().WithMany().HasForeignKey(s => s.OrganisationId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.LocationId).IsRequired();
        builder.HasIndex(s => s.LocationId);
        builder.HasOne<Location>().WithMany().HasForeignKey(s => s.LocationId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>().WithMany().HasForeignKey(s => s.LinkedUserId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
