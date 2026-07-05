using DaxaPos.Domain.Enums;

namespace DaxaPos.Domain.Entities;

/// <summary>
/// A tenant-owned tax configuration, optionally cloned from a <see cref="TaxDefinitionTemplate"/>
/// (see <see cref="SourceTemplateCode"/>). Independently editable per OI-0007's closed Decision —
/// one tenant's rate edit must never leak into another tenant's, which rules out sharing the
/// global template row directly.
/// </summary>
public class TaxDefinition
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OrganisationId { get; set; }

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

    /// <summary>The <see cref="TaxDefinitionTemplate.Code"/> this row was cloned from, if any — traceability only, not a foreign key (the template may change independently later).</summary>
    public string? SourceTemplateCode { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
