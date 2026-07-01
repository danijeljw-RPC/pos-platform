using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EventType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityType).HasMaxLength(100);
        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.Property(a => a.IpAddress).HasMaxLength(45);

        builder.Property(a => a.BeforeValue).HasColumnType("jsonb");
        builder.Property(a => a.AfterValue).HasColumnType("jsonb");

        builder.Property(a => a.OccurredAtUtc).IsRequired();

        builder.Property(a => a.TenantId).IsRequired();
        builder.HasIndex(a => a.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Organisation>().WithMany().HasForeignKey(a => a.OrganisationId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne<Location>().WithMany().HasForeignKey(a => a.LocationId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne<Terminal>().WithMany().HasForeignKey(a => a.TerminalId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne<Device>().WithMany().HasForeignKey(a => a.DeviceId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        // StaffMemberId intentionally has no FK yet — StaffMember doesn't exist until Milestone F.

        builder.HasIndex(a => a.OccurredAtUtc);
    }
}
