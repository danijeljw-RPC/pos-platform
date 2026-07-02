namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised when a device is revoked (ADR-0008, PLAN-0003 Milestone E). Terminal for that device
/// identity — all its credentials are revoked and it must re-register as a new
/// <see cref="Entities.Device"/>.
/// </summary>
public sealed record DeviceRevokedDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    Guid DeviceId,
    Guid? UserId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
