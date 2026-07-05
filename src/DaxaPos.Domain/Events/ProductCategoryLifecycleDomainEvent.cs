namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a create/update/deactivate/reactivate action on a <see cref="Entities.ProductCategory"/>
/// (PLAN-0004 Milestone D).
/// </summary>
public sealed record ProductCategoryLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid ProductCategoryId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
