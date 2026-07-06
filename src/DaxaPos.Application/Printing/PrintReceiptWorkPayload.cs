namespace DaxaPos.Application.Printing;

/// <summary>
/// The <c>"PrintReceipt"</c> outbox work item's payload (PLAN-0005 Milestone E) — just the order id
/// to render and print a receipt for. The worker re-derives everything else (lines, tax snapshots,
/// payments, refunds) from persisted data at processing time, per ADR-0010's "PDF Generation
/// Strategy" pattern; nothing here is itself a financial snapshot.
/// </summary>
public sealed record PrintReceiptWorkPayload(Guid OrderId)
{
    public const string WorkType = "PrintReceipt";
}
