using DaxaPos.Application.Payments;

namespace DaxaPos.UnitTests.Payments;

/// <summary>
/// PLAN-0005 Milestone C. The genuinely financial-logic unit this milestone adds — TDD is
/// mandatory per CLAUDE.md's Testing Rules. Proves the refund/payment balance rule the plan states
/// in prose ("<c>Amount &lt;= Payment.AmountApproved - Sum(existing refunds against that
/// payment)</c>, enforced server-side") without ever letting the running refunded total exceed the
/// payment's approved amount, mirroring <see cref="PaymentSettlementTests"/>' pure,
/// dependency-free style for its sibling limit.
/// </summary>
public class RefundSettlementTests
{
    [Fact]
    public void WouldExceedRefundableAmount_WhenNewRefundExactlyCompletesFullRefund_DoesNotExceed()
    {
        // $15.00 already refunded (partial refund in progress), $5.30 finishes it exactly.
        var exceeds = RefundSettlement.WouldExceedRefundableAmount(existingRefundedTotal: 15.00m, newAmount: 5.30m, paymentApprovedAmount: 20.30m);

        Assert.False(exceeds);
    }

    [Fact]
    public void WouldExceedRefundableAmount_WhenNewRefundGoesOverPaymentAmount_Exceeds()
    {
        var exceeds = RefundSettlement.WouldExceedRefundableAmount(existingRefundedTotal: 15.00m, newAmount: 6.00m, paymentApprovedAmount: 20.30m);

        Assert.True(exceeds);
    }

    [Fact]
    public void WouldExceedRefundableAmount_WhenNewRefundIsAPartialRefund_DoesNotExceed()
    {
        var exceeds = RefundSettlement.WouldExceedRefundableAmount(existingRefundedTotal: 0m, newAmount: 10.00m, paymentApprovedAmount: 20.30m);

        Assert.False(exceeds);
    }

    [Fact]
    public void WouldExceedRefundableAmount_WhenPaymentIsAlreadyFullyRefunded_AnyPositiveRefundExceeds()
    {
        var exceeds = RefundSettlement.WouldExceedRefundableAmount(existingRefundedTotal: 20.30m, newAmount: 0.01m, paymentApprovedAmount: 20.30m);

        Assert.True(exceeds);
    }

    [Fact]
    public void WouldExceedRefundableAmount_IsDeterministic_AndTakesNoDependencies()
    {
        // A static class with zero constructor/DI dependencies has nothing to inject a database,
        // HTTP context, or clock into — calling it twice with the same input must produce the same
        // result every time (mirrors PaymentSettlementTests' identical proof for its sibling).
        Assert.Equal(
            RefundSettlement.WouldExceedRefundableAmount(15.00m, 5.30m, 20.30m),
            RefundSettlement.WouldExceedRefundableAmount(15.00m, 5.30m, 20.30m));
    }
}
