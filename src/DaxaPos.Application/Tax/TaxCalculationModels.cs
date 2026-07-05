using DaxaPos.Domain.Enums;

namespace DaxaPos.Application.Tax;

/// <summary>
/// A resolved <see cref="DaxaPos.Domain.Entities.TaxDefinition"/>'s fields carried by value — the
/// pure engine never touches the entity or the database directly (PLAN-0004 Milestone B).
/// Resolving which <c>TaxDefinition</c>s apply to a product/location (the DB-touching part) is a
/// separate, later step layered on top of this engine, not part of it.
/// </summary>
public sealed record TaxComponentSnapshot(
    Guid TaxDefinitionId,
    string TaxName,
    decimal RatePercent,
    string JurisdictionName,
    TaxJurisdictionType JurisdictionType,
    bool IncludedInPrice,
    TaxRoundingMode RoundingMode,
    int RoundingPrecision);

/// <summary>Input to <see cref="TaxCalculationEngine.CalculateLine"/>: one order line's amount and its resolved tax components.</summary>
public sealed record TaxableLineRequest(decimal LineAmount, IReadOnlyList<TaxComponentSnapshot> Components);

/// <summary>
/// One tax component's calculated result for a line, matching <c>OrderLineTax</c>'s documented
/// shape (ADR-0006): <c>TaxRateId</c> → <see cref="TaxDefinitionId"/>. A 0%-rate component (e.g.
/// GST-free) still produces a fully-populated result with <see cref="TaxAmount"/> of zero — never
/// an absent/skipped line — so receipts can show the marker rather than silently omitting tax data.
/// </summary>
public sealed record TaxLineResult(
    Guid TaxDefinitionId,
    string TaxName,
    decimal RatePercent,
    decimal TaxableAmount,
    decimal TaxAmount,
    string JurisdictionName,
    TaxJurisdictionType JurisdictionType);
