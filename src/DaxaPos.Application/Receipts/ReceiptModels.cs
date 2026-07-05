namespace DaxaPos.Application.Receipts;

/// <summary>
/// One <c>OrderLineTax</c> row carried by value into <see cref="ReceiptRenderer"/> (PLAN-0005
/// Milestone D) — the renderer never queries the database or knows about
/// <c>DaxaPos.Domain.Entities.OrderLineTax</c> directly, mirroring
/// <see cref="Orders.OrderTaxAggregation"/>/<see cref="Payments.RefundSettlement"/>'s
/// dependency-free style. <see cref="ReceiptMarkerCode"/>/<see cref="ReceiptMarkerLabel"/> are the
/// snapshot values already stored on <c>OrderLineTax</c> at add-line time (ADR-0011) — this type
/// reads that precedence, it does not resolve markers a second time.
/// </summary>
public sealed record ReceiptLineTaxInput(
    string TaxName,
    decimal RatePercent,
    decimal TaxableAmount,
    decimal TaxAmount,
    string? ReceiptMarkerCode,
    string? ReceiptMarkerLabel);

/// <summary>One <c>OrderLine</c> row carried by value into <see cref="ReceiptRenderer"/>.</summary>
public sealed record ReceiptLineInput(
    Guid OrderLineId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotalAmount,
    bool Voided,
    IReadOnlyList<ReceiptLineTaxInput> Taxes);

/// <summary>One <c>Payment</c> row carried by value into <see cref="ReceiptRenderer"/>.</summary>
public sealed record ReceiptPaymentInput(
    Guid PaymentId,
    string Method,
    decimal AmountApproved,
    DateTimeOffset RecordedAtUtc);

/// <summary>
/// One <c>Refund</c> row carried by value into <see cref="ReceiptRenderer"/>. <see cref="PaymentId"/>
/// is the original payment the refund reverses (ADR-0010) — its presence in the same
/// <see cref="ReceiptDocument"/> as the order's own payments is the refund-receipt linking behaviour
/// this milestone requires; no separate "refund receipt" shape is needed.
/// </summary>
public sealed record ReceiptRefundInput(
    Guid RefundId,
    Guid PaymentId,
    decimal Amount,
    string ReasonCode,
    DateTimeOffset RecordedAtUtc);

/// <summary>
/// The already-loaded <c>Order</c> aggregate (its active/voided lines, tax snapshots, payments, and
/// refunds) that <see cref="ReceiptRenderer.Render"/> projects into a <see cref="ReceiptDocument"/>.
/// Every amount here is read verbatim from persisted snapshots — the renderer never recomputes tax
/// or price (ADR-0006, ADR-0010's "PDF Generation Strategy" pattern).
/// </summary>
public sealed record ReceiptOrderInput(
    Guid OrderId,
    long OrderNumber,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    decimal SubtotalAmount,
    decimal TotalTaxAmount,
    decimal GrandTotalAmount,
    IReadOnlyList<ReceiptLineInput> Lines,
    IReadOnlyList<ReceiptPaymentInput> Payments,
    IReadOnlyList<ReceiptRefundInput> Refunds);

/// <summary>
/// Venue-configurable receipt label strings (ADR-0011 + ADR-0016) — the renderer reads these from
/// its caller rather than baking any wording into rendering logic, so a later locale/venue override
/// does not require touching <see cref="ReceiptRenderer"/>. <see cref="Default"/> is the only
/// concrete value shipped in this milestone (en-AU, per ADR-0016 §7's MVP scope) — a future
/// location-level settings lookup supplies a different instance without any renderer change.
/// </summary>
public sealed record ReceiptLabelSet(string TotalLabel, string TaxInclusiveSummaryLabel)
{
    public static ReceiptLabelSet Default { get; } = new("Total", "Includes GST");
}

/// <summary>One rendered receipt line — the product name is never replaced by a tax description (ADR-0011).</summary>
public sealed record ReceiptLineItemLine(
    string ProductName,
    int Quantity,
    decimal LineTotalAmount,
    string? TaxMarkerCode);

/// <summary>One tax name's aggregated total across all of the receipt's active lines.</summary>
public sealed record ReceiptTaxSummaryEntry(
    string TaxName,
    decimal RatePercent,
    decimal TaxableAmount,
    decimal TaxAmount);

/// <summary>One rendered payment summary line.</summary>
public sealed record ReceiptPaymentSummaryLine(
    Guid PaymentId,
    string Method,
    decimal AmountApproved,
    DateTimeOffset RecordedAtUtc);

/// <summary>One rendered refund summary line, still carrying its link back to the original payment.</summary>
public sealed record ReceiptRefundSummaryLine(
    Guid RefundId,
    Guid PaymentId,
    decimal Amount,
    string ReasonCode,
    DateTimeOffset RecordedAtUtc);

/// <summary>
/// The structured output of <see cref="ReceiptRenderer.Render"/> — not a print-ready byte stream
/// (ESC/POS generation is Milestone E's job); a venue-agnostic, print-transport-agnostic model a
/// later thermal/PDF/digital renderer can consume.
/// </summary>
public sealed record ReceiptDocument(
    Guid OrderId,
    long OrderNumber,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    IReadOnlyList<ReceiptLineItemLine> Lines,
    decimal SubtotalAmount,
    string TotalLabel,
    decimal GrandTotalAmount,
    IReadOnlyList<ReceiptTaxSummaryEntry> TaxSummary,
    string TaxInclusiveSummaryLabel,
    decimal TotalTaxAmount,
    IReadOnlyList<string> MarkerLegend,
    IReadOnlyList<ReceiptPaymentSummaryLine> Payments,
    IReadOnlyList<ReceiptRefundSummaryLine> Refunds);
