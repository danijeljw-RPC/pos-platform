using DaxaPos.Domain.Enums;

namespace DaxaPos.Domain.Entities;

/// <summary>
/// A tenant/organisation-owned semantic label (e.g. <c>Taxable</c>, <c>GSTFree</c>) that a
/// <see cref="Product"/> is assigned to. Does not itself carry a rate — rates live on
/// <see cref="TaxDefinition"/>, connected via <see cref="TaxCategoryDefinition"/>.
/// </summary>
public class TaxCategory
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OrganisationId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public TaxTreatment TaxTreatment { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
