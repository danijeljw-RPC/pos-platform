namespace DaxaPos.Application.Orders;

/// <summary>
/// Pure, DB-independent enforcement of ADR-0006's per-order tax design limit (PLAN-0005 Milestone
/// A) — mirrors <see cref="Application.Tax.TaxCalculationEngine"/>'s per-line sibling limit.
/// PLAN-0004 deliberately left this unenforced (it is an aggregate across all of an order's active
/// lines, and <c>Order</c> didn't exist until now). Never queries the database, never knows about
/// <c>Order</c>/<c>OrderLine</c> entities directly, has no constructor dependencies to inject —
/// callers pass in already-loaded <see cref="Guid"/> collections.
/// </summary>
public static class OrderTaxAggregation
{
    /// <summary>ADR-0006's per-order design limit.</summary>
    public const int MaxComponentsPerOrder = 20;

    /// <summary>
    /// Counts distinct <c>TaxDefinitionId</c>s across all of an order's active lines — the same
    /// tax definition (e.g. AU GST 10%) applying to many lines counts once, matching ADR-0006's
    /// "components the order touches" framing rather than counting <c>OrderLineTax</c> rows.
    /// </summary>
    public static int CountDistinctTaxComponents(IReadOnlyCollection<IReadOnlyCollection<Guid>> perLineTaxDefinitionIds) =>
        perLineTaxDefinitionIds.SelectMany(line => line).Distinct().Count();

    public static bool ExceedsLimit(int distinctTaxComponentCount) => distinctTaxComponentCount > MaxComponentsPerOrder;
}
