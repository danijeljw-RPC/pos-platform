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

    /// <summary>
    /// Admin-sensitive permission codes a staff PIN session must never hold (ADR-0013's staff-PIN
    /// restrictions; PLAN-0003 Milestone F, Decision 8). Staff PIN login is rejected outright if
    /// the role snapshot would include any of these — defense-in-depth beneath the endpoint-level
    /// <c>rejectStaffPin</c> check. TEMPORARY MECHANISM (recorded follow-up): this is the current
    /// catalogue enumerated by hand; permission metadata/category should eventually define
    /// staff-PIN eligibility per permission instead of a hard-coded list here.
    /// </summary>
    public static readonly IReadOnlySet<string> AdminSensitive = new HashSet<string>
    {
        OrganisationsManage,
        LocationsManage,
        TerminalsManage,
        DevicesManage,
        DevicesRegister,
        StaffManage,
        UsersManage,
        SessionsManage,
    };
}
