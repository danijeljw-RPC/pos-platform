namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a create/delete action on a <see cref="Entities.MenuAvailabilityRule"/> (PLAN-0004
/// Milestone G). <c>Action</c> is only ever <c>"Created"</c> or <c>"Deleted"</c> — the plan's
/// endpoint list for this entity is create/list/delete only, no update.
/// </summary>
public sealed record MenuAvailabilityRuleChangedDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid MenuAvailabilityRuleId,
    Guid MenuId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
