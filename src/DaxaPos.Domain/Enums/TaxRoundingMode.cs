namespace DaxaPos.Domain.Enums;

/// <summary>
/// Rounding behaviour for a <see cref="Entities.TaxDefinition"/>, per ADR-0006's Acceptance
/// Addendum: rounding belongs to the configured tax definition, not hard-coded application logic.
/// </summary>
public enum TaxRoundingMode
{
    /// <summary>Round to the configured precision, half away from zero.</summary>
    NearestCent = 0,
}
