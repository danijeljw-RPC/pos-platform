namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised when a device successfully completes PIN-gated registration (ADR-0008, PLAN-0003
/// Milestone E). Tenant/organisation/location come from the matched registration PIN row —
/// never from the request.
/// </summary>
public sealed record DeviceRegisteredDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    Guid DeviceId,
    Guid DeviceCredentialId,
    Guid PinId,
    string DeviceTypeName,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
