namespace DaxaPos.Domain.Events;

/// <summary>
/// Marker for an in-process domain event, per ADR-0014.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAtUtc { get; }
}
