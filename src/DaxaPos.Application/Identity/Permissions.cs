using DaxaPos.Domain.Enums;

namespace DaxaPos.Application.Identity;

/// <summary>
/// Permission code catalogue for PLAN-0003's and PLAN-0004's own endpoints. Intentionally
/// minimal — later plans add their own codes as those modules are built; this is not the full
/// eventual permission set. Staff-PIN eligibility is decided per-code by
/// <see cref="DaxaPos.Domain.Entities.Permission.Category"/> (OI-0015), not by a list in this
/// class — see the staff-PIN login guard in <c>AuthEndpoints</c>.
/// </summary>
public static class Permissions
{
    public const string OrganisationsManage = "organisations.manage";
    public const string LocationsManage = "locations.manage";
    public const string TerminalsManage = "terminals.manage";
    public const string DevicesManage = "devices.manage";
    public const string DevicesRegister = "devices.register";
    public const string StaffManage = "staff.manage";
    public const string UsersManage = "users.manage";
    public const string SessionsManage = "sessions.manage";

    /// <summary>PLAN-0004 Milestone A. AdminSensitive — see OI-0007.</summary>
    public const string CatalogManage = "catalog.manage";

    /// <summary>PLAN-0004 Milestone A. AdminSensitive.</summary>
    public const string PricingManage = "pricing.manage";

    /// <summary>PLAN-0004 Milestone A. AdminSensitive.</summary>
    public const string MenusManage = "menus.manage";

    /// <summary>
    /// PLAN-0004 Milestone A. Operational — the first permission code ever granted to the
    /// <c>Staff</c> role, and the first deliberate proof that <see cref="PermissionCategory"/>
    /// (not a hard-coded list) decides staff-PIN eligibility.
    /// </summary>
    public const string CatalogSoldOutToggle = "catalog.sold-out-toggle";
}
