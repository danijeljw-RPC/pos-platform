using System.Text.Json;
using DaxaPos.Application.Outbox;
using DaxaPos.Application.Printing;
using DaxaPos.Application.Receipts;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Enums;
using DaxaPos.Infrastructure.Printing;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Workers.Processing;

/// <summary>
/// Processes one <c>"PrintReceipt"</c> <see cref="OutboxWorkItem"/> (PLAN-0005 Milestone E) — loads
/// the order's already-immutable lines/tax snapshots/payments/refunds, renders them via
/// <see cref="ReceiptRenderer"/> (Milestone D, reused verbatim — this class never recomputes tax or
/// price), formats ESC/POS bytes via <see cref="EscPosReceiptFormatter"/>, and sends them through
/// <see cref="IPrinterTransport"/>. On failure, defers to <see cref="OutboxRetryPolicy"/> to decide
/// whether to retry (with backoff) or mark the item permanently <see cref="OutboxWorkItemStatus.Failed"/>.
/// </summary>
/// <remarks>
/// Deliberately duplicates <c>ReceiptEndpoints.BuildReceiptDocumentAsync</c>'s query shape rather
/// than extracting a shared helper: the two run in different processes/contexts (an authenticated
/// HTTP request vs. a background job with an ambient tenant context) and Milestone D's endpoint
/// code is intentionally left untouched — this milestone must not change receipt rendering
/// semantics. <see cref="ReceiptRenderer"/> itself (the actual rendering logic) is reused, not
/// duplicated; only the plain EF Core loading queries are repeated here.
/// </remarks>
public sealed class PrintReceiptOutboxProcessor(IPrinterTransport printerTransport)
{
    public async Task ProcessAsync(DaxaDbContext dbContext, OutboxWorkItem item, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        item.Status = OutboxWorkItemStatus.Processing;
        item.AttemptCount++;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var payload = JsonSerializer.Deserialize<PrintReceiptWorkPayload>(item.PayloadJson)
                ?? throw new InvalidOperationException($"Outbox work item {item.Id} has an unreadable PrintReceipt payload.");

            var receiptOrder = await LoadReceiptOrderInputAsync(dbContext, payload.OrderId, cancellationToken);
            var document = ReceiptRenderer.Render(receiptOrder, ReceiptLabelSet.Default);
            var bytes = EscPosReceiptFormatter.FormatReceipt(document);

            await printerTransport.SendAsync(bytes, cancellationToken);

            item.Status = OutboxWorkItemStatus.Completed;
            item.ProcessedAtUtc = now;
            item.LastError = null;
        }
        catch (Exception ex)
        {
            item.LastError = ex.Message;

            var decision = OutboxRetryPolicy.Decide(item.AttemptCount, item.MaxAttempts, now);
            if (decision.ShouldRetry)
            {
                item.Status = OutboxWorkItemStatus.Pending;
                item.NextAttemptAtUtc = decision.NextAttemptAtUtc!.Value;
            }
            else
            {
                item.Status = OutboxWorkItemStatus.Failed;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Mirrors <c>ReceiptEndpoints.BuildReceiptDocumentAsync</c>'s query shape — see this class's remarks.</summary>
    private static async Task<ReceiptOrderInput> LoadReceiptOrderInputAsync(DaxaDbContext dbContext, Guid orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleAsync(o => o.Id == orderId, cancellationToken);

        var lines = await dbContext.OrderLines
            .Where(line => line.OrderId == order.Id)
            .ToListAsync(cancellationToken);

        var lineIds = lines.Select(line => line.Id).ToList();
        var taxesByLineId = (await dbContext.OrderLineTaxes
                .Where(tax => lineIds.Contains(tax.OrderLineId))
                .ToListAsync(cancellationToken))
            .GroupBy(tax => tax.OrderLineId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var payments = await dbContext.Payments
            .Where(payment => payment.OrderId == order.Id)
            .OrderBy(payment => payment.RecordedAtUtc)
            .ToListAsync(cancellationToken);

        var paymentIds = payments.Select(payment => payment.Id).ToList();
        var refunds = await dbContext.Refunds
            .Where(refund => paymentIds.Contains(refund.PaymentId))
            .OrderBy(refund => refund.RecordedAtUtc)
            .ToListAsync(cancellationToken);

        return new ReceiptOrderInput(
            order.Id,
            order.OrderNumber,
            order.OpenedAtUtc,
            order.ClosedAtUtc,
            order.SubtotalAmount,
            order.TotalTaxAmount,
            order.GrandTotalAmount,
            lines.Select(line => new ReceiptLineInput(
                line.Id,
                line.ProductNameSnapshot,
                line.Quantity,
                line.UnitPriceSnapshot,
                line.LineTotalAmount,
                line.Status == OrderLineStatus.Voided,
                taxesByLineId.TryGetValue(line.Id, out var taxes)
                    ? taxes.Select(tax => new ReceiptLineTaxInput(
                        tax.TaxNameSnapshot,
                        tax.RatePercentSnapshot,
                        tax.TaxableAmount,
                        tax.TaxAmount,
                        tax.ReceiptMarkerCodeSnapshot,
                        tax.ReceiptMarkerLabelSnapshot)).ToList()
                    : []))
                .ToList(),
            payments.Select(payment => new ReceiptPaymentInput(payment.Id, payment.Method.ToString(), payment.AmountApproved ?? 0m, payment.RecordedAtUtc)).ToList(),
            refunds.Select(refund => new ReceiptRefundInput(refund.Id, refund.PaymentId, refund.Amount, refund.ReasonCode, refund.RecordedAtUtc)).ToList());
    }
}
