using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class DeviceCredentialConfiguration : IEntityTypeConfiguration<DeviceCredential>
{
    public void Configure(EntityTypeBuilder<DeviceCredential> builder)
    {
        builder.ToTable("device_credentials");

        builder.HasKey(c => c.Id);

        // Salted HMAC hash of the credential secret — the salt is embedded in the hash string
        // ({salt}.{hash}), so there is deliberately no separate Salt column (ADR-0015 §3).
        builder.Property(c => c.CredentialHash)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.IssuedAtUtc).IsRequired();

        builder.HasIndex(c => c.DeviceId);

        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(c => c.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Denormalized for tenant-isolation query filters (ADR-0015).
        builder.Property(c => c.TenantId).IsRequired();

        builder.HasIndex(c => c.TenantId);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
