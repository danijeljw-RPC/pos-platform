namespace DaxaPos.Persistence.Seed;

/// <summary>
/// Fixed GUIDs for the seeded <c>Role</c>/<c>Permission</c> catalogue (PLAN-0003 Milestone C).
/// These are deterministic by design — <c>HasData</c> seed rows must have stable keys across
/// migrations. Permission codes here must stay in sync with
/// <c>DaxaPos.Application.Identity.Permissions</c>; <c>DaxaPos.Persistence</c> cannot reference
/// <c>DaxaPos.Application</c> (see ADR-0015 on the project reference graph), so the string
/// literals are necessarily duplicated rather than shared.
/// </summary>
internal static class RbacSeedIds
{
    public static readonly Guid SystemAdminRoleId = new("00000000-0000-0000-0001-000000000001");
    public static readonly Guid OrganisationOwnerRoleId = new("00000000-0000-0000-0001-000000000002");
    public static readonly Guid VenueManagerRoleId = new("00000000-0000-0000-0001-000000000003");
    public static readonly Guid StaffRoleId = new("00000000-0000-0000-0001-000000000004");
    public static readonly Guid SupportAccessRoleId = new("00000000-0000-0000-0001-000000000005");

    public static readonly Guid OrganisationsManagePermissionId = new("00000000-0000-0000-0002-000000000001");
    public static readonly Guid LocationsManagePermissionId = new("00000000-0000-0000-0002-000000000002");
    public static readonly Guid TerminalsManagePermissionId = new("00000000-0000-0000-0002-000000000003");
    public static readonly Guid DevicesManagePermissionId = new("00000000-0000-0000-0002-000000000004");
    public static readonly Guid DevicesRegisterPermissionId = new("00000000-0000-0000-0002-000000000005");
    public static readonly Guid StaffManagePermissionId = new("00000000-0000-0000-0002-000000000006");
    public static readonly Guid UsersManagePermissionId = new("00000000-0000-0000-0002-000000000007");
    public static readonly Guid SessionsManagePermissionId = new("00000000-0000-0000-0002-000000000008");

    // PLAN-0004 Milestone A (permission metadata / OI-0015 close-out).
    public static readonly Guid CatalogManagePermissionId = new("00000000-0000-0000-0002-000000000009");
    public static readonly Guid PricingManagePermissionId = new("00000000-0000-0000-0002-000000000010");
    public static readonly Guid MenusManagePermissionId = new("00000000-0000-0000-0002-000000000011");
    public static readonly Guid CatalogSoldOutTogglePermissionId = new("00000000-0000-0000-0002-000000000012");

    // PLAN-0005 Milestone A.
    public static readonly Guid OrdersManagePermissionId = new("00000000-0000-0000-0002-000000000013");
}
