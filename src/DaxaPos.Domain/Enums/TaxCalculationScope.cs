namespace DaxaPos.Domain.Enums;

/// <summary>
/// Whether a <see cref="Entities.TaxDefinition"/>'s components are calculated per order line
/// (MVP; the only value the engine exercises today) or per stacked component across a future
/// multi-jurisdiction basket — see ADR-0006's Acceptance Addendum. Not yet read by
/// <see cref="Application.Tax.TaxCalculationEngine"/>: <c>Order</c> doesn't exist until PLAN-0005,
/// which is where cross-line aggregation by scope would be exercised.
/// </summary>
public enum TaxCalculationScope
{
    PerLine = 0,
    PerComponent = 1,
}
