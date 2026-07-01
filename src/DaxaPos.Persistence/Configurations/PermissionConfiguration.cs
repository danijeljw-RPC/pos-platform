using DaxaPos.Domain.Entities;
using DaxaPos.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.Description).HasMaxLength(500);

        builder.HasData(
            new Permission { Id = RbacSeedIds.OrganisationsManagePermissionId, Code = "organisations.manage", Description = "Create/update organisations." },
            new Permission { Id = RbacSeedIds.LocationsManagePermissionId, Code = "locations.manage", Description = "Create/update locations within an organisation." },
            new Permission { Id = RbacSeedIds.TerminalsManagePermissionId, Code = "terminals.manage", Description = "Create/update terminals within a location." },
            new Permission { Id = RbacSeedIds.DevicesManagePermissionId, Code = "devices.manage", Description = "Rotate/revoke device credentials, list devices." },
            new Permission { Id = RbacSeedIds.DevicesRegisterPermissionId, Code = "devices.register", Description = "Generate/rotate device registration PINs." },
            new Permission { Id = RbacSeedIds.StaffManagePermissionId, Code = "staff.manage", Description = "Create staff members, reset PIN, assign roles, disable staff." },
            new Permission { Id = RbacSeedIds.UsersManagePermissionId, Code = "users.manage", Description = "Create local manager/admin users, assign roles." },
            new Permission { Id = RbacSeedIds.SessionsManagePermissionId, Code = "sessions.manage", Description = "Force-revoke another identity's active session." });
    }
}
