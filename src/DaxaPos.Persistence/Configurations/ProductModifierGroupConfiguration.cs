using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class ProductModifierGroupConfiguration : IEntityTypeConfiguration<ProductModifierGroup>
{
    public void Configure(EntityTypeBuilder<ProductModifierGroup> builder)
    {
        builder.ToTable("product_modifier_groups");

        builder.HasKey(pmg => pmg.Id);

        builder.Property(pmg => pmg.DisplayOrder).IsRequired();
        builder.Property(pmg => pmg.CreatedAtUtc).IsRequired();

        builder.Property(pmg => pmg.TenantId).IsRequired();
        builder.HasIndex(pmg => pmg.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(pmg => pmg.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(pmg => pmg.ProductId).IsRequired();
        builder.HasIndex(pmg => pmg.ProductId);
        builder.HasOne<Product>().WithMany().HasForeignKey(pmg => pmg.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(pmg => pmg.ModifierGroupId).IsRequired();
        builder.HasIndex(pmg => pmg.ModifierGroupId);
        builder.HasOne<ModifierGroup>().WithMany().HasForeignKey(pmg => pmg.ModifierGroupId).OnDelete(DeleteBehavior.Restrict);
    }
}
