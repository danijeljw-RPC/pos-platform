namespace DaxaPos.Domain.Entities;

/// <summary>
/// Assigns a <see cref="Role"/> to a <see cref="StaffMember"/>, optionally scoped to a location.
/// Tenant-owned (unlike <see cref="Role"/> itself) — carries its own denormalized
/// <see cref="TenantId"/> for the fail-closed query filter (ADR-0015). Mirrors
/// <see cref="UserRole"/>; the nullable <see cref="LocationId"/> scope column is the
/// forward-compatibility hook for multi-location staff assignment later (PLAN-0003 Decision 5).
/// </summary>
public class StaffMemberRole
{
    public Guid StaffMemberId { get; set; }

    public Guid RoleId { get; set; }

    public Guid TenantId { get; set; }

    public Guid? LocationId { get; set; }
}
