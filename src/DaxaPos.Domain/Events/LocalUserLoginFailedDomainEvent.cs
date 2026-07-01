namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised only when a real <c>User</c> record was matched (wrong password, or locked out) — an
/// unknown email has no tenant to attach an audit row to and is not raised as this event (see
/// PLAN-0003 Milestone C notes).
/// </summary>
public sealed record LocalUserLoginFailedDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid UserId,
    string FailureReason,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
