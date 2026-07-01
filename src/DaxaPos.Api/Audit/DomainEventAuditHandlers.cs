using DaxaPos.Application.Events;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;

namespace DaxaPos.Api.Audit;

/// <summary>
/// In-process domain-event handlers writing <see cref="AuditEvent"/> rows (ADR-0010, ADR-0014's
/// Handler I/O Rule — a fast, local DB write is fine directly in a handler). Hosted in
/// <c>DaxaPos.Api</c>, not <c>DaxaPos.Persistence</c>, per ADR-0015 §4: no reference-graph change
/// for a single DB-touching handler consumer.
/// </summary>
public sealed class LocalUserLoginSucceededAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<LocalUserLoginSucceededDomainEvent>
{
    public async Task HandleAsync(LocalUserLoginSucceededDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            UserId = domainEvent.UserId,
            EventType = "LocalUserLoginSucceeded",
            EntityType = nameof(AuthSession),
            EntityId = domainEvent.AuthSessionId,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Only raised when a real <see cref="User"/> was matched (wrong password, or locked out) — see
/// <see cref="LocalUserLoginFailedDomainEvent"/>.
/// </summary>
public sealed class LocalUserLoginFailedAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<LocalUserLoginFailedDomainEvent>
{
    public async Task HandleAsync(LocalUserLoginFailedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            UserId = domainEvent.UserId,
            EventType = "LocalUserLoginFailed",
            EntityType = nameof(User),
            EntityId = domainEvent.UserId,
            Reason = domainEvent.FailureReason,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AuthSessionRevokedAuditHandler(DaxaDbContext dbContext)
    : IDomainEventHandler<AuthSessionRevokedDomainEvent>
{
    public async Task HandleAsync(AuthSessionRevokedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            OrganisationId = domainEvent.OrganisationId,
            UserId = domainEvent.UserId,
            StaffMemberId = domainEvent.StaffMemberId,
            EventType = "AuthSessionRevoked",
            EntityType = nameof(AuthSession),
            EntityId = domainEvent.AuthSessionId,
            Reason = domainEvent.Reason,
            OccurredAtUtc = domainEvent.OccurredAtUtc,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
