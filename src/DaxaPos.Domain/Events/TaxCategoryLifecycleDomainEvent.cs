namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a create/update/deactivate/reactivate action on a <see cref="Entities.TaxCategory"/>
/// (PLAN-0004 Milestone C, OI-0007's audit requirement).
/// </summary>
public sealed record TaxCategoryLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid TaxCategoryId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
