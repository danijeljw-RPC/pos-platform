namespace DaxaPos.Domain.Entities;

/// <summary>
/// A system-wide permission catalogue entry (e.g. <c>organisations.manage</c>). Not tenant-owned —
/// see <see cref="Role"/>.
/// </summary>
public class Permission
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }
}
