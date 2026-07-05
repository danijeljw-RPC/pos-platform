namespace DaxaPos.Domain.Entities;

/// <summary>No <see cref="OrganisationId"/> column of its own — scoped entirely through <see cref="MenuId"/>, matching the <see cref="ProductVariant"/> precedent.</summary>
public class MenuSection
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid MenuId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
