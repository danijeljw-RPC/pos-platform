using DaxaPos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DaxaPos.Persistence.Configurations;

public class OrderNumberCounterConfiguration : IEntityTypeConfiguration<OrderNumberCounter>
{
    public void Configure(EntityTypeBuilder<OrderNumberCounter> builder)
    {
        builder.ToTable("order_number_counters");

        // LocationId is the primary key, not a surrogate Id — one row per location, allocated via
        // an atomic upsert-and-increment (see OrderEndpoints.AllocateOrderNumberAsync), never
        // pre-provisioned by a location-create workflow.
        builder.HasKey(c => c.LocationId);

        builder.Property(c => c.NextValue).IsRequired();

        builder.Property(c => c.TenantId).IsRequired();
        builder.HasIndex(c => c.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>().WithMany().HasForeignKey(c => c.LocationId).OnDelete(DeleteBehavior.Restrict);
    }
}
