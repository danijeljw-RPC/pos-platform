using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;
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

        builder.Property(p => p.Category).IsRequired();

        builder.HasData(
            new Permission { Id = RbacSeedIds.OrganisationsManagePermissionId, Code = "organisations.manage", Description = "Create/update organisations.", Category = PermissionCategory.AdminSensitive },
            new Permission { Id = RbacSeedIds.LocationsManagePermissionId, Code = "locations.manage", Description = "Create/update locations within an organisation.", Category = PermissionCategory.AdminSensitive },
            new Permission { Id = RbacSeedIds.TerminalsManagePermissionId, Code = "terminals.manage", Description = "Create/update terminals within a location.", Category = PermissionCategory.AdminSensitive },
            new Permission { Id = RbacSeedIds.DevicesManagePermissionId, Code = "devices.manage", Description = "Rotate/revoke device credentials, list devices.", Category = PermissionCategory.AdminSensitive },
            new Permission { Id = RbacSeedIds.DevicesRegisterPermissionId, Code = "devices.register", Description = "Generate/rotate device registration PINs.", Category = PermissionCategory.AdminSensitive },
            new Permission { Id = RbacSeedIds.StaffManagePermissionId, Code = "staff.manage", Description = "Create staff members, reset PIN, assign roles, disable staff.", Category = PermissionCategory.AdminSensitive },
            new Permission { Id = RbacSeedIds.UsersManagePermissionId, Code = "users.manage", Description = "Create local manager/admin users, assign roles.", Category = PermissionCategory.AdminSensitive },
            new Permission { Id = RbacSeedIds.SessionsManagePermissionId, Code = "sessions.manage", Description = "Force-revoke another identity's active session.", Category = PermissionCategory.AdminSensitive },
            new Permission { Id = RbacSeedIds.CatalogManagePermissionId, Code = "catalog.manage", Description = "Create/update product categories, products, variants, and modifiers; assign product tax categories (OI-0007).", Category = PermissionCategory.AdminSensitive },
            new Permission { Id = RbacSeedIds.PricingManagePermissionId, Code = "pricing.manage", Description = "Create/update location price overrides and venue tax configuration.", Category = PermissionCategory.AdminSensitive },
            new Permission { Id = RbacSeedIds.MenusManagePermissionId, Code = "menus.manage", Description = "Create/update menus, sections, and availability rules.", Category = PermissionCategory.AdminSensitive },
            new Permission { Id = RbacSeedIds.CatalogSoldOutTogglePermissionId, Code = "catalog.sold-out-toggle", Description = "Toggle a product's sold-out state at a location.", Category = PermissionCategory.Operational },
            new Permission { Id = RbacSeedIds.OrdersManagePermissionId, Code = "orders.manage", Description = "Open orders, add/void lines, hold/resume/void/cancel orders.", Category = PermissionCategory.Operational });
    }
}
