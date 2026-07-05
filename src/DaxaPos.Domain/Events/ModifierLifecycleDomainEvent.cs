namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a create/update/deactivate/reactivate action on a <see cref="Entities.Modifier"/>
/// (PLAN-0004 Milestone E). <see cref="OrganisationId"/> is carried for audit context even though
/// the entity itself has no <c>OrganisationId</c> column — resolved via <c>ModifierGroupId</c> at
/// the endpoint layer.
/// </summary>
public sealed record ModifierLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid ModifierId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
