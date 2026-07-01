using DaxaPos.Domain.Events;

namespace DaxaPos.Application.Events;

/// <summary>
/// Dispatches a domain event to its registered in-process handlers, per ADR-0014.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}
