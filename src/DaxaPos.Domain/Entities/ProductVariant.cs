namespace DaxaPos.Domain.Entities;

/// <summary>
/// <see cref="PriceDelta"/> is a delta on the resolved base price (<c>+</c>/<c>-</c>), not an
/// absolute price — a $0.50 large-size upcharge is <c>+0.50</c>, a discount variant could
/// legitimately be negative (PLAN-0004 Milestone E Domain Assumptions). Unlike <see cref="Product"/>,
/// this entity carries no <see cref="OrganisationId"/> column of its own — it is scoped entirely
/// through <see cref="ProductId"/>, matching the <see cref="Terminal"/>-derives-through-<see cref="Location"/>
/// precedent.
/// </summary>
public class ProductVariant
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal PriceDelta { get; set; }

    public string? Sku { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
