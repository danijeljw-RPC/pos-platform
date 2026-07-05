namespace DaxaPos.Domain.Entities;

/// <summary>
/// <see cref="PriceDelta"/> is a delta on the resolved price (<c>+</c>/<c>-</c>), not an absolute
/// price — an "Extra shot" modifier might be <c>+1.00</c> (PLAN-0004 Milestone E Domain
/// Assumptions). No <see cref="OrganisationId"/> column of its own — scoped entirely through
/// <see cref="ModifierGroupId"/>, matching <see cref="ProductVariant"/>'s pattern.
/// </summary>
public class Modifier
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid ModifierGroupId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal PriceDelta { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
