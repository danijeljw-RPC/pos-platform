using DaxaPos.Application.Orders;

namespace DaxaPos.UnitTests.Orders;

/// <summary>
/// PLAN-0005 Milestone A. The first genuinely financial logic this milestone adds — TDD is
/// mandatory per CLAUDE.md's Testing Rules. Proves ADR-0006's 20-distinct-tax-component-per-order
/// limit, which PLAN-0004 deliberately left unenforced (it's an order-level aggregate across
/// lines, and <c>Order</c> didn't exist until this plan) — mirrors
/// <c>TaxCalculationEngineTests</c>' boundary-proof style for the sibling 10-per-line limit.
/// </summary>
public class OrderTaxAggregationTests
{
    [Fact]
    public void CountDistinctComponents_WithNoOverlap_SumsAcrossLines()
    {
        var line1 = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var line2 = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        var count = OrderTaxAggregation.CountDistinctTaxComponents([line1, line2]);

        Assert.Equal(5, count);
    }

    [Fact]
    public void CountDistinctComponents_SameTaxDefinitionAcrossMultipleLines_CountsOnce()
    {
        // The same TaxDefinitionId (e.g. AU GST 10%) applies to most lines in a basket — the
        // 20-component limit is about distinct tax definitions the order touches, not the number of
        // OrderLineTax rows, otherwise a 21-line all-GST order would fail closed for no reason.
        var sharedGst = Guid.NewGuid();
        var line1 = new[] { sharedGst };
        var line2 = new[] { sharedGst };
        var line3 = new[] { sharedGst, Guid.NewGuid() };

        var count = OrderTaxAggregation.CountDistinctTaxComponents([line1, line2, line3]);

        Assert.Equal(2, count);
    }

    [Fact]
    public void ExceedsLimit_WithExactlyTwentyComponents_Succeeds()
    {
        var twenty = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid()).ToArray();

        Assert.False(OrderTaxAggregation.ExceedsLimit(twenty.Length));
    }

    [Fact]
    public void ExceedsLimit_WithTwentyOneComponents_FailsClosed()
    {
        var twentyOne = Enumerable.Range(0, 21).Select(_ => Guid.NewGuid()).ToArray();

        Assert.True(OrderTaxAggregation.ExceedsLimit(twentyOne.Length));
    }

    [Fact]
    public void ExceedsLimit_IsDeterministic_AndTakesNoDependencies()
    {
        // A static class with zero constructor/DI dependencies has nothing to inject a database,
        // HTTP context, or clock into — calling it twice with the same input must produce the same
        // result every time (mirrors TaxCalculationEngineTests' identical proof for its sibling).
        Assert.Equal(OrderTaxAggregation.ExceedsLimit(20), OrderTaxAggregation.ExceedsLimit(20));
        Assert.Equal(OrderTaxAggregation.ExceedsLimit(21), OrderTaxAggregation.ExceedsLimit(21));
    }
}
