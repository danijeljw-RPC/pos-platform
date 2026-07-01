using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(l => l.OrganisationId);

        builder.HasOne<Organisation>()
            .WithMany()
            .HasForeignKey(l => l.OrganisationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
