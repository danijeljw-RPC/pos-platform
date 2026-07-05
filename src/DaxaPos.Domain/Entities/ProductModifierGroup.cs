namespace DaxaPos.Domain.Entities;

/// <summary>
/// Attaches a <see cref="ModifierGroup"/> to a <see cref="Product"/> (PLAN-0004 Milestone E) — a
/// pure join with no lifecycle beyond assign/unassign (create/hard-delete), unlike every other
/// catalogue entity in this plan: no <see cref="Product"/>-style <c>IsActive</c>/archive state,
/// since "unassigned" already fully expresses removal.
/// </summary>
public class ProductModifierGroup
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid ProductId { get; set; }

    public Guid ModifierGroupId { get; set; }

    public int DisplayOrder { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
