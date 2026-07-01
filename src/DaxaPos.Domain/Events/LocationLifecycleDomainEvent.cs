namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a create/update/deactivate/reactivate action on a <see cref="Entities.Location"/>
/// (PLAN-0003 Milestone D). See <see cref="OrganisationLifecycleDomainEvent"/> for why this is one
/// event type per entity rather than one per action.
/// </summary>
public sealed record LocationLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid LocationId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
