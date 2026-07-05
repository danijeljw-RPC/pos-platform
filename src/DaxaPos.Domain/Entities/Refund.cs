using DaxaPos.Domain.Enums;

namespace DaxaPos.Domain.Entities;

/// <summary>
/// A refund against a <see cref="Payment"/> (PLAN-0005 Milestone C) — a reversal record, per
/// ADR-0010: the original <see cref="Payment"/>/<see cref="Order"/> rows are never mutated by a
/// refund, their financial fields stay exactly as they were at sale time. No
/// <see cref="OrganisationId"/>/<see cref="LocationId"/> column of its own — scoped entirely
/// through <see cref="PaymentId"/>/<see cref="OrderId"/>, matching <see cref="Payment"/>'s own
/// precedent of deriving organisation/location context through its parent rather than
/// denormalizing every ancestor column. <see cref="ReasonCode"/> is a caller-supplied free-form
/// code (matching <see cref="TaxCategory.Code"/>'s precedent), not a closed enum — the plan names
/// the field but does not enumerate a fixed set of reason values, and guessing one now risks being
/// wrong in a way a later milestone would need to rework anyway. No <see cref="IdempotencyKey"/>
/// field, unlike <see cref="Payment"/> — the plan's Milestone C entity field list does not include
/// one for <see cref="Refund"/>.
/// </summary>
public class Refund
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid PaymentId { get; set; }

    public Guid OrderId { get; set; }

    public decimal Amount { get; set; }

    public string ReasonCode { get; set; } = string.Empty;

    public string? ReasonNote { get; set; }

    public Guid? RequestedByUserId { get; set; }

    public Guid? RequestedByStaffMemberId { get; set; }

    public RefundStatus Status { get; set; }

    public DateTimeOffset RecordedAtUtc { get; set; }

    public string? ProviderReference { get; set; }
}
