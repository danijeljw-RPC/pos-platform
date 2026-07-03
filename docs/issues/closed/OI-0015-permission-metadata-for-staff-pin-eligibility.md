# OI-0015 — Permission Metadata for Staff-PIN Eligibility

## Status

Closed

## Area

Identity / Security

## Summary

Which permissions a staff PIN session may carry is currently defined by a hard-coded list — `Permissions.AdminSensitive`, containing all eight current catalogue codes — checked at staff PIN login as defense-in-depth beneath the endpoint-level `rejectStaffPin` gate. Permission metadata (a category/flag on the `Permission` catalogue itself) should define staff-PIN eligibility instead, so the list cannot silently rot as later plans add permission codes.

## Context

Recorded as an explicit follow-up at Milestone F approval (Decision 8, 2026-07-02): the hard-coded list was approved as a temporary mechanism with the instruction "do not extend the hard-coded list indefinitely." The list lives in `src/DaxaPos.Application/Identity/Permissions.cs` with the follow-up note in its doc comment. The danger is directional: every permission PLAN-0004+ adds (`tax.manage`, `refunds.approve`, `reports.export`, …) must be consciously classified, and a forgotten addition to the hard-coded list fails *open* at the login-time guard (though the endpoint-level `rejectStaffPin` gate on sensitive endpoints remains an independent net).

## Impact

- Affects the `Permission` entity/seed data (a new column or category field — small migration).
- Affects staff PIN login's guard (reads metadata instead of the static list) and potentially `RequirePermissionFilter` (a `rejectStaffPin` default derived from the permission's own classification rather than per-call-site flags).
- ADR-0013's operational-vs-sensitive action split becomes data, which future per-venue configurability ("manager PIN may be configurable for low-risk venues") can then build on.

## Options

1. **`Permission.Category` enum** (`Operational` / `AdminSensitive`), seeded per permission; the login guard and (optionally) `RequirePermissionFilter` derive behaviour from it.
2. **Boolean flag** (`AllowedForStaffPinSessions`) — same effect, less room for future tiers (e.g. ADR-0013's manager-PIN middle tier).
3. **Keep the static list** until the catalogue is large enough to hurt — pure deferral.

## Recommendation

Option 1, implemented by the first plan that adds new permission codes (expected PLAN-0004, `tax.manage`) — that is the moment the hard-coded list would otherwise take its first unclassified addition. The enum leaves room for ADR-0013's manager-PIN tier later.

## Decision Needed

- Category shape (enum vs. flag) and whether `RequirePermissionFilter`'s per-call-site `rejectStaffPin` flags should be replaced by metadata-derived defaults or kept as explicit belt-and-braces.

## Related Documents

- [ADR-0013 — Cloud Identity and Local POS Authentication Strategy](../../adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md) (Staff ID/PIN Login Rules)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../../plans/active/PLAN-0003-identity-tenancy-locations-devices.md) (Milestone F, Decision 8)
- [PLAN-0004 — Catalog, Menu, Tax, Pricing](../../plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md) (Milestone A)

---

## Decision

Option 1 (`Permission.Category` enum: `Operational` / `AdminSensitive`), implemented by PLAN-0004 Milestone A — the first plan to add new permission codes since this issue was recorded, exactly as the original recommendation anticipated.

`RequirePermissionFilter`'s per-call-site `rejectStaffPin` flag was **kept as an explicit, independent belt-and-braces check**, not replaced by a metadata-derived default. The two mechanisms serve different layers: `Permission.Category` decides what a staff PIN session's role snapshot is *allowed to contain in the first place* (checked once, at login); `rejectStaffPin` decides what a specific *endpoint* accepts, regardless of the caller's permissions (checked on every request). Collapsing them into one would remove the endpoint-level net that `StaffSession_MisconfiguredWithSensitivePermissions_IsStillRejectedByRejectStaffPinEndpoints` (`tests/DaxaPos.Api.Tests/StaffPinLoginTests.cs`) exists specifically to prove still holds even if the login-time guard were ever bypassed or misconfigured.

## Outcome

- Added `PermissionCategory` enum (`Operational = 0`, `AdminSensitive = 1`) in `src/DaxaPos.Domain/Enums/PermissionCategory.cs`.
- Added a required `Permission.Category` column (migration `20260703120121_AddPermissionCategory`), backfilling all 8 pre-existing permission codes to `AdminSensitive` (matching their prior hard-coded classification exactly — no behaviour change for existing codes) and seeding the 4 new PLAN-0004 Milestone A codes with explicit categories.
- Refactored the staff-PIN login guard in `src/DaxaPos.Api/Endpoints/Identity/AuthEndpoints.cs` (`StaffPinLoginAsync`) to query the assigned roles' `Permission.Category` values directly, rather than intersecting against a hard-coded set.
- Deleted `Permissions.AdminSensitive` (`src/DaxaPos.Application/Identity/Permissions.cs`) — its one other caller, a test seeding a deliberately-misconfigured session, now lists the same 8 codes explicitly instead.
- `catalog.sold-out-toggle` was seeded as `Operational` and granted to the `Staff` role — the first permission code the `Staff` role has ever held, and a live proof that an `Operational`-category permission does not trip the staff-PIN rejection (`Login_WhenAssignedRoleGrantsOnlyOperationalPermissions_Succeeds`), while the existing `AdminSensitive` rejection continues to hold (`Login_WhenAssignedRoleGrantsSensitivePermissions_IsRejectedAndAudited`, unchanged, still green).
- A new test (`PermissionCatalogue_ClassifiesPLAN0004MilestoneAPermissions_ByCategory`) asserts the seeded `Category` value for each of the 4 new codes plus one pre-existing code, directly against the database — proof the classification is real seed data, not just enum plumbing.

## Status Update

This open issue is resolved: `Permission.Category` now defines staff-PIN eligibility per permission, seeded explicitly at creation time for every code in the catalogue (12 total after this plan). No hard-coded list remains. Any future plan adding a permission code must supply a `Category` value or the migration/seed data will not compile — this is what prevents the "silently defaults open" failure mode this issue was raised to close.

Status: **Closed**
