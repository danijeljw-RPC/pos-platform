using DaxaPos.Domain.Enums;

namespace DaxaPos.Domain.Entities;

/// <summary>
/// One row per <see cref="Location"/> (PLAN-0004 Milestone F) — absence means "not yet
/// configured." Per the plan's approved Human Decision #5, absence must 404/fail closed rather
/// than silently default to an AU-flavoured configuration, which would violate the tax engine's
/// country-agnostic design (ADR-0006). No <c>OrganisationId</c> column of its own — derived via
/// <see cref="LocationId"/>, matching <see cref="ProductLocationOverride"/>. <see cref="TaxCalculationMode"/>
/// reuses <see cref="TaxCalculationScope"/> (the same per-line/per-component concept already on
/// <see cref="TaxDefinition"/>) rather than a near-duplicate enum with identical values.
/// </summary>
public class VenueTaxConfiguration
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid LocationId { get; set; }

    public bool TaxInclusivePricing { get; set; }

    public TaxCalculationScope TaxCalculationMode { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
