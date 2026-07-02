namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised when an admin issues a device registration PIN (ADR-0008's "PIN generated" audit
/// requirement, PLAN-0003 Milestone E). Never carries the PIN itself, raw or hashed.
/// </summary>
public sealed record DeviceRegistrationPinCreatedDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    Guid PinId,
    Guid? UserId,
    DateTimeOffset ExpiresAtUtc,
    int MaxUses,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
