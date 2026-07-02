namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a failed device registration attempt — but only when the presented PIN matched a
/// real <see cref="Entities.DeviceRegistrationPin"/> row that resolves to a single tenant
/// (expired/revoked/exhausted PIN, or an ambiguous multi-match within one tenant). An unknown PIN
/// raises nothing: there is no tenant to attach the audit row to, since
/// <see cref="Entities.AuditEvent.TenantId"/> is non-nullable by design — the same rule as
/// <see cref="LocalUserLoginFailedDomainEvent"/>'s unknown-email case. Rate limiting covers the
/// unknown-PIN abuse path; a tenant-less global security-event store is a flagged future need.
/// </summary>
public sealed record DeviceRegistrationFailedDomainEvent(
    Guid TenantId,
    Guid? OrganisationId,
    Guid? LocationId,
    Guid? PinId,
    string FailureReason,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
