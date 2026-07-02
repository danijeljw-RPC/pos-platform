namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised when a device credential is rotated (ADR-0008 rotation model, PLAN-0003 Milestone E).
/// The old credential stops granting access immediately.
/// </summary>
public sealed record DeviceCredentialRotatedDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    Guid DeviceId,
    Guid OldCredentialId,
    Guid NewCredentialId,
    Guid? UserId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
