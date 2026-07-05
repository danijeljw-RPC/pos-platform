using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class MenuSectionItemConfiguration : IEntityTypeConfiguration<MenuSectionItem>
{
    public void Configure(EntityTypeBuilder<MenuSectionItem> builder)
    {
        builder.ToTable("menu_section_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.DisplayOrder).IsRequired();
        builder.Property(i => i.CreatedAtUtc).IsRequired();

        builder.Property(i => i.TenantId).IsRequired();
        builder.HasIndex(i => i.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(i => i.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(i => i.MenuSectionId).IsRequired();
        builder.HasIndex(i => i.MenuSectionId);
        builder.HasOne<MenuSection>().WithMany().HasForeignKey(i => i.MenuSectionId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(i => i.ProductId).IsRequired();
        builder.HasIndex(i => i.ProductId);
        builder.HasOne<Product>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
