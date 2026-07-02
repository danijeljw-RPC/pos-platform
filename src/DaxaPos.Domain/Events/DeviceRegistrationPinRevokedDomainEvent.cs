namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised when an admin revokes a device registration PIN before its expiry (PLAN-0003
/// Milestone E, approved amendment).
/// </summary>
public sealed record DeviceRegistrationPinRevokedDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    Guid PinId,
    Guid? UserId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
