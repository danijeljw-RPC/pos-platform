namespace DaxaPos.Application.Receipts;

/// <summary>
/// Pure, DB-independent receipt rendering (PLAN-0005 Milestone D) — mirrors
/// <see cref="Orders.OrderTaxAggregation"/>/<see cref="Payments.RefundSettlement"/>'s dependency-free
/// shape. Never queries the database, never knows about <c>Order</c>/<c>OrderLine</c>/<c>Payment</c>/
/// <c>Refund</c> entities directly, has no constructor dependencies to inject — callers pass in
/// already-loaded snapshots via <see cref="ReceiptOrderInput"/>. Never recomputes tax or price: every
/// amount in the output is read verbatim from the input (ADR-0006, ADR-0010's "PDF Generation
/// Strategy" pattern, ADR-0011's receipt tax marker strategy).
/// </summary>
public static class ReceiptRenderer
{
    public static ReceiptDocument Render(ReceiptOrderInput order, ReceiptLabelSet labels)
    {
        // Voided lines are a reversal (ADR-0010) — they were removed from the sale and never
        // charged, so they do not appear on the printed receipt. Order.SubtotalAmount/
        // GrandTotalAmount already exclude them (Milestone A's RecomputeOrderTotalsAsync).
        var activeLines = order.Lines.Where(line => !line.Voided).ToList();

        var lines = activeLines
            .Select(line => new ReceiptLineItemLine(
                line.ProductName,
                line.Quantity,
                line.LineTotalAmount,
                ResolveLineMarkerCode(line)))
            .ToList();

        var taxSummary = activeLines
            .SelectMany(line => line.Taxes)
            .GroupBy(tax => tax.TaxName, StringComparer.Ordinal)
            .Select(group => new ReceiptTaxSummaryEntry(
                group.Key,
                group.First().RatePercent,
                group.Sum(tax => tax.TaxableAmount),
                group.Sum(tax => tax.TaxAmount)))
            .OrderBy(entry => entry.TaxName, StringComparer.Ordinal)
            .ToList();

        var markerLegend = activeLines
            .SelectMany(line => line.Taxes)
            .Where(tax => tax.ReceiptMarkerCode is not null)
            .Select(tax => (Code: tax.ReceiptMarkerCode!, Label: tax.ReceiptMarkerLabel ?? tax.ReceiptMarkerCode!))
            .Distinct()
            .OrderBy(marker => marker.Code, StringComparer.Ordinal)
            .Select(marker => $"{marker.Code} = {marker.Label}")
            .ToList();

        var payments = order.Payments
            .Select(payment => new ReceiptPaymentSummaryLine(payment.PaymentId, payment.Method, payment.AmountApproved, payment.RecordedAtUtc))
            .ToList();

        var refunds = order.Refunds
            .Select(refund => new ReceiptRefundSummaryLine(refund.RefundId, refund.PaymentId, refund.Amount, refund.ReasonCode, refund.RecordedAtUtc))
            .ToList();

        return new ReceiptDocument(
            order.OrderId,
            order.OrderNumber,
            order.OpenedAtUtc,
            order.ClosedAtUtc,
            lines,
            order.SubtotalAmount,
            labels.TotalLabel,
            order.GrandTotalAmount,
            taxSummary,
            labels.TaxInclusiveSummaryLabel,
            order.TotalTaxAmount,
            markerLegend,
            payments,
            refunds);
    }

    /// <summary>
    /// A line's displayed marker is the first non-null <see cref="ReceiptLineTaxInput.ReceiptMarkerCode"/>
    /// among its tax components — reads the precedence ADR-0011 already resolved into the snapshot
    /// at add-line time (item override → tax category marker → tax definition marker → location
    /// default), does not re-resolve it. In practice a line has at most one marker-bearing component.
    /// </summary>
    private static string? ResolveLineMarkerCode(ReceiptLineInput line) =>
        line.Taxes.Select(tax => tax.ReceiptMarkerCode).FirstOrDefault(code => code is not null);
}
