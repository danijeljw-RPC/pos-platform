using DaxaPos.Domain.Enums;

namespace DaxaPos.Domain.Entities;

/// <summary>
/// A system-wide, unfiltered reference tax configuration (e.g. <c>AU_GST_10</c>) — same status as
/// <see cref="Role"/>/<see cref="Permission"/>, never tenant-edited. A tenant clones a
/// <see cref="TaxDefinition"/> from one of these via an explicit endpoint call (Milestone C).
/// </summary>
public class TaxDefinitionTemplate
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string? RegionCode { get; set; }

    public decimal RatePercent { get; set; }

    public string JurisdictionName { get; set; } = string.Empty;

    public TaxJurisdictionType JurisdictionType { get; set; }

    public bool IncludedInPrice { get; set; }

    public TaxRoundingMode RoundingMode { get; set; }

    public int RoundingPrecision { get; set; }

    public TaxCalculationScope CalculationScope { get; set; }

    public string? ReceiptMarkerCode { get; set; }

    public string? ReceiptMarkerLabel { get; set; }

    public string? ReportingCategory { get; set; }

    public bool IsActive { get; set; } = true;
}
