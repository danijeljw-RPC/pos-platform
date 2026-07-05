using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class MenuSectionConfiguration : IEntityTypeConfiguration<MenuSection>
{
    public void Configure(EntityTypeBuilder<MenuSection> builder)
    {
        builder.ToTable("menu_sections");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.DisplayOrder).IsRequired();
        builder.Property(s => s.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(s => s.CreatedAtUtc).IsRequired();

        builder.Property(s => s.TenantId).IsRequired();
        builder.HasIndex(s => s.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.MenuId).IsRequired();
        builder.HasIndex(s => s.MenuId);
        builder.HasOne<Menu>().WithMany().HasForeignKey(s => s.MenuId).OnDelete(DeleteBehavior.Restrict);
    }
}
