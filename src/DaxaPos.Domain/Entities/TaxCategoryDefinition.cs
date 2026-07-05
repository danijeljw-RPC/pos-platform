namespace DaxaPos.Domain.Entities;

/// <summary>
/// Maps a <see cref="TaxCategory"/> to the <see cref="TaxDefinition"/> it resolves to, optionally
/// scoped to one <see cref="Location"/> (null = organisation-wide) — this is what lets a
/// multi-location tenant spanning AU and NZ locations share one "Taxable" category while
/// resolving to a different tax definition per location's jurisdiction, without per-country
/// product duplication. A pure mapping row, not itself a financial record (ADR-0010) — unlike
/// <see cref="TaxCategory"/>/<see cref="TaxDefinition"/>, it supports hard delete (Milestone C).
/// </summary>
public class TaxCategoryDefinition
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid TaxCategoryId { get; set; }

    public Guid TaxDefinitionId { get; set; }

    public Guid? LocationId { get; set; }

    public int Priority { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
