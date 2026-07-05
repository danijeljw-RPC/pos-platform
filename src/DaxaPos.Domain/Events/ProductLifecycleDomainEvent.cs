namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a create/update/deactivate/reactivate/archive action on a <see cref="Entities.Product"/>
/// (PLAN-0004 Milestone D, OI-0007's audit requirement). <c>Action</c> is <c>"Created"</c>,
/// <c>"Updated"</c> (in-place, non-tax-affecting), <c>"Deactivated"</c>, <c>"Reactivated"</c>,
/// <c>"Archived"</c> (the superseded old row in an archive-and-replace), or
/// <c>"CreatedFromReplace"</c> (the new row in an archive-and-replace) — the latter two are always
/// raised as a pair from the same <c>TaxCategoryId</c>-changing <c>PATCH</c>, so OI-0007's "whether a
/// product was archived and replaced" audit requirement is answered by their co-occurrence, not a
/// separate boolean flag.
/// </summary>
public sealed record ProductLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid ProductId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
