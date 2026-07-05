namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a whole-<see cref="Entities.Order"/> lifecycle action (PLAN-0005 Milestone A).
/// <c>Action</c> is <c>"Opened"</c>, <c>"Held"</c>, <c>"Resumed"</c>, <c>"Voided"</c>, or
/// <c>"Cancelled"</c>. Carries both <see cref="UserId"/> and <see cref="StaffMemberId"/> — only one
/// is ever populated for a given event, matching the dual-identity pattern
/// <c>ProductLocationOverrideChangedDomainEvent</c> established, since order actions are the
/// plan's first staff-PIN-accessible write from day one (unlike catalogue writes, which stayed
/// admin-only until Milestone F).
/// </summary>
public sealed record OrderLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid OrderId,
    Guid? UserId,
    Guid? StaffMemberId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
