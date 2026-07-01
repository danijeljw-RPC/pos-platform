namespace DaxaPos.Domain.Entities;

/// <summary>
/// A system-wide role catalogue entry (e.g. <c>SystemAdmin</c>, <c>VenueManager</c>). Roles are
/// not tenant-owned — they are shared across all tenants and seeded once, not created via API.
/// </summary>
public class Role
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystemDefined { get; set; }
}
