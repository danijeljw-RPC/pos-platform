using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(320);

        // Email is globally unique (not per-tenant): login resolves the user — and therefore the
        // tenant — from the email alone, before any tenant context exists.
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);

        builder.Property(u => u.PasswordHash).HasMaxLength(500);

        builder.Property(u => u.ExternalIdentityProvider).HasMaxLength(100);

        builder.Property(u => u.ExternalSubjectId).HasMaxLength(200);

        builder.Property(u => u.IsActive).IsRequired();

        builder.Property(u => u.FailedLoginCount).IsRequired();

        builder.Property(u => u.CreatedAtUtc).IsRequired();

        builder.Property(u => u.TenantId).IsRequired();
        builder.HasIndex(u => u.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(u => u.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(u => u.OrganisationId).IsRequired();
        builder.HasIndex(u => u.OrganisationId);
        builder.HasOne<Organisation>().WithMany().HasForeignKey(u => u.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }
}
