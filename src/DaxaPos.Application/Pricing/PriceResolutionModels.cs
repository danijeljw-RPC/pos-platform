namespace DaxaPos.Application.Pricing;

/// <summary>
/// Output of <see cref="PriceResolver.Resolve"/> — a fully resolved amount plus whether it should
/// be interpreted as tax-inclusive (from <see cref="Domain.Entities.VenueTaxConfiguration.TaxInclusivePricing"/>).
/// </summary>
public sealed record ResolvedPrice(decimal Amount, bool IsTaxInclusive);
