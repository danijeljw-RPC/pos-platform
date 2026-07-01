using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.HasOne<User>().WithMany().HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Role>().WithMany().HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Organisation>().WithMany().HasForeignKey(ur => ur.OrganisationId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.HasOne<Location>().WithMany().HasForeignKey(ur => ur.LocationId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        builder.Property(ur => ur.TenantId).IsRequired();
        builder.HasIndex(ur => ur.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(ur => ur.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
