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

        // Seed mapping per the accepted PLAN-0003 permission catalogue, extended by PLAN-0004
        // Milestone A's four new codes and PLAN-0005 Milestones A/B/C/D's `orders.manage`/
        // `payments.record`/`payments.refund`/`receipts.reprint`. `Staff` was previously granted none
        // of these — staff PIN login must never reach identity/tenancy management — but now receives
        // catalog.sold-out-toggle, orders.manage, payments.record, and receipts.reprint, all
        // Operational-category permissions (OI-0015). `payments.refund` is AdminSensitive (approved
        // Human Decision #4, manager/admin-only by default) and is deliberately absent from
        // `staffPermissionIds` below.
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
            RbacSeedIds.CatalogManagePermissionId,
            RbacSeedIds.PricingManagePermissionId,
            RbacSeedIds.MenusManagePermissionId,
            RbacSeedIds.CatalogSoldOutTogglePermissionId,
            RbacSeedIds.OrdersManagePermissionId,
            RbacSeedIds.PaymentsRecordPermissionId,
            RbacSeedIds.PaymentsRefundPermissionId,
            RbacSeedIds.ReceiptsReprintPermissionId,
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
            RbacSeedIds.CatalogManagePermissionId,
            RbacSeedIds.PricingManagePermissionId,
            RbacSeedIds.MenusManagePermissionId,
            RbacSeedIds.CatalogSoldOutTogglePermissionId,
            RbacSeedIds.OrdersManagePermissionId,
            RbacSeedIds.PaymentsRecordPermissionId,
            RbacSeedIds.PaymentsRefundPermissionId,
            RbacSeedIds.ReceiptsReprintPermissionId,
        };

        var venueManagerPermissionIds = new[]
        {
            RbacSeedIds.TerminalsManagePermissionId,
            RbacSeedIds.DevicesManagePermissionId,
            RbacSeedIds.DevicesRegisterPermissionId,
            RbacSeedIds.StaffManagePermissionId,
            RbacSeedIds.CatalogManagePermissionId,
            RbacSeedIds.PricingManagePermissionId,
            RbacSeedIds.MenusManagePermissionId,
            RbacSeedIds.CatalogSoldOutTogglePermissionId,
            RbacSeedIds.OrdersManagePermissionId,
            RbacSeedIds.PaymentsRecordPermissionId,
            RbacSeedIds.PaymentsRefundPermissionId,
            RbacSeedIds.ReceiptsReprintPermissionId,
        };

        var supportAccessPermissionIds = new[]
        {
            RbacSeedIds.DevicesManagePermissionId,
            RbacSeedIds.SessionsManagePermissionId,
        };

        // `Staff` role grants (PLAN-0004 Milestone A, PLAN-0005 Milestones A/B/D): Operational only,
        // per OI-0015. `receipts.reprint` joins this list per approved Human Decision #5 — reprinting
        // a receipt is routine counter work, not a manager-only override like `payments.refund`.
        var staffPermissionIds = new[]
        {
            RbacSeedIds.CatalogSoldOutTogglePermissionId,
            RbacSeedIds.OrdersManagePermissionId,
            RbacSeedIds.PaymentsRecordPermissionId,
            RbacSeedIds.ReceiptsReprintPermissionId,
        };

        var seedRows = allPermissionIds
            .Select(permissionId => new RolePermission { RoleId = RbacSeedIds.SystemAdminRoleId, PermissionId = permissionId })
            .Concat(organisationOwnerPermissionIds.Select(permissionId => new RolePermission { RoleId = RbacSeedIds.OrganisationOwnerRoleId, PermissionId = permissionId }))
            .Concat(venueManagerPermissionIds.Select(permissionId => new RolePermission { RoleId = RbacSeedIds.VenueManagerRoleId, PermissionId = permissionId }))
            .Concat(supportAccessPermissionIds.Select(permissionId => new RolePermission { RoleId = RbacSeedIds.SupportAccessRoleId, PermissionId = permissionId }))
            .Concat(staffPermissionIds.Select(permissionId => new RolePermission { RoleId = RbacSeedIds.StaffRoleId, PermissionId = permissionId }))
            .ToArray();

        builder.HasData(seedRows);
    }
}
