namespace DaxaPos.Domain.Enums;

/// <summary>
/// Reporting/receipt-marker-defaulting metadata on a <see cref="Entities.TaxCategory"/> — per
/// ADR-0006's Global Tax Design Constraints, this never drives calculation branching directly;
/// calculation only ever reads the resolved <see cref="Entities.TaxDefinition.RatePercent"/> etc.
/// </summary>
public enum TaxTreatment
{
    Taxable = 0,
    GSTFree = 1,
    ZeroRated = 2,
    Exempt = 3,
}
