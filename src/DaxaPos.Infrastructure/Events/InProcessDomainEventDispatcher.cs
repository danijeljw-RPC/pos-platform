using DaxaPos.Application.Events;
using DaxaPos.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Infrastructure.Events;

/// <summary>
/// Simple in-process implementation of <see cref="IDomainEventDispatcher"/>, per ADR-0014.
/// Resolves whatever <see cref="IDomainEventHandler{TEvent}"/> instances are registered for the
/// event's type and invokes them in-process. With no handlers registered, this is a no-op.
/// </summary>
public sealed class InProcessDomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var handlers = serviceProvider.GetServices<IDomainEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }
}
