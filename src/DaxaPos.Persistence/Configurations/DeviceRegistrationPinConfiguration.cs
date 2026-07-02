using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class DeviceRegistrationPinConfiguration : IEntityTypeConfiguration<DeviceRegistrationPin>
{
    public void Configure(EntityTypeBuilder<DeviceRegistrationPin> builder)
    {
        builder.ToTable("device_registration_pins");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PinHash)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.ExpiresAtUtc).IsRequired();
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        // The pre-auth registration endpoint scans recent candidate rows by creation time (see
        // DeviceRegistrationEndpoints) — indexed so that scan stays bounded and cheap.
        builder.HasIndex(p => p.CreatedAtUtc);

        builder.HasIndex(p => p.LocationId);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(p => p.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Organisation>()
            .WithMany()
            .HasForeignKey(p => p.OrganisationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Denormalized for tenant-isolation query filters (ADR-0015).
        builder.Property(p => p.TenantId).IsRequired();

        builder.HasIndex(p => p.TenantId);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
