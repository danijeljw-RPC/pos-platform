namespace DaxaPos.Application.Payments;

/// <summary>
/// Pure, DB-independent payment/order balance rules (PLAN-0005 Milestone B) — mirrors
/// <see cref="Orders.OrderTaxAggregation"/>'s shape. Never queries the database, never knows about
/// <c>Order</c>/<c>Payment</c> entities directly, has no constructor dependencies to inject —
/// callers pass in already-summed decimal amounts. Enforces the plan's stated rule that a running
/// total of <see cref="Domain.Enums.PaymentStatus.Recorded"/> payments may never exceed an order's
/// grand total (split payments must add up to it exactly, never past it) and that "fully settled"
/// means the recorded total equals the order's grand total exactly.
/// </summary>
public static class PaymentSettlement
{
    public static bool WouldExceedOrderTotal(decimal existingRecordedTotal, decimal newAmount, decimal orderGrandTotal) =>
        existingRecordedTotal + newAmount > orderGrandTotal;

    public static bool IsFullySettled(decimal totalRecordedAfterThisPayment, decimal orderGrandTotal) =>
        totalRecordedAfterThisPayment == orderGrandTotal;
}
