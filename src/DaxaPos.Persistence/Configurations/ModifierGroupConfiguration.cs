using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class ModifierGroupConfiguration : IEntityTypeConfiguration<ModifierGroup>
{
    public void Configure(EntityTypeBuilder<ModifierGroup> builder)
    {
        builder.ToTable("modifier_groups");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).IsRequired().HasMaxLength(200);
        builder.Property(g => g.SelectionMin).IsRequired();
        builder.Property(g => g.SelectionMax).IsRequired();
        builder.Property(g => g.IsRequired).IsRequired();
        builder.Property(g => g.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(g => g.CreatedAtUtc).IsRequired();

        builder.Property(g => g.TenantId).IsRequired();
        builder.HasIndex(g => g.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(g => g.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(g => g.OrganisationId).IsRequired();
        builder.HasIndex(g => g.OrganisationId);
        builder.HasOne<Organisation>().WithMany().HasForeignKey(g => g.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }
}
