using DaxaPos.Domain.Enums;

namespace DaxaPos.Domain.Entities;

/// <summary>
/// One row per <see cref="Application.Tax.TaxLineResult"/> returned by
/// <see cref="Application.Tax.TaxCalculationEngine.CalculateLine"/> for an <see cref="OrderLine"/>
/// (PLAN-0005 Milestone A) — stored verbatim, never recalculated later, matching ADR-0006's
/// documented <c>OrderLineTax</c> shape. <see cref="ReceiptMarkerCodeSnapshot"/>/
/// <see cref="ReceiptMarkerLabelSnapshot"/> carry <see cref="TaxDefinition.ReceiptMarkerCode"/>/
/// <see cref="TaxDefinition.ReceiptMarkerLabel"/> at calculation time, so a later edit to the tax
/// definition's marker (or a future ADR-0016 translation of it) cannot retroactively change a
/// historical receipt's marker (ADR-0011).
/// </summary>
public class OrderLineTax
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OrderLineId { get; set; }

    public Guid TaxDefinitionId { get; set; }

    public string TaxNameSnapshot { get; set; } = string.Empty;

    public decimal RatePercentSnapshot { get; set; }

    public decimal TaxableAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public string JurisdictionNameSnapshot { get; set; } = string.Empty;

    public TaxJurisdictionType JurisdictionTypeSnapshot { get; set; }

    public string? ReceiptMarkerCodeSnapshot { get; set; }

    public string? ReceiptMarkerLabelSnapshot { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
