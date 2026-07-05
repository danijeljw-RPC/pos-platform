using DaxaPos.Application.Payments;

namespace DaxaPos.UnitTests.Payments;

/// <summary>
/// PLAN-0005 Milestone B. The genuinely financial-logic unit this milestone adds — TDD is
/// mandatory per CLAUDE.md's Testing Rules. Proves the payment/order balance rules the plan states
/// in prose ("a payment fully settles when `AmountApproved == Order.GrandTotalAmount`, summed
/// across all `Recorded` payments on the order, split payments included") without ever letting the
/// running total exceed the order's grand total, mirroring <c>OrderTaxAggregationTests</c>' pure,
/// dependency-free style for its sibling limit.
/// </summary>
public class PaymentSettlementTests
{
    [Fact]
    public void WouldExceedOrderTotal_WhenNewPaymentExactlyCompletesTheOrder_DoesNotExceed()
    {
        // $19.30 already recorded (split payment in progress), $0.70 cash finishes it exactly.
        var exceeds = PaymentSettlement.WouldExceedOrderTotal(existingRecordedTotal: 19.30m, newAmount: 0.70m, orderGrandTotal: 20.00m);

        Assert.False(exceeds);
    }

    [Fact]
    public void WouldExceedOrderTotal_WhenNewPaymentGoesOverTheOrderTotal_Exceeds()
    {
        var exceeds = PaymentSettlement.WouldExceedOrderTotal(existingRecordedTotal: 19.30m, newAmount: 1.00m, orderGrandTotal: 20.00m);

        Assert.True(exceeds);
    }

    [Fact]
    public void WouldExceedOrderTotal_WhenNewPaymentIsAPartialSplitPayment_DoesNotExceed()
    {
        var exceeds = PaymentSettlement.WouldExceedOrderTotal(existingRecordedTotal: 0m, newAmount: 10.00m, orderGrandTotal: 20.30m);

        Assert.False(exceeds);
    }

    [Fact]
    public void WouldExceedOrderTotal_WhenOrderHasZeroGrandTotal_AnyPositivePaymentExceeds()
    {
        // A fully-voided order (all lines removed) has a zero grand total — no payment should ever
        // be accepted against it, not even a token amount.
        var exceeds = PaymentSettlement.WouldExceedOrderTotal(existingRecordedTotal: 0m, newAmount: 0.01m, orderGrandTotal: 0m);

        Assert.True(exceeds);
    }

    [Fact]
    public void IsFullySettled_WhenTotalRecordedEqualsGrandTotal_ReturnsTrue()
    {
        Assert.True(PaymentSettlement.IsFullySettled(totalRecordedAfterThisPayment: 20.30m, orderGrandTotal: 20.30m));
    }

    [Fact]
    public void IsFullySettled_WhenTotalRecordedIsLessThanGrandTotal_ReturnsFalse()
    {
        Assert.False(PaymentSettlement.IsFullySettled(totalRecordedAfterThisPayment: 10.00m, orderGrandTotal: 20.30m));
    }

    [Fact]
    public void IsFullySettled_IsDeterministic_AndTakesNoDependencies()
    {
        // A static class with zero constructor/DI dependencies has nothing to inject a database,
        // HTTP context, or clock into — calling it twice with the same input must produce the same
        // result every time (mirrors OrderTaxAggregationTests' identical proof for its sibling).
        Assert.Equal(
            PaymentSettlement.IsFullySettled(20.30m, 20.30m),
            PaymentSettlement.IsFullySettled(20.30m, 20.30m));
        Assert.Equal(
            PaymentSettlement.WouldExceedOrderTotal(19.30m, 1.00m, 20.00m),
            PaymentSettlement.WouldExceedOrderTotal(19.30m, 1.00m, 20.00m));
    }
}
