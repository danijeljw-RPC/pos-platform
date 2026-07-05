using DaxaPos.Domain.Enums;

namespace DaxaPos.Domain.Entities;

/// <summary>
/// A payment against an <see cref="Order"/> (PLAN-0005 Milestone B). No <see cref="OrganisationId"/>
/// column of its own — scoped entirely through <see cref="OrderId"/>, matching <see cref="OrderLine"/>'s
/// precedent from Milestone A. Cash and <see cref="PaymentMethod.ManualEftpos"/> are recorded
/// immediately as <see cref="PaymentStatus.Recorded"/> with <see cref="AmountApproved"/> set equal
/// to <see cref="AmountRequested"/> — neither method has an external system to await a result from.
/// Append-only per ADR-0010: once created, a <see cref="Payment"/> row's financial fields are never
/// edited; <see cref="PaymentLedgerEntry"/> carries the transition trail.
/// </summary>
public class Payment
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OrderId { get; set; }

    public Guid LocationId { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; }

    public decimal AmountRequested { get; set; }

    public decimal? AmountApproved { get; set; }

    public Guid IdempotencyKey { get; set; }

    public Guid? TakenByUserId { get; set; }

    public Guid? TakenByStaffMemberId { get; set; }

    public DateTimeOffset RecordedAtUtc { get; set; }

    public string? ProviderReference { get; set; }
}
