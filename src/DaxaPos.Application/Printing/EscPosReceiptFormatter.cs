using System.Text;
using DaxaPos.Application.Receipts;

namespace DaxaPos.Application.Printing;

/// <summary>
/// Pure, DB-independent ESC/POS byte generation from an already-rendered
/// <see cref="ReceiptDocument"/> (PLAN-0005 Milestone D's output) — mirrors
/// <see cref="ReceiptRenderer"/>'s dependency-free shape. Never recomputes tax, price, or marker
/// resolution: every byte comes from the document it is handed. Transport (how the bytes reach a
/// physical printer) is a separate concern in <c>DaxaPos.Infrastructure.Printing</c> — this class
/// only produces bytes, it never sends them anywhere.
/// </summary>
public static class EscPosReceiptFormatter
{
    private const byte Esc = 0x1B;
    private const byte Gs = 0x1D;

    /// <summary>ESC @ — initialise the printer (reset any prior formatting state).</summary>
    public static readonly IReadOnlyList<byte> InitBytes = [Esc, 0x40];

    /// <summary>GS V 0 — full paper cut.</summary>
    public static readonly IReadOnlyList<byte> FullCutBytes = [Gs, 0x56, 0x00];

    /// <summary>
    /// ESC p 0 25 250 — the standard ESC/POS "generate pulse to drawer kick-out connector pin 2"
    /// command. A fixed byte sequence, not configurable — every reference ESC/POS printer
    /// implements this exact command for the standard cash drawer connector.
    /// </summary>
    public static readonly IReadOnlyList<byte> CashDrawerKickBytes = [Esc, 0x70, 0x00, 0x19, 0xFA];

    public static byte[] GenerateCashDrawerKickBytes() => CashDrawerKickBytes.ToArray();

    /// <summary>
    /// Renders <paramref name="document"/> to ESC/POS bytes: init, line items (with tax marker code
    /// next to the price, per ADR-0011 — the product name itself is never replaced), total/tax
    /// summary labels (read verbatim from the document, never hard-coded, per ADR-0011/ADR-0016),
    /// marker legend, payment summary, refund summary, a cash-drawer kick if (and only if) the
    /// order's payments include a <c>Cash</c> payment, then a full cut.
    /// </summary>
    public static byte[] FormatReceipt(ReceiptDocument document)
    {
        var body = new StringBuilder();

        body.Append("Order #").Append(document.OrderNumber).Append('\n');
        body.Append('\n');

        foreach (var line in document.Lines)
        {
            body.Append(line.ProductName);
            if (line.TaxMarkerCode is not null)
            {
                body.Append(' ').Append(line.TaxMarkerCode).Append(' ');
            }
            else
            {
                body.Append(' ');
            }

            body.Append(line.LineTotalAmount.ToString("F2")).Append('\n');
        }

        body.Append(new string('-', 20)).Append('\n');
        body.Append(document.TotalLabel).Append(' ').Append(document.GrandTotalAmount.ToString("F2")).Append('\n');
        body.Append(document.TaxInclusiveSummaryLabel).Append(' ').Append(document.TotalTaxAmount.ToString("F2")).Append('\n');

        if (document.MarkerLegend.Count > 0)
        {
            body.Append('\n');
            foreach (var legend in document.MarkerLegend)
            {
                body.Append(legend).Append('\n');
            }
        }

        if (document.Payments.Count > 0)
        {
            body.Append('\n');
            foreach (var payment in document.Payments)
            {
                body.Append(payment.Method).Append(' ').Append(payment.AmountApproved.ToString("F2")).Append('\n');
            }
        }

        if (document.Refunds.Count > 0)
        {
            body.Append('\n');
            foreach (var refund in document.Refunds)
            {
                body.Append("Refund (").Append(refund.ReasonCode).Append(") ").Append(refund.Amount.ToString("F2")).Append('\n');
            }
        }

        var bytes = new List<byte>();
        bytes.AddRange(InitBytes);
        bytes.AddRange(Encoding.ASCII.GetBytes(body.ToString()));

        // Cash drawer kick is counter behaviour, not a printer-capability concern — only a cash
        // sale should pop the drawer, never a card-only sale.
        var hasCashPayment = document.Payments.Any(payment => string.Equals(payment.Method, "Cash", StringComparison.OrdinalIgnoreCase));
        if (hasCashPayment)
        {
            bytes.AddRange(CashDrawerKickBytes);
        }

        bytes.AddRange(FullCutBytes);

        return bytes.ToArray();
    }
}
