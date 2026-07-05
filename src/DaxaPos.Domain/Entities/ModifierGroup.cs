namespace DaxaPos.Domain.Entities;

public class ModifierGroup
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid OrganisationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SelectionMin { get; set; }

    public int SelectionMax { get; set; }

    public bool IsRequired { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
