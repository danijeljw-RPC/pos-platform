namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for staff-member management actions (PLAN-0003 Milestone F): <paramref name="Action"/>
/// is <c>"Created"</c>, <c>"PinReset"</c>, or <c>"RoleAssigned"</c> — audited as
/// <c>$"StaffMember{Action}"</c>, per ADR-0013's requirement that identity/permission changes are
/// audit logged. Disabling has its own <see cref="StaffMemberDisabledDomainEvent"/> because it
/// also carries session-revocation semantics.
/// </summary>
public sealed record StaffMemberLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    Guid StaffMemberId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
