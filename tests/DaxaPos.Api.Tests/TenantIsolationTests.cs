using DaxaPos.Api.Tests.Support;
using DaxaPos.Domain.Entities;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Tests;

public class TenantIsolationTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DaxaDb")
            ?? "Host=localhost;Port=5432;Database=daxapos;Username=daxapos;Password=daxapos_dev_password";

    private static DaxaDbContext CreateContext(Guid? tenantId) =>
        new(
            new DbContextOptionsBuilder<DaxaDbContext>().UseNpgsql(ConnectionString).Options,
            new FakeCurrentTenantProvider(tenantId));

    [Fact]
    public async Task Location_IsVisible_ToItsOwnTenant()
    {
        var tenantId = Guid.NewGuid();
        var (_, locationId) = await SeedOrganisationAndLocationAsync(tenantId);

        await using var context = CreateContext(tenantId);
        var location = await context.Locations.SingleOrDefaultAsync(l => l.Id == locationId);

        Assert.NotNull(location);
    }

    [Fact]
    public async Task Location_IsNotVisible_ToADifferentTenant()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var (_, locationId) = await SeedOrganisationAndLocationAsync(tenantId);

        await using var context = CreateContext(otherTenantId);
        var location = await context.Locations.SingleOrDefaultAsync(l => l.Id == locationId);

        Assert.Null(location);
    }

    [Fact]
    public async Task Location_IsNotVisible_WhenTenantContextIsMissing()
    {
        var tenantId = Guid.NewGuid();
        var (_, locationId) = await SeedOrganisationAndLocationAsync(tenantId);

        await using var context = CreateContext(null);
        var location = await context.Locations.SingleOrDefaultAsync(l => l.Id == locationId);

        Assert.Null(location);
    }

    [Fact]
    public async Task Organisation_IsNotVisible_ToADifferentTenant()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var (organisationId, _) = await SeedOrganisationAndLocationAsync(tenantId);

        await using var context = CreateContext(otherTenantId);
        var organisation = await context.Organisations.SingleOrDefaultAsync(o => o.Id == organisationId);

        Assert.Null(organisation);
    }

    [Fact]
    public async Task Organisation_IsNotVisible_WhenTenantContextIsMissing()
    {
        var tenantId = Guid.NewGuid();
        var (organisationId, _) = await SeedOrganisationAndLocationAsync(tenantId);

        await using var context = CreateContext(null);
        var organisation = await context.Organisations.SingleOrDefaultAsync(o => o.Id == organisationId);

        Assert.Null(organisation);
    }

    [Fact]
    public async Task LocationList_OnlyContainsCallersTenant_WhenOtherTenantsHaveData()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var (_, locationAId) = await SeedOrganisationAndLocationAsync(tenantA);
        await SeedOrganisationAndLocationAsync(tenantB);

        await using var context = CreateContext(tenantA);
        var locations = await context.Locations.Where(l => l.Id == locationAId || l.TenantId == tenantB).ToListAsync();

        var location = Assert.Single(locations);
        Assert.Equal(tenantA, location.TenantId);
    }

    private static async Task<(Guid OrganisationId, Guid LocationId)> SeedOrganisationAndLocationAsync(Guid tenantId)
    {
        await using var context = CreateContext(tenantId);

        var tenant = new Tenant { Id = tenantId, Name = $"Tenant {tenantId}", CreatedAtUtc = DateTimeOffset.UtcNow };
        var organisation = new Organisation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Org",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        var location = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrganisationId = organisation.Id,
            Name = "Location",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        context.Tenants.Add(tenant);
        context.Organisations.Add(organisation);
        context.Locations.Add(location);
        await context.SaveChangesAsync();

        return (organisation.Id, location.Id);
    }
}
