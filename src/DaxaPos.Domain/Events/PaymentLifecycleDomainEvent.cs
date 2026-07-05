namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a <see cref="Entities.Payment"/> lifecycle action (PLAN-0005 Milestone B). <c>Action</c>
/// is <c>"Recorded"</c> in this milestone (cash/manual EFTPOS settle immediately — there is no
/// intermediate state to raise a second event for). Carries both <see cref="UserId"/> and
/// <see cref="StaffMemberId"/> — only one is ever populated for a given event, matching
/// <see cref="OrderLifecycleDomainEvent"/>'s dual-identity pattern (payment recording is
/// staff-PIN-accessible from day one, same as order entry).
/// </summary>
public sealed record PaymentLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid OrderId,
    Guid PaymentId,
    Guid? UserId,
    Guid? StaffMemberId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
