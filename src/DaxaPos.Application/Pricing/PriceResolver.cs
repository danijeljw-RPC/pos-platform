using DaxaPos.Domain.Entities;

namespace DaxaPos.Application.Pricing;

/// <summary>
/// Pure, DB-independent price resolver (PLAN-0004 Milestone F, ADR-0006's sibling for pricing).
/// Takes already-loaded entities by reference and returns a resolved price — never queries the
/// database, never knows about <c>Order</c>/HTTP/session state, and has no constructor
/// dependencies to inject. Resolution order: <see cref="Product.BasePrice"/> -> variant delta ->
/// modifier deltas -> <see cref="ProductLocationOverride.PriceOverride"/> (replaces the computed
/// total outright, never adds to it) -> tax-inclusive/exclusive mode from
/// <see cref="VenueTaxConfiguration"/>, which fails closed when absent rather than silently
/// defaulting (the plan's approved Human Decision #5, applied here the same way
/// <c>TaxCalculationEngine</c> fails closed on missing tax configuration).
/// </summary>
public static class PriceResolver
{
    public static PriceResolutionResult Resolve(
        Product product,
        ProductVariant? variant,
        IReadOnlyList<Modifier> modifiers,
        ProductLocationOverride? locationOverride,
        VenueTaxConfiguration? venueTaxConfiguration)
    {
        if (venueTaxConfiguration is null)
        {
            return PriceResolutionResult.Failure(PriceResolutionErrorCode.MissingVenueTaxConfiguration);
        }

        var amount = locationOverride?.PriceOverride
            ?? product.BasePrice + (variant?.PriceDelta ?? 0m) + modifiers.Sum(m => m.PriceDelta);

        return PriceResolutionResult.Success(new ResolvedPrice(amount, venueTaxConfiguration.TaxInclusivePricing));
    }
}
