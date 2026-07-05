namespace DaxaPos.Domain.Entities;

/// <summary>
/// A per-<see cref="Location"/> override of a <see cref="Product"/>'s availability/price
/// (PLAN-0004 Milestone F, ADR-0003's default-and-override model). Absence of a row for a given
/// <see cref="ProductId"/>/<see cref="LocationId"/> pair means "use the organisation-wide
/// <see cref="Product"/> defaults" — not a special-cased single-location code path. No
/// <c>OrganisationId</c> column of its own — derived via <see cref="LocationId"/>, matching
/// <see cref="Terminal"/>'s precedent. <see cref="PriceOverride"/>, when set, replaces the
/// resolved base+variant+modifier total outright (see <c>DaxaPos.Application.Pricing.PriceResolver</c>) —
/// it does not add to it.
/// </summary>
public class ProductLocationOverride
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid LocationId { get; set; }

    public Guid ProductId { get; set; }

    public bool IsAvailable { get; set; } = true;

    public bool IsSoldOut { get; set; }

    public decimal? PriceOverride { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
