namespace DaxaPos.Domain.Entities;

/// <summary>
/// A <see cref="Modifier"/> applied to an <see cref="OrderLine"/> (PLAN-0005 Milestone A), snapshot
/// by value at add-line time (<see cref="NameSnapshot"/>/<see cref="PriceDeltaSnapshot"/>) for the
/// same ADR-0010/ADR-0011 immutability reason as <see cref="OrderLine"/>'s own snapshots. No
/// <see cref="OrderLineModifier"/>-level <c>OrganisationId</c> — scoped entirely through
/// <see cref="OrderLineId"/>, matching <see cref="OrderLine"/>'s own pattern.
/// </summary>
public class OrderLineModifier
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OrderLineId { get; set; }

    public Guid ModifierId { get; set; }

    public string NameSnapshot { get; set; } = string.Empty;

    public decimal PriceDeltaSnapshot { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
