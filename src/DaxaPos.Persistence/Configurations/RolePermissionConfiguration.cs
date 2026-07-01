using DaxaPos.Domain.Entities;
using DaxaPos.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed mapping per the accepted PLAN-0003 permission catalogue. `Staff` is deliberately
        // granted none of these — staff PIN login must never reach identity/tenancy management.
        var allPermissionIds = new[]
        {
            RbacSeedIds.OrganisationsManagePermissionId,
            RbacSeedIds.LocationsManagePermissionId,
            RbacSeedIds.TerminalsManagePermissionId,
            RbacSeedIds.DevicesManagePermissionId,
            RbacSeedIds.DevicesRegisterPermissionId,
            RbacSeedIds.StaffManagePermissionId,
            RbacSeedIds.UsersManagePermissionId,
            RbacSeedIds.SessionsManagePermissionId,
        };

        var organisationOwnerPermissionIds = new[]
        {
            RbacSeedIds.LocationsManagePermissionId,
            RbacSeedIds.TerminalsManagePermissionId,
            RbacSeedIds.DevicesManagePermissionId,
            RbacSeedIds.DevicesRegisterPermissionId,
            RbacSeedIds.StaffManagePermissionId,
            RbacSeedIds.UsersManagePermissionId,
            RbacSeedIds.SessionsManagePermissionId,
        };

        var venueManagerPermissionIds = new[]
        {
            RbacSeedIds.TerminalsManagePermissionId,
            RbacSeedIds.DevicesManagePermissionId,
            RbacSeedIds.DevicesRegisterPermissionId,
            RbacSeedIds.StaffManagePermissionId,
        };

        var supportAccessPermissionIds = new[]
        {
            RbacSeedIds.DevicesManagePermissionId,
            RbacSeedIds.SessionsManagePermissionId,
        };

        var seedRows = allPermissionIds
            .Select(permissionId => new RolePermission { RoleId = RbacSeedIds.SystemAdminRoleId, PermissionId = permissionId })
            .Concat(organisationOwnerPermissionIds.Select(permissionId => new RolePermission { RoleId = RbacSeedIds.OrganisationOwnerRoleId, PermissionId = permissionId }))
            .Concat(venueManagerPermissionIds.Select(permissionId => new RolePermission { RoleId = RbacSeedIds.VenueManagerRoleId, PermissionId = permissionId }))
            .Concat(supportAccessPermissionIds.Select(permissionId => new RolePermission { RoleId = RbacSeedIds.SupportAccessRoleId, PermissionId = permissionId }))
            .ToArray();

        builder.HasData(seedRows);
    }
}
