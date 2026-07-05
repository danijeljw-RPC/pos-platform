namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for an assign/unassign action on a <see cref="Entities.MenuSectionItem"/> (PLAN-0004
/// Milestone G). <c>Action</c> is only ever <c>"Assigned"</c> or <c>"Unassigned"</c> — the join has
/// no update lifecycle (changing <c>DisplayOrder</c> means unassign then reassign), matching
/// <see cref="ProductModifierGroupChangedDomainEvent"/>.
/// </summary>
public sealed record MenuSectionItemChangedDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid MenuSectionItemId,
    Guid MenuSectionId,
    Guid ProductId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
