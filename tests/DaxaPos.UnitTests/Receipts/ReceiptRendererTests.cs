using DaxaPos.Application.Receipts;

namespace DaxaPos.UnitTests.Receipts;

/// <summary>
/// PLAN-0005 Milestone D. Proves <see cref="ReceiptRenderer"/> is a pure, DB-independent
/// projection over already-loaded order/line/tax/payment/refund snapshots — it must never
/// recompute tax or price, only read the values it is given (ADR-0010's "PDF Generation Strategy"
/// pattern; ADR-0006's tax-line snapshots; ADR-0011's configurable receipt tax marker). Mirrors
/// <see cref="DaxaPos.UnitTests.Orders.OrderTaxAggregationTests"/>/<see cref="DaxaPos.UnitTests.Payments.RefundSettlementTests"/>'
/// pure, dependency-free style.
/// </summary>
public class ReceiptRendererTests
{
    [Fact]
    public void Render_AuMixedBasket_MatchesClaudeMdWorkedExample()
    {
        var order = new ReceiptOrderInput(
            OrderId: Guid.NewGuid(),
            OrderNumber: 42,
            OpenedAtUtc: DateTimeOffset.UtcNow,
            ClosedAtUtc: DateTimeOffset.UtcNow,
            SubtotalAmount: 19.00m,
            TotalTaxAmount: 1.30m,
            GrandTotalAmount: 20.30m,
            Lines:
            [
                new ReceiptLineInput(Guid.NewGuid(), "Flat white", 1, 5.50m, 5.50m, Voided: false,
                    Taxes: [new ReceiptLineTaxInput("GST", 10m, 5.00m, 0.50m, null, null)]),
                new ReceiptLineInput(Guid.NewGuid(), "Chocolate cake slice", 1, 8.80m, 8.80m, Voided: false,
                    Taxes: [new ReceiptLineTaxInput("GST", 10m, 8.00m, 0.80m, null, null)]),
                new ReceiptLineInput(Guid.NewGuid(), "Loaf of bread", 1, 6.00m, 6.00m, Voided: false,
                    Taxes: [new ReceiptLineTaxInput("GST", 0m, 6.00m, 0.00m, "F", "GST-free")]),
            ],
            Payments: [],
            Refunds: []);

        var receipt = ReceiptRenderer.Render(order, ReceiptLabelSet.Default);

        Assert.Equal(3, receipt.Lines.Count);
        Assert.Equal("Flat white", receipt.Lines[0].ProductName);
        Assert.Equal(5.50m, receipt.Lines[0].LineTotalAmount);
        Assert.Null(receipt.Lines[0].TaxMarkerCode);
        Assert.Equal("Loaf of bread", receipt.Lines[2].ProductName);
        Assert.Equal(6.00m, receipt.Lines[2].LineTotalAmount);
        Assert.Equal("F", receipt.Lines[2].TaxMarkerCode);

        Assert.Equal(19.00m, receipt.SubtotalAmount);
        Assert.Equal(20.30m, receipt.GrandTotalAmount);
        Assert.Equal(1.30m, receipt.TotalTaxAmount);
        Assert.Equal("Total", receipt.TotalLabel);
        Assert.Equal("Includes GST", receipt.TaxInclusiveSummaryLabel);

        Assert.Equal(["F = GST-free"], receipt.MarkerLegend);
    }

    [Fact]
    public void Render_TaxSummary_AggregatesByTaxNameAcrossLines()
    {
        var order = OrderWith(
            new ReceiptLineInput(Guid.NewGuid(), "Item A", 1, 11.00m, 11.00m, Voided: false,
                Taxes: [new ReceiptLineTaxInput("GST", 10m, 10.00m, 1.00m, null, null)]),
            new ReceiptLineInput(Guid.NewGuid(), "Item B", 1, 22.00m, 22.00m, Voided: false,
                Taxes: [new ReceiptLineTaxInput("GST", 10m, 20.00m, 2.00m, null, null)]));

        var receipt = ReceiptRenderer.Render(order, ReceiptLabelSet.Default);

        var gstSummary = Assert.Single(receipt.TaxSummary);
        Assert.Equal("GST", gstSummary.TaxName);
        Assert.Equal(30.00m, gstSummary.TaxableAmount);
        Assert.Equal(3.00m, gstSummary.TaxAmount);
    }

    [Fact]
    public void Render_VoidedLines_AreExcludedFromReceipt()
    {
        var order = OrderWith(
            new ReceiptLineInput(Guid.NewGuid(), "Kept item", 1, 5.00m, 5.00m, Voided: false, Taxes: []),
            new ReceiptLineInput(Guid.NewGuid(), "Voided item", 1, 3.00m, 3.00m, Voided: true, Taxes: []));

        var receipt = ReceiptRenderer.Render(order, ReceiptLabelSet.Default);

        var line = Assert.Single(receipt.Lines);
        Assert.Equal("Kept item", line.ProductName);
    }

    [Fact]
    public void Render_NoTaxFreeMarkerConfigured_LegendIsEmpty()
    {
        var order = OrderWith(
            new ReceiptLineInput(Guid.NewGuid(), "Taxable item", 1, 11.00m, 11.00m, Voided: false,
                Taxes: [new ReceiptLineTaxInput("GST", 10m, 10.00m, 1.00m, null, null)]));

        var receipt = ReceiptRenderer.Render(order, ReceiptLabelSet.Default);

        Assert.Empty(receipt.MarkerLegend);
    }

    [Fact]
    public void Render_MultipleLinesShareSameMarker_LegendListedOnce()
    {
        var order = OrderWith(
            new ReceiptLineInput(Guid.NewGuid(), "Bread", 1, 6.00m, 6.00m, Voided: false,
                Taxes: [new ReceiptLineTaxInput("GST", 0m, 6.00m, 0.00m, "F", "GST-free")]),
            new ReceiptLineInput(Guid.NewGuid(), "Milk", 1, 4.00m, 4.00m, Voided: false,
                Taxes: [new ReceiptLineTaxInput("GST", 0m, 4.00m, 0.00m, "F", "GST-free")]));

        var receipt = ReceiptRenderer.Render(order, ReceiptLabelSet.Default);

        Assert.Equal(["F = GST-free"], receipt.MarkerLegend);
    }

    [Fact]
    public void Render_PaymentSummary_IncludesEachRecordedPayment()
    {
        var paymentId = Guid.NewGuid();
        var order = new ReceiptOrderInput(
            Guid.NewGuid(), 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            10.00m, 0m, 10.00m,
            Lines: [],
            Payments: [new ReceiptPaymentInput(paymentId, "Cash", 10.00m, DateTimeOffset.UtcNow)],
            Refunds: []);

        var receipt = ReceiptRenderer.Render(order, ReceiptLabelSet.Default);

        var payment = Assert.Single(receipt.Payments);
        Assert.Equal(paymentId, payment.PaymentId);
        Assert.Equal("Cash", payment.Method);
        Assert.Equal(10.00m, payment.AmountApproved);
    }

    [Fact]
    public void Render_RefundSummary_LinksToOriginalPaymentAndOrder()
    {
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var refundId = Guid.NewGuid();
        var order = new ReceiptOrderInput(
            orderId, 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            10.00m, 0m, 10.00m,
            Lines: [],
            Payments: [new ReceiptPaymentInput(paymentId, "Cash", 10.00m, DateTimeOffset.UtcNow)],
            Refunds: [new ReceiptRefundInput(refundId, paymentId, 4.00m, "CustomerRequest", DateTimeOffset.UtcNow)]);

        var receipt = ReceiptRenderer.Render(order, ReceiptLabelSet.Default);

        Assert.Equal(orderId, receipt.OrderId);
        var refund = Assert.Single(receipt.Refunds);
        Assert.Equal(refundId, refund.RefundId);
        Assert.Equal(paymentId, refund.PaymentId);
        Assert.Equal(4.00m, refund.Amount);
        Assert.Equal("CustomerRequest", refund.ReasonCode);
    }

    [Fact]
    public void Render_UsesSuppliedLabelSet_RatherThanHardCodedStrings()
    {
        var order = OrderWith();
        var customLabels = new ReceiptLabelSet("Grand Total", "Tax included");

        var receipt = ReceiptRenderer.Render(order, customLabels);

        Assert.Equal("Grand Total", receipt.TotalLabel);
        Assert.Equal("Tax included", receipt.TaxInclusiveSummaryLabel);
    }

    [Fact]
    public void Render_IsDeterministic_AndTakesNoDependencies()
    {
        var order = OrderWith(
            new ReceiptLineInput(Guid.NewGuid(), "Item", 1, 11.00m, 11.00m, Voided: false,
                Taxes: [new ReceiptLineTaxInput("GST", 10m, 10.00m, 1.00m, null, null)]));

        var first = ReceiptRenderer.Render(order, ReceiptLabelSet.Default);
        var second = ReceiptRenderer.Render(order, ReceiptLabelSet.Default);

        // ReceiptDocument's list-typed properties use reference equality under record-synthesized
        // Equals, so the determinism proof compares scalars and sequence contents explicitly rather
        // than the whole record.
        Assert.Equal(first.SubtotalAmount, second.SubtotalAmount);
        Assert.Equal(first.GrandTotalAmount, second.GrandTotalAmount);
        Assert.Equal(first.TotalTaxAmount, second.TotalTaxAmount);
        Assert.Equal(first.Lines, second.Lines);
        Assert.Equal(first.TaxSummary, second.TaxSummary);
        Assert.Equal(first.MarkerLegend, second.MarkerLegend);
    }

    private static ReceiptOrderInput OrderWith(params ReceiptLineInput[] lines) => new(
        Guid.NewGuid(),
        1,
        DateTimeOffset.UtcNow,
        null,
        SubtotalAmount: lines.Sum(l => l.LineTotalAmount),
        TotalTaxAmount: lines.SelectMany(l => l.Taxes).Sum(t => t.TaxAmount),
        GrandTotalAmount: lines.Sum(l => l.LineTotalAmount),
        Lines: lines,
        Payments: [],
        Refunds: []);
}
