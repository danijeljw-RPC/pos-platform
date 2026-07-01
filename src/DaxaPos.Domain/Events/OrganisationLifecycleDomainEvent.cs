namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a create/update/deactivate/reactivate action on an <see cref="Entities.Organisation"/>
/// (PLAN-0003 Milestone D). One event type per entity, carrying an <see cref="Action"/>
/// discriminator, rather than one type per action — see the Milestone D plan's Design Decisions.
/// </summary>
public sealed record OrganisationLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
