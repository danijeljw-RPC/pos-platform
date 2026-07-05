namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a create/update/deactivate/reactivate action on a <see cref="Entities.ProductVariant"/>
/// (PLAN-0004 Milestone E). <see cref="OrganisationId"/> is carried for audit context even though
/// the entity itself has no <c>OrganisationId</c> column — resolved via <c>ProductId</c> at the
/// endpoint layer, matching <see cref="TerminalLifecycleDomainEvent"/>'s precedent.
/// </summary>
public sealed record ProductVariantLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid ProductVariantId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
