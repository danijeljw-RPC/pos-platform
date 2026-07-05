namespace DaxaPos.Application.Payments;

/// <summary>
/// Pure, DB-independent refund/payment balance rule (PLAN-0005 Milestone C) — mirrors
/// <see cref="PaymentSettlement"/>'s shape. Never queries the database, never knows about
/// <c>Payment</c>/<c>Refund</c> entities directly, has no constructor dependencies to inject —
/// callers pass in already-summed decimal amounts. Enforces the plan's stated rule that a refund's
/// amount, added to the running total of existing refunds already recorded against the same
/// payment, may never exceed <c>Payment.AmountApproved</c> (full and partial refunds both add up
/// against this same ceiling, never past it).
/// </summary>
public static class RefundSettlement
{
    public static bool WouldExceedRefundableAmount(decimal existingRefundedTotal, decimal newAmount, decimal paymentApprovedAmount) =>
        existingRefundedTotal + newAmount > paymentApprovedAmount;
}
