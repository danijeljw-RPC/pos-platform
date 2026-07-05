namespace DaxaPos.Domain.Enums;

/// <summary>
/// Classifies the level a <see cref="Entities.TaxDefinition"/>'s jurisdiction operates at.
/// Country-agnostic by design (ADR-0006): AU/NZ GST are <see cref="Country"/>; future US-style
/// stacked sales tax would use <see cref="State"/>/<see cref="County"/>/<see cref="City"/> on
/// separate <see cref="Entities.TaxDefinition"/> rows, not a special engine mode.
/// </summary>
public enum TaxJurisdictionType
{
    Country = 0,
    Region = 1,
    State = 2,
    County = 3,
    City = 4,
}
