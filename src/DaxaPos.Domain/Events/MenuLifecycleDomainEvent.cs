namespace DaxaPos.Domain.Events;

/// <summary>Raised for a create/update/deactivate/reactivate action on a <see cref="Entities.Menu"/> (PLAN-0004 Milestone G).</summary>
public sealed record MenuLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid MenuId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
