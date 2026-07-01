namespace DaxaPos.Domain.Entities;

/// <summary>
/// Assigns a <see cref="Role"/> to a <see cref="User"/>, optionally scoped to an organisation or
/// location. Tenant-owned (unlike <see cref="Role"/> itself) — carries its own denormalized
/// <see cref="TenantId"/> for the fail-closed query filter (ADR-0015).
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public Guid TenantId { get; set; }

    public Guid? OrganisationId { get; set; }

    public Guid? LocationId { get; set; }
}
