using DaxaPos.Application.Tax;
using DaxaPos.Domain.Enums;

namespace DaxaPos.UnitTests.Tax;

/// <summary>
/// PLAN-0004 Milestone B. The first genuinely financial logic in the codebase — TDD is mandatory
/// per CLAUDE.md's Testing Rules. Reproduces the CLAUDE.md/ADR-0006 AU mixed-basket worked example
/// byte-for-byte ($5.50/$8.80/$6.00 → $20.30 total, $1.30 GST).
/// </summary>
public class TaxCalculationEngineTests
{
    [Fact]
    public void CalculateLine_AuGst10Inclusive_FiveFiftyDollarItem_ExtractsFiftyCentsGst()
    {
        var result = TaxCalculationEngine.CalculateLine(new TaxableLineRequest(5.50m, [AuGst10Component()]));

        Assert.True(result.IsSuccess);
        var line = Assert.Single(result.TaxLines);
        Assert.Equal(0.50m, line.TaxAmount);
        Assert.Equal(5.00m, line.TaxableAmount);
        Assert.Equal(0.50m, result.TotalTaxAmount);
    }

    [Fact]
    public void CalculateLine_AuMixedBasket_TotalGstAcrossThreeLinesEqualsOneThirty()
    {
        var flatWhite = TaxCalculationEngine.CalculateLine(new TaxableLineRequest(5.50m, [AuGst10Component()]));
        var cakeSlice = TaxCalculationEngine.CalculateLine(new TaxableLineRequest(8.80m, [AuGst10Component()]));
        var bread = TaxCalculationEngine.CalculateLine(new TaxableLineRequest(6.00m, [AuGstFreeComponent()]));

        var basketTotal = flatWhite.LineAmount + cakeSlice.LineAmount + bread.LineAmount;
        var basketGst = flatWhite.TotalTaxAmount + cakeSlice.TotalTaxAmount + bread.TotalTaxAmount;

        Assert.Equal(20.30m, basketTotal);
        Assert.Equal(1.30m, basketGst);
        Assert.Equal(0.50m, flatWhite.TotalTaxAmount);
        Assert.Equal(0.80m, cakeSlice.TotalTaxAmount);
        Assert.Equal(0.00m, bread.TotalTaxAmount);
    }

    [Fact]
    public void CalculateLine_GstFreeLine_ProducesAZeroTaxLineResult_NotAnAbsentLine()
    {
        var result = TaxCalculationEngine.CalculateLine(new TaxableLineRequest(6.00m, [AuGstFreeComponent()]));

        Assert.True(result.IsSuccess);
        var line = Assert.Single(result.TaxLines);
        Assert.Equal(0m, line.TaxAmount);
        Assert.Equal(6.00m, line.TaxableAmount);
    }

    [Fact]
    public void CalculateLine_NzGst15Inclusive_ElevenFiftyDollarItem_ExtractsOneFiftyGst()
    {
        var result = TaxCalculationEngine.CalculateLine(new TaxableLineRequest(11.50m, [NzGst15Component()]));

        Assert.True(result.IsSuccess);
        var line = Assert.Single(result.TaxLines);
        Assert.Equal(1.50m, line.TaxAmount);
        Assert.Equal(10.00m, line.TaxableAmount);
    }

    [Fact]
    public void CalculateLine_ExclusiveComponent_AddsTaxOnTopOfTheLineAmount_InsteadOfExtractingIt()
    {
        var exclusiveTenPercent = new TaxComponentSnapshot(
            Guid.NewGuid(), "State Sales Tax", 10m, "Some State", TaxJurisdictionType.State,
            IncludedInPrice: false, TaxRoundingMode.NearestCent, RoundingPrecision: 2);

        var result = TaxCalculationEngine.CalculateLine(new TaxableLineRequest(100.00m, [exclusiveTenPercent]));

        Assert.True(result.IsSuccess);
        var line = Assert.Single(result.TaxLines);
        Assert.Equal(100.00m, line.TaxableAmount);
        Assert.Equal(10.00m, line.TaxAmount);
    }

    [Fact]
    public void CalculateLine_RoundsHalfAwayFromZero_AtTheConfiguredPrecision()
    {
        // 1.65 * 10% = 0.165 exactly - a genuine midpoint at 2 decimal places. Away-from-zero
        // rounds to 0.17; .NET's default banker's rounding would round to 0.16 since 6 is already
        // even. This proves the engine's rounding choice deliberately, not by CLR-default accident.
        var exclusiveTenPercent = new TaxComponentSnapshot(
            Guid.NewGuid(), "Test Tax", 10m, "Test Jurisdiction", TaxJurisdictionType.Country,
            IncludedInPrice: false, TaxRoundingMode.NearestCent, RoundingPrecision: 2);

        var result = TaxCalculationEngine.CalculateLine(new TaxableLineRequest(1.65m, [exclusiveTenPercent]));

        var line = Assert.Single(result.TaxLines);
        Assert.Equal(0.17m, line.TaxAmount);
    }

    [Fact]
    public void CalculateLine_WithNoComponents_FailsClosed_InsteadOfSilentlyReturningZeroTax()
    {
        var result = TaxCalculationEngine.CalculateLine(new TaxableLineRequest(10.00m, []));

        Assert.False(result.IsSuccess);
        Assert.Equal(TaxCalculationErrorCode.MissingTaxConfiguration, result.ErrorCode);
        Assert.Empty(result.TaxLines);
    }

    [Fact]
    public void CalculateLine_WithExactlyTenComponents_Succeeds()
    {
        var tenComponents = Enumerable.Range(0, 10).Select(_ => AuGst10Component()).ToList();

        var result = TaxCalculationEngine.CalculateLine(new TaxableLineRequest(100.00m, tenComponents));

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.TaxLines.Count);
    }

    [Fact]
    public void CalculateLine_WithMoreThanTenComponents_FailsClosed_InsteadOfThrowing()
    {
        var elevenComponents = Enumerable.Range(0, 11).Select(_ => AuGst10Component()).ToList();

        var result = TaxCalculationEngine.CalculateLine(new TaxableLineRequest(10.00m, elevenComponents));

        Assert.False(result.IsSuccess);
        Assert.Equal(TaxCalculationErrorCode.TooManyTaxComponents, result.ErrorCode);
    }

    [Fact]
    public void CalculateLine_IsDeterministic_AndTakesNoDependencies()
    {
        // A static class with zero constructor/DI dependencies has nothing to inject a database,
        // HTTP context, or clock into — calling it twice with the same input must produce
        // equal-by-value output every time.
        var request = new TaxableLineRequest(5.50m, [AuGst10Component()]);

        var first = TaxCalculationEngine.CalculateLine(request);
        var second = TaxCalculationEngine.CalculateLine(request);

        Assert.Equal(first.TaxLines, second.TaxLines);
        Assert.Equal(first.TotalTaxAmount, second.TotalTaxAmount);
    }

    private static TaxComponentSnapshot AuGst10Component() => new(
        Guid.NewGuid(), "GST", 10m, "Australia", TaxJurisdictionType.Country,
        IncludedInPrice: true, TaxRoundingMode.NearestCent, RoundingPrecision: 2);

    private static TaxComponentSnapshot AuGstFreeComponent() => new(
        Guid.NewGuid(), "GST", 0m, "Australia", TaxJurisdictionType.Country,
        IncludedInPrice: true, TaxRoundingMode.NearestCent, RoundingPrecision: 2);

    private static TaxComponentSnapshot NzGst15Component() => new(
        Guid.NewGuid(), "GST", 15m, "New Zealand", TaxJurisdictionType.Country,
        IncludedInPrice: true, TaxRoundingMode.NearestCent, RoundingPrecision: 2);
}
