namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised when a receipt is explicitly reprinted after the fact (PLAN-0005 Milestone D) — CLAUDE.md's
/// "Receipt reprints must be audited" requirement. Not raised for a live-sale receipt view/print
/// (that surface stays under <c>orders.manage</c>, unaudited, per the plan's own Milestone D scope).
/// No dedicated <c>Receipt</c> entity/table exists (receipts are rendered from <c>Order</c>/
/// <c>OrderLine</c>/<c>OrderLineTax</c>/<c>Payment</c>/<c>Refund</c>, never persisted as their own
/// row) — <see cref="OrderId"/> is the only entity link the audit row needs.
/// </summary>
public sealed record ReceiptReprintedDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid OrderId,
    Guid? UserId,
    Guid? StaffMemberId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
