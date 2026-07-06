using DaxaPos.Domain.Enums;

namespace DaxaPos.Domain.Entities;

/// <summary>
/// A durable, generic unit of work for asynchronous processing outside the request path (PLAN-0005
/// Milestone E) — the concrete mechanism ADR-0014's Handler I/O Rule requires for any domain event
/// handler that needs slow/unreliable/external I/O (e.g. sending a receipt to a printer). Written
/// by an in-process domain event handler immediately after the triggering change is saved; consumed
/// asynchronously by <c>DaxaPos.Workers</c>. Not printing-specific — <see cref="WorkType"/>
/// distinguishes what kind of work a row represents, so a later plan (e.g. a payment-provider
/// webhook retry) can reuse this same table rather than inventing a second one, per ADR-0014's own
/// Follow-Up Work ("the two should likely share one outbox table/worker pattern rather than being
/// built twice").
/// </summary>
public class OutboxWorkItem
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    /// <summary>E.g. <c>"PrintReceipt"</c> — the only work type this milestone produces or consumes.</summary>
    public string WorkType { get; set; } = string.Empty;

    /// <summary>Serialized work-type-specific payload (e.g. the order id to print a receipt for).</summary>
    public string PayloadJson { get; set; } = string.Empty;

    public OutboxWorkItemStatus Status { get; set; }

    public int AttemptCount { get; set; }

    public int MaxAttempts { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>When this row becomes eligible to be claimed again (backoff after a failed attempt).</summary>
    public DateTimeOffset NextAttemptAtUtc { get; set; }

    public DateTimeOffset? ProcessedAtUtc { get; set; }
}
