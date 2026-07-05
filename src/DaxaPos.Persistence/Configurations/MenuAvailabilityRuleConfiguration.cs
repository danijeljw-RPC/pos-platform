using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class MenuAvailabilityRuleConfiguration : IEntityTypeConfiguration<MenuAvailabilityRule>
{
    public void Configure(EntityTypeBuilder<MenuAvailabilityRule> builder)
    {
        builder.ToTable("menu_availability_rules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.DaysOfWeekMask).IsRequired();
        builder.Property(r => r.StartTimeLocal).IsRequired();
        builder.Property(r => r.EndTimeLocal).IsRequired();
        builder.Property(r => r.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(r => r.CreatedAtUtc).IsRequired();

        builder.Property(r => r.TenantId).IsRequired();
        builder.HasIndex(r => r.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.MenuId).IsRequired();
        builder.HasIndex(r => r.MenuId);
        builder.HasOne<Menu>().WithMany().HasForeignKey(r => r.MenuId).OnDelete(DeleteBehavior.Restrict);
    }
}
