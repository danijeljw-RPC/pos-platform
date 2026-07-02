# OI-0015 — Permission Metadata for Staff-PIN Eligibility

## Status

Open

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
