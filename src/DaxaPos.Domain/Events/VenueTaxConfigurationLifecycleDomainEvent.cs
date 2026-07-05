namespace DaxaPos.Domain.Events;

/// <summary>
/// Raised for a create/update action on a <see cref="Entities.VenueTaxConfiguration"/> (PLAN-0004
/// Milestone F). No deactivate/reactivate — the entity has no <c>IsActive</c> lifecycle (removing a
/// venue's tax configuration is not a supported operation in this milestone; see
/// <c>docs/modules/tax.md</c>).
/// </summary>
public sealed record VenueTaxConfigurationLifecycleDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid VenueTaxConfigurationId,
    Guid LocationId,
    Guid? UserId,
    string Action,
    string? BeforeValue,
    string? AfterValue,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
