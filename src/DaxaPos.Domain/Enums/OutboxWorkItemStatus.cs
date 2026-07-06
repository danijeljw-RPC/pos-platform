namespace DaxaPos.Domain.Enums;

/// <summary>
/// Lifecycle of a durable <see cref="Entities.OutboxWorkItem"/> row (PLAN-0005 Milestone E,
/// ADR-0014's Handler I/O Rule).
/// </summary>
public enum OutboxWorkItemStatus
{
    /// <summary>Waiting to be picked up by <c>DaxaPos.Workers</c>.</summary>
    Pending = 0,

    /// <summary>Claimed by a worker and currently being processed.</summary>
    Processing = 1,

    /// <summary>Processed successfully — terminal state.</summary>
    Completed = 2,

    /// <summary>Exhausted its retry attempts (per the outbox retry policy) — terminal state.</summary>
    Failed = 3,
}
