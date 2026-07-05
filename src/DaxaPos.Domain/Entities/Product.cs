namespace DaxaPos.Domain.Entities;

/// <summary>
/// <see cref="IsActive"/> and <see cref="IsArchived"/> are independent flags (PLAN-0004 Milestone D
/// Domain Assumptions): an active-but-later-archived product and an explicitly-deactivated-but-not-
/// archived product are different states. <see cref="IsArchived"/> is set only by the
/// tax-category-changing archive-and-replace flow (OI-0007) — never by the plain deactivate/
/// reactivate toggle — and, once set, is permanent: <see cref="SupersededByProductId"/> points at
/// the replacement row created in the same operation.
/// </summary>
public class Product
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OrganisationId { get; set; }

    public Guid ProductCategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Sku { get; set; }

    public string? Barcode { get; set; }

    public Guid TaxCategoryId { get; set; }

    public decimal BasePrice { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsArchived { get; set; }

    public DateTimeOffset? ArchivedAtUtc { get; set; }

    public Guid? SupersededByProductId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
