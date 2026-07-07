using DaxaPos.Api.Authorization;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Application.Receipts;
using DaxaPos.Domain.Entities;
using DaxaPos.Domain.Events;
using DaxaPos.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DaxaPos.Api.Endpoints.Receipts;

public sealed record ReceiptLineResponse(string ProductName, int Quantity, decimal LineTotalAmount, string? TaxMarkerCode);

public sealed record ReceiptTaxSummaryResponse(string TaxName, decimal RatePercent, decimal TaxableAmount, decimal TaxAmount);

public sealed record ReceiptPaymentResponse(Guid PaymentId, string Method, decimal AmountApproved, DateTimeOffset RecordedAtUtc);

public sealed record ReceiptRefundResponse(Guid RefundId, Guid PaymentId, decimal Amount, string ReasonCode, DateTimeOffset RecordedAtUtc);

public sealed record ReceiptResponse(
    Guid OrderId,
    long OrderNumber,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    IReadOnlyList<ReceiptLineResponse> Lines,
    decimal SubtotalAmount,
    string TotalLabel,
    decimal GrandTotalAmount,
    IReadOnlyList<ReceiptTaxSummaryResponse> TaxSummary,
    string TaxInclusiveSummaryLabel,
    decimal TotalTaxAmount,
    IReadOnlyList<string> MarkerLegend,
    IReadOnlyList<ReceiptPaymentResponse> Payments,
    IReadOnlyList<ReceiptRefundResponse> Refunds)
{
    public static ReceiptResponse FromDocument(ReceiptDocument document) => new(
        document.OrderId,
        document.OrderNumber,
        document.OpenedAtUtc,
        document.ClosedAtUtc,
        document.Lines.Select(line => new ReceiptLineResponse(line.ProductName, line.Quantity, line.LineTotalAmount, line.TaxMarkerCode)).ToList(),
        document.SubtotalAmount,
        document.TotalLabel,
        document.GrandTotalAmount,
        document.TaxSummary.Select(tax => new ReceiptTaxSummaryResponse(tax.TaxName, tax.RatePercent, tax.TaxableAmount, tax.TaxAmount)).ToList(),
        document.TaxInclusiveSummaryLabel,
        document.TotalTaxAmount,
        document.MarkerLegend,
        document.Payments.Select(payment => new ReceiptPaymentResponse(payment.PaymentId, payment.Method, payment.AmountApproved, payment.RecordedAtUtc)).ToList(),
        document.Refunds.Select(refund => new ReceiptRefundResponse(refund.RefundId, refund.PaymentId, refund.Amount, refund.ReasonCode, refund.RecordedAtUtc)).ToList());
}

/// <summary>
/// Receipt generation foundation (PLAN-0005 Milestone D) — a pure projection over an already-loaded
/// <see cref="Order"/>'s lines/tax snapshots/payments/refunds via <see cref="ReceiptRenderer"/>, not
/// a print-ready byte stream (ESC/POS generation is Milestone E's job) and not a persisted row (no
/// <c>Receipt</c> table exists — regenerated from immutable source data every time, per ADR-0010's
/// "PDF Generation Strategy" pattern). Live-sale viewing (<see cref="GetAsync"/>) stays under
/// <c>orders.manage</c>, unaudited; the standalone after-the-fact reprint action
/// (<see cref="ReprintAsync"/>) is gated <c>receipts.reprint</c> and audited (CLAUDE.md's explicit
/// "receipt reprints must be audited" requirement).
/// </summary>
public static class ReceiptEndpoints
{
    public static void MapReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders/{orderId:guid}/receipt").RequireAuthorization();

        group.MapGet("", GetAsync).RequirePermission(Permissions.OrdersManage);
        group.MapPost("/reprint", ReprintAsync).RequirePermission(Permissions.ReceiptsReprint);
    }

    private static async Task<IResult> GetAsync(Guid orderId, IAuthContextAccessor authContextAccessor, DaxaDbContext dbContext)
    {
        var authContext = authContextAccessor.Current!;
        var order = await LoadAuthorizedOrderAsync(dbContext, orderId, authContext);
        if (order is null)
        {
            return Results.NotFound();
        }

        var document = await BuildReceiptDocumentAsync(dbContext, order);
        return Results.Ok(ReceiptResponse.FromDocument(document));
    }

    private static async Task<IResult> ReprintAsync(
        Guid orderId,
        IAuthContextAccessor authContextAccessor,
        DaxaDbContext dbContext,
        IDomainEventDispatcher dispatcher)
    {
        var authContext = authContextAccessor.Current!;
        var order = await LoadAuthorizedOrderAsync(dbContext, orderId, authContext);
        if (order is null)
        {
            return Results.NotFound();
        }

        var document = await BuildReceiptDocumentAsync(dbContext, order);

        await dispatcher.DispatchAsync(new ReceiptReprintedDomainEvent(
            order.TenantId,
            order.OrganisationId,
            order.Id,
            authContext.UserId,
            authContext.StaffMemberId,
            DateTimeOffset.UtcNow));

        return Results.Ok(ReceiptResponse.FromDocument(document));
    }

    /// <summary>
    /// Loads the order's active/voided lines, tax snapshots, payments, and refunds, and hands them
    /// to <see cref="ReceiptRenderer"/> as already-loaded snapshots — the renderer itself never
    /// touches the database (ADR-0006, ADR-0010).
    /// </summary>
    private static async Task<ReceiptDocument> BuildReceiptDocumentAsync(DaxaDbContext dbContext, Order order)
    {
        var lines = await dbContext.OrderLines
            .Where(line => line.OrderId == order.Id)
            .ToListAsync();

        var lineIds = lines.Select(line => line.Id).ToList();
        var taxesByLineId = (await dbContext.OrderLineTaxes
                .Where(tax => lineIds.Contains(tax.OrderLineId))
                .ToListAsync())
            .GroupBy(tax => tax.OrderLineId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var payments = await dbContext.Payments
            .Where(payment => payment.OrderId == order.Id)
            .OrderBy(payment => payment.RecordedAtUtc)
            .ToListAsync();

        var paymentIds = payments.Select(payment => payment.Id).ToList();
        var refunds = await dbContext.Refunds
            .Where(refund => paymentIds.Contains(refund.PaymentId))
            .OrderBy(refund => refund.RecordedAtUtc)
            .ToListAsync();

        var receiptOrder = new ReceiptOrderInput(
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
                line.Status == Domain.Enums.OrderLineStatus.Voided,
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

        return ReceiptRenderer.Render(receiptOrder, ReceiptLabelSet.Default);
    }

    /// <summary>Context provenance (ADR-0015): same organisation/location/terminal-bound-session
    /// rule as <see cref="Payments.PaymentEndpoints"/>'s identical helper (Milestone C.2 added the
    /// TerminalId check — a staff/device session for Terminal A must not be able to view or
    /// reprint the receipt for Terminal B's order at the same location).</summary>
    private static async Task<Order?> LoadAuthorizedOrderAsync(DaxaDbContext dbContext, Guid orderId, AuthContext authContext)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(o => o.Id == orderId);
        if (order is null || order.OrganisationId != authContext.OrganisationId)
        {
            return null;
        }

        if (authContext.LocationId is not null && authContext.LocationId != order.LocationId)
        {
            return null;
        }

        if (authContext.LocationId is not null && (authContext.TerminalId is null || authContext.TerminalId != order.TerminalId))
        {
            return null;
        }

        return order;
    }
}
