namespace DaxaPos.Domain.Entities;

/// <summary>
/// Join entity granting a <see cref="Permission"/> to a <see cref="Role"/>. Not tenant-owned.
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }
}
