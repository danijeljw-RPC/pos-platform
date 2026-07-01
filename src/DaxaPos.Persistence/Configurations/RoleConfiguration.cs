using DaxaPos.Domain.Entities;
using DaxaPos.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(r => r.Name).IsUnique();

        builder.Property(r => r.Description).HasMaxLength(500);

        builder.HasData(
            new Role { Id = RbacSeedIds.SystemAdminRoleId, Name = "SystemAdmin", Description = "Full access across all tenants and functions.", IsSystemDefined = true },
            new Role { Id = RbacSeedIds.OrganisationOwnerRoleId, Name = "OrganisationOwner", Description = "Full access within their own organisation.", IsSystemDefined = true },
            new Role { Id = RbacSeedIds.VenueManagerRoleId, Name = "VenueManager", Description = "Store-level management within an assigned location.", IsSystemDefined = true },
            new Role { Id = RbacSeedIds.StaffRoleId, Name = "Staff", Description = "Operational POS use only (staff PIN login).", IsSystemDefined = true },
            new Role { Id = RbacSeedIds.SupportAccessRoleId, Name = "SupportAccess", Description = "Limited, audited support access.", IsSystemDefined = true });
    }
}
