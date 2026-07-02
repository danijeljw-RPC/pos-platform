using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class StaffMemberRoleConfiguration : IEntityTypeConfiguration<StaffMemberRole>
{
    public void Configure(EntityTypeBuilder<StaffMemberRole> builder)
    {
        builder.ToTable("staff_member_roles");

        builder.HasKey(sr => new { sr.StaffMemberId, sr.RoleId });

        builder.HasOne<StaffMember>().WithMany().HasForeignKey(sr => sr.StaffMemberId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Role>().WithMany().HasForeignKey(sr => sr.RoleId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Location>().WithMany().HasForeignKey(sr => sr.LocationId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        builder.Property(sr => sr.TenantId).IsRequired();
        builder.HasIndex(sr => sr.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(sr => sr.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
