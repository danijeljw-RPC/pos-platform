using System.Text.Json;
using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> builder)
    {
        builder.ToTable("auth_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.AuthMethod).IsRequired().HasConversion<string>().HasMaxLength(50);

        var stringListComparer = new ValueComparer<List<string>>(
            (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
            v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            v => v.ToList());

        builder.Property(s => s.RoleSnapshot)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(stringListComparer);

        builder.Property(s => s.PermissionSnapshot)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(stringListComparer);

        builder.Property(s => s.SessionTokenHash).IsRequired().HasMaxLength(200);
        builder.HasIndex(s => s.SessionTokenHash).IsUnique();

        builder.Property(s => s.RevokedReason).HasMaxLength(200);

        builder.Property(s => s.IssuedAtUtc).IsRequired();
        builder.Property(s => s.ExpiresAtUtc).IsRequired();
        builder.Property(s => s.LastActivityAtUtc).IsRequired();

        builder.Property(s => s.TenantId).IsRequired();
        builder.HasIndex(s => s.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Organisation>().WithMany().HasForeignKey(s => s.OrganisationId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne<Location>().WithMany().HasForeignKey(s => s.LocationId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne<Terminal>().WithMany().HasForeignKey(s => s.TerminalId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne<Device>().WithMany().HasForeignKey(s => s.DeviceId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        // FK added in Milestone F's AddStaffMembers migration, once the referenced table existed.
        builder.HasOne<StaffMember>().WithMany().HasForeignKey(s => s.StaffMemberId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
