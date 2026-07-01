using DaxaPos.Domain.Events;

namespace DaxaPos.Application.Events;

/// <summary>
/// Reacts to a domain event in-process. Per ADR-0014's Handler I/O Rule, a handler must not
/// perform slow, unreliable, or external I/O directly — that work must be written as a durable
/// outbox/work item for DaxaPos.Workers instead.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
