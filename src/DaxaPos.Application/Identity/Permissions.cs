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

    /// <summary>
    /// PLAN-0005 Milestone A. Operational — order entry is core POS counter work, the same
    /// reasoning PLAN-0004 used for <see cref="CatalogSoldOutToggle"/>.
    /// </summary>
    public const string OrdersManage = "orders.manage";

    /// <summary>
    /// PLAN-0005 Milestone B. Operational — taking a cash/manual EFTPOS payment is routine counter
    /// work, the same reasoning as <see cref="OrdersManage"/>.
    /// </summary>
    public const string PaymentsRecord = "payments.record";

    /// <summary>
    /// PLAN-0005 Milestone C. AdminSensitive — manager/admin-only by default (approved Human
    /// Decision #4), a firmer floor than <see cref="OrdersManage"/>/<see cref="PaymentsRecord"/>:
    /// refunds are exactly the kind of override CLAUDE.md's pricing/discounts section says must be
    /// "permissioned and audited," matching <see cref="CatalogManage"/>/<see cref="PricingManage"/>/
    /// <see cref="MenusManage"/>'s precedent.
    /// </summary>
    public const string PaymentsRefund = "payments.refund";

    /// <summary>
    /// PLAN-0005 Milestone D. Operational — resolves the plan's approved Human Decision #5 addition:
    /// a standalone after-the-fact reprint action gets its own code, separate from
    /// <see cref="OrdersManage"/>'s live-sale-viewing surface, but staff-PIN-eligible like
    /// <see cref="OrdersManage"/>/<see cref="PaymentsRecord"/> — reprinting a receipt is routine
    /// counter work, not a manager-only override the way <see cref="PaymentsRefund"/> is.
    /// </summary>
    public const string ReceiptsReprint = "receipts.reprint";
}
