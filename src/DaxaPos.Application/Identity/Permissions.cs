namespace DaxaPos.Application.Identity;

/// <summary>
/// Permission code catalogue for PLAN-0003's own endpoints. Intentionally minimal — later plans
/// add their own codes (e.g. <c>tax.manage</c> per OI-0007) as those modules are built; this is
/// not the full eventual permission set.
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
}
