namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a create/delete action on a <see cref="Entities.TaxCategoryDefinition"/> (PLAN-0004
/// Milestone C). Unlike <see cref="TaxDefinitionLifecycleDomainEvent"/>/
/// <see cref="TaxCategoryLifecycleDomainEvent"/>, this mapping row supports hard delete rather than
/// deactivate/reactivate (it is not itself a financial record, ADR-0010) — <c>Action</c> is only
/// ever <c>"Created"</c> or <c>"Deleted"</c>. <see cref="LocationId"/> is carried for audit context
/// even though it is nullable on the mapping itself (null = organisation-wide).
/// </summary>
public sealed record TaxCategoryDefinitionChangedDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid TaxCategoryDefinitionId,
    Guid? LocationId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
