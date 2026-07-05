namespace DaxaPos.Domain.Entities;

/// <summary>
/// Attaches a <see cref="Product"/> to a <see cref="MenuSection"/> (PLAN-0004 Milestone G) — a pure
/// join with no lifecycle beyond assign/unassign, matching <see cref="ProductModifierGroup"/>: no
/// <c>IsActive</c>, since "unassigned" (hard delete) already fully expresses removal.
/// </summary>
public class MenuSectionItem
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid MenuSectionId { get; set; }

    public Guid ProductId { get; set; }

    public int DisplayOrder { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
