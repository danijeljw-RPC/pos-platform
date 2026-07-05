namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for an assign/unassign action on a <see cref="Entities.ProductModifierGroup"/> (PLAN-0004
/// Milestone E). <c>Action</c> is only ever <c>"Assigned"</c> or <c>"Unassigned"</c> — the join has
/// no update lifecycle (changing <c>DisplayOrder</c> means unassign then reassign).
/// </summary>
public sealed record ProductModifierGroupChangedDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid ProductModifierGroupId,
    Guid ProductId,
    Guid ModifierGroupId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
