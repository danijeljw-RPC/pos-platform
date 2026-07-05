using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("menus");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(m => m.CreatedAtUtc).IsRequired();

        builder.Property(m => m.TenantId).IsRequired();
        builder.HasIndex(m => m.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.OrganisationId).IsRequired();
        builder.HasIndex(m => m.OrganisationId);
        builder.HasOne<Organisation>().WithMany().HasForeignKey(m => m.OrganisationId).OnDelete(DeleteBehavior.Restrict);

        // Null = organisation-wide (no Location row to reference), matching TaxCategoryDefinition's pattern.
        builder.HasIndex(m => m.LocationId);
        builder.HasOne<Location>().WithMany().HasForeignKey(m => m.LocationId).OnDelete(DeleteBehavior.Restrict);
    }
}
