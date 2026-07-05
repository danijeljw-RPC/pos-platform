namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for an <see cref="Entities.OrderLine"/> add/void action (PLAN-0005 Milestone A).
/// <c>Action</c> is <c>"LineAdded"</c> or <c>"LineVoided"</c>. Same dual <see cref="UserId"/>/
/// <see cref="StaffMemberId"/> pattern as <see cref="OrderLifecycleDomainEvent"/>.
/// </summary>
public sealed record OrderLineChangedDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid OrderId,
    Guid OrderLineId,
    Guid? UserId,
    Guid? StaffMemberId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
