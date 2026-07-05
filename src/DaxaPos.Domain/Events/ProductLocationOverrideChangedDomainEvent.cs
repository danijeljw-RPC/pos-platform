namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a create/update/delete/sold-out-toggle action on a
/// <see cref="Entities.ProductLocationOverride"/> (PLAN-0004 Milestone F). <c>Action</c> is
/// <c>"Created"</c>, <c>"Updated"</c>, or <c>"Deleted"</c> for the <c>pricing.manage</c>-gated CRUD
/// endpoints, or <c>"SoldOutToggled"</c> for the separate staff-accessible sold-out endpoint — kept
/// distinct in the audit trail so a reviewer can tell which surface made the change, even though
/// both write the same entity. <see cref="StaffMemberId"/> is carried alongside
/// <see cref="UserId"/> since the sold-out toggle is the plan's first staff-PIN-accessible
/// catalogue write (only one of the two is ever non-null for a given event).
/// </summary>
public sealed record ProductLocationOverrideChangedDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid ProductLocationOverrideId,
    Guid ProductId,
    Guid LocationId,
    Guid? UserId,
    Guid? StaffMemberId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
