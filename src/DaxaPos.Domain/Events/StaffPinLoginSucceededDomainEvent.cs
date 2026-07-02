namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised when a staff member successfully authenticates via staff code + PIN on a trusted
/// device (PLAN-0003 Milestone F). Tenant/organisation/location/device come from the device's
/// <c>AuthContext</c> — never from the request body.
/// </summary>
public sealed record StaffPinLoginSucceededDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    Guid DeviceId,
    Guid StaffMemberId,
    Guid AuthSessionId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
