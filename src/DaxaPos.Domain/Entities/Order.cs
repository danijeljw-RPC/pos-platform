using DaxaPos.Domain.Enums;

namespace DaxaPos.Domain.Entities;

/// <summary>
/// The core commercial transaction (PLAN-0005 Milestone A). Opened by exactly one of
/// <see cref="OpenedByUserId"/>/<see cref="OpenedByStaffMemberId"/> (never both), matching the dual
/// <c>UserId</c>/<c>StaffMemberId</c> pattern PLAN-0004 Milestone F introduced. <see cref="OrderNumber"/>
/// is a location-scoped monotonic sequence (approved Human Decision #2) — allocated by an atomic
/// increment against <see cref="OrderNumberCounter"/>, never a computed <c>MAX + 1</c>.
/// <see cref="IsTaxInclusivePricing"/> is snapshotted from <see cref="VenueTaxConfiguration"/> at
/// open time (fail-closed if missing, matching PLAN-0004's approved Human Decision #5 precedent) —
/// a later change to the venue's configuration must never alter an already-open order's basis.
/// <see cref="SubtotalAmount"/>/<see cref="TotalTaxAmount"/>/<see cref="GrandTotalAmount"/> are
/// always server-computed from <see cref="OrderLine"/> rows, never client-supplied.
/// </summary>
public class Order
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OrganisationId { get; set; }

    public Guid LocationId { get; set; }

    public Guid TerminalId { get; set; }

    public long OrderNumber { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Open;

    public DateTimeOffset OpenedAtUtc { get; set; }

    public DateTimeOffset? ClosedAtUtc { get; set; }

    public Guid? OpenedByUserId { get; set; }

    public Guid? OpenedByStaffMemberId { get; set; }

    public string? Notes { get; set; }

    public bool IsTaxInclusivePricing { get; set; }

    public decimal SubtotalAmount { get; set; }

    public decimal TotalTaxAmount { get; set; }

    public decimal GrandTotalAmount { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
