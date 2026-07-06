using System.Text.Json;
using DaxaPos.Application.Events;
using DaxaPos.Application.Outbox;
using DaxaPos.Application.Printing;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;

namespace DaxaPos.Api.Printing;

/// <summary>
/// PLAN-0005 Milestone E — the concrete proof of ADR-0014's Handler I/O Rule: a fully-settling
/// payment's <see cref="OrderLifecycleDomainEvent"/> (<c>Action == "Completed"</c>) enqueues a
/// durable <see cref="PrintReceiptWorkPayload.WorkType"/> outbox row instead of calling a printer
/// inline from the request handler. Registered alongside the existing
/// <c>OrderLifecycleAuditHandler</c> for the same event — ADR-0014's "one event, several
/// independent reactors" pattern, not a replacement for it. Every other
/// <see cref="OrderLifecycleDomainEvent"/> action (<c>Opened</c>/<c>Held</c>/<c>Resumed</c>/
/// <c>Voided</c>/<c>Cancelled</c>) is a no-op here — only order completion triggers a receipt
/// print.
/// </summary>
public sealed class OrderCompletedPrintOutboxHandler(DaxaDbContext dbContext) : IDomainEventHandler<OrderLifecycleDomainEvent>
{
    public async Task HandleAsync(OrderLifecycleDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.Action != "Completed")
        {
            return;
        }

        var now = domainEvent.OccurredAtUtc;

        dbContext.OutboxWorkItems.Add(new OutboxWorkItem
        {
            Id = Guid.NewGuid(),
            TenantId = domainEvent.TenantId,
            WorkType = PrintReceiptWorkPayload.WorkType,
            PayloadJson = JsonSerializer.Serialize(new PrintReceiptWorkPayload(domainEvent.OrderId)),
            Status = OutboxWorkItemStatus.Pending,
            AttemptCount = 0,
            MaxAttempts = OutboxRetryPolicy.DefaultMaxAttempts,
            CreatedAtUtc = now,
            NextAttemptAtUtc = now,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
