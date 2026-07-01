namespace DaxaPos.Domain.Events;

public sealed record LocalUserLoginSucceededDomainEvent(
    Guid TenantId,
    Guid OrganisationId,
    Guid UserId,
    Guid AuthSessionId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
