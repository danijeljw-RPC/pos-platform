using DaxaPos.Domain.Enums;

namespace DaxaPos.Application.Tax;

/// <summary>
/// Pure, DB-independent tax calculation engine (PLAN-0004 Milestone B, ADR-0006). Takes
/// already-resolved <see cref="TaxComponentSnapshot"/> inputs and a line amount, returns tax line
/// results. Never queries the database, never knows about <c>Product</c>/<c>Order</c>, has no
/// constructor dependencies to inject — nothing here can access the database, HTTP, orders,
/// payments, receipts, or session state even by accident.
/// </summary>
public static class TaxCalculationEngine
{
    /// <summary>ADR-0006's per-line design limit.</summary>
    public const int MaxComponentsPerLine = 10;

    public static TaxLineCalculationResult CalculateLine(TaxableLineRequest request)
    {
        if (request.Components.Count == 0)
        {
            return TaxLineCalculationResult.Failure(TaxCalculationErrorCode.MissingTaxConfiguration);
        }

        if (request.Components.Count > MaxComponentsPerLine)
        {
            return TaxLineCalculationResult.Failure(TaxCalculationErrorCode.TooManyTaxComponents);
        }

        var taxLines = request.Components
            .Select(component => CalculateComponent(request.LineAmount, component))
            .ToList();

        return TaxLineCalculationResult.Success(request.LineAmount, taxLines);
    }

    private static TaxLineResult CalculateComponent(decimal lineAmount, TaxComponentSnapshot component)
    {
        decimal taxAmount;
        decimal taxableAmount;

        if (component.IncludedInPrice)
        {
            // Tax is already included in lineAmount; extract it: rate/(100+rate) of the
            // inclusive amount. E.g. $5.50 at 10% GST -> $5.50 * 10/110 = $0.50.
            taxAmount = Round(lineAmount * component.RatePercent / (100m + component.RatePercent), component);
            taxableAmount = lineAmount - taxAmount;
        }
        else
        {
            // Tax is added on top of lineAmount (e.g. a stacked US-style exclusive sales tax).
            taxAmount = Round(lineAmount * component.RatePercent / 100m, component);
            taxableAmount = lineAmount;
        }

        return new TaxLineResult(
            component.TaxDefinitionId,
            component.TaxName,
            component.RatePercent,
            taxableAmount,
            taxAmount,
            component.JurisdictionName,
            component.JurisdictionType);
    }

    private static decimal Round(decimal value, TaxComponentSnapshot component) =>
        component.RoundingMode switch
        {
            TaxRoundingMode.NearestCent => Math.Round(value, component.RoundingPrecision, MidpointRounding.AwayFromZero),
            _ => throw new ArgumentOutOfRangeException(nameof(component), component.RoundingMode, "Unsupported tax rounding mode."),
        };
}
