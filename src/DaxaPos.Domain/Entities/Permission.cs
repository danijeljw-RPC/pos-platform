using DaxaPos.Domain.Enums;

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

    /// <summary>
    /// Whether a staff PIN session may hold this permission (OI-0015). Required at creation time
    /// so every new permission code is consciously classified, rather than defaulting open.
    /// </summary>
    public PermissionCategory Category { get; set; }
}
