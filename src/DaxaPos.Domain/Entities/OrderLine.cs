using DaxaPos.Domain.Enums;

namespace DaxaPos.Domain.Entities;

/// <summary>
/// One line on an <see cref="Order"/> (PLAN-0005 Milestone A). Snapshots
/// <see cref="ProductNameSnapshot"/>/<see cref="UnitPriceSnapshot"/>/<see cref="TaxCategoryCodeSnapshot"/>
/// at add-line time rather than joining live to <see cref="Product"/>/<see cref="TaxCategory"/> at
/// read/receipt time — required by ADR-0010 (immutable financial source records) and ADR-0011
/// (a historical receipt preserves the marker meaning in effect at time of sale). No
/// <see cref="OrganisationId"/> column of its own — scoped entirely through <see cref="OrderId"/>,
/// matching the <see cref="Terminal"/>-derives-through-<see cref="Location"/> precedent. Voided
/// (never hard-deleted) per ADR-0010 — <see cref="VoidedAtUtc"/>/<see cref="VoidReason"/> record the
/// reversal.
/// </summary>
public class OrderLine
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public Guid? ProductVariantId { get; set; }

    public int Quantity { get; set; }

    public string ProductNameSnapshot { get; set; } = string.Empty;

    public decimal UnitPriceSnapshot { get; set; }

    public decimal LineSubtotalAmount { get; set; }

    public decimal LineTotalAmount { get; set; }

    public string TaxCategoryCodeSnapshot { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public OrderLineStatus Status { get; set; } = OrderLineStatus.Active;

    public DateTimeOffset? VoidedAtUtc { get; set; }

    public Guid? VoidedByUserId { get; set; }

    public Guid? VoidedByStaffMemberId { get; set; }

    public string? VoidReason { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
