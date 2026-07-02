namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for every failed staff PIN login attempt (PLAN-0003 Milestone F) — including unknown
/// staff codes (<paramref name="StaffMemberId"/> null, <paramref name="FailureReason"/> =
/// <c>"UnknownStaffCode"</c>), a deliberate departure from the unknown-email/unknown-PIN
/// precedents: the trusted device context supplies the tenant, so the non-nullable
/// <c>AuditEvent.TenantId</c> is always satisfiable here. The client response stays generic
/// regardless of the reason.
/// </summary>
public sealed record StaffPinLoginFailedDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    Guid DeviceId,
    Guid? StaffMemberId,
    string FailureReason,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
