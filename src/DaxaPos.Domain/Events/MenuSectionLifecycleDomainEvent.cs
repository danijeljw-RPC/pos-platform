namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a create/update action on a <see cref="Entities.MenuSection"/> (PLAN-0004 Milestone
/// G). No separate deactivate/reactivate — <c>IsActive</c> is one of the fields the single
/// <c>PATCH</c> endpoint updates, matching <see cref="ProductLocationOverrideChangedDomainEvent"/>'s
/// combined-update style. <see cref="OrganisationId"/> is carried for audit context even though the
/// entity has no <c>OrganisationId</c> column — resolved via <c>MenuId</c> at the endpoint layer.
/// </summary>
public sealed record MenuSectionLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid MenuSectionId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
