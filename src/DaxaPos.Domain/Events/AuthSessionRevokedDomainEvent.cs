namespace DaxaPos.Domain.Events;

public sealed record AuthSessionRevokedDomainEvent(
    Guid TenantId,
    Guid? OrganisationId,
    Guid AuthSessionId,
    Guid? UserId,
    Guid? StaffMemberId,
    string Reason,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
