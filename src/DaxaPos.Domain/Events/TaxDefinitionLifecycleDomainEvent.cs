namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a create/update/deactivate/reactivate action on a <see cref="Entities.TaxDefinition"/>
/// (PLAN-0004 Milestone C, OI-0007's audit requirement). <c>Action</c> is <c>"Created"</c>,
/// <c>"CreatedFromTemplate"</c> (distinguishes a from-template clone from a from-scratch
/// definition in the audit trail), <c>"Updated"</c>, <c>"Deactivated"</c>, or <c>"Reactivated"</c> —
/// see <see cref="Events.LocationLifecycleDomainEvent"/> for why this is one event type per entity
/// rather than one per action.
/// </summary>
public sealed record TaxDefinitionLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid TaxDefinitionId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
