namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a create/update/deactivate/reactivate action on a <see cref="Entities.ModifierGroup"/>
/// (PLAN-0004 Milestone E).
/// </summary>
public sealed record ModifierGroupLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid ModifierGroupId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
