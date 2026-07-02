namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised by the emergency-disable path (ADR-0013 hybrid deployment behaviour, PLAN-0003
/// Milestone F): the staff member is deactivated and all of their active sessions are revoked
/// in the same operation.
/// </summary>
public sealed record StaffMemberDisabledDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    Guid StaffMemberId,
    Guid? UserId,
    int SessionsRevoked,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
