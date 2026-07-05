using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class ModifierConfiguration : IEntityTypeConfiguration<Modifier>
{
    public void Configure(EntityTypeBuilder<Modifier> builder)
    {
        builder.ToTable("modifiers");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.PriceDelta).IsRequired().HasPrecision(18, 2);
        builder.Property(m => m.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(m => m.CreatedAtUtc).IsRequired();

        builder.Property(m => m.TenantId).IsRequired();
        builder.HasIndex(m => m.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.ModifierGroupId).IsRequired();
        builder.HasIndex(m => m.ModifierGroupId);
        builder.HasOne<ModifierGroup>().WithMany().HasForeignKey(m => m.ModifierGroupId).OnDelete(DeleteBehavior.Restrict);
    }
}
