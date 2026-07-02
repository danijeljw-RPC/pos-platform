# OI-0011 — User Management Endpoints

## Status

Open

## Area

Identity / Security

## Summary

The `users.manage` permission is seeded but no endpoint consumes it: there is no API to create local manager/admin `User` accounts, assign their roles, reset their passwords, or disable them. The only `User` in any deployment is the bootstrap admin created by `BootstrapAdminSeeder` at startup.

## Context

PLAN-0003 built the full `User`/`UserRole`/RBAC schema and local username/password login (Milestone C), and the permission catalogue includes `users.manage` ("Create local manager/admin users, assign roles", seeded to `SystemAdmin` and `OrganisationOwner`) — but no milestone implemented endpoints for it. The plan's original "Files Likely To Change" listed a `users` endpoints file that no implementation step ever covered; the gap was made concrete by the manual smoke test (`docs/testing/local-smoke-test.md`):

- Every location/PIN/staff operation cross-checks the caller's session `OrganisationId` (ADR-0015 Context Provenance).
- The bootstrap admin's session is pinned to the auto-created Bootstrap Organisation.
- A `SystemAdmin` can therefore create a second `Organisation` (201) but can never mint a login inside it — the "bootstrap organisation dead end". A multi-organisation tenant cannot actually be operated.

**Explicit human decision (2026-07-03, Milestone G approval):** create this issue only; do **not** implement user-management endpoints in Milestone G, which is test/docs/hardening only. This is a follow-up implementation plan.

## Impact

- Multi-organisation tenants are unusable until this exists (single-organisation MVP deployments are unaffected — the bootstrap admin covers them).
- Real tenant onboarding (beyond dev-only bootstrap seeding) depends on it.
- The `Organisation`-endpoints-scope-to-tenant design decision (Milestone D) only becomes exercisable in practice once a `SystemAdmin` can create users in a second organisation.

## Options

1. **Small follow-up plan adding `users.manage`-gated CRUD** (`POST/GET /api/v1/users`, role assignment, password reset, disable) following the Milestone D/F endpoint conventions exactly (flat routes, `rejectStaffPin: true`, 400 on client-supplied `TenantId`, lifecycle audit events).
2. **Fold into the plan that first wires Keycloak** (`AuthMethod.CloudIdentityProvider`) so local user management and cloud identity are designed together.
3. **Defer until the admin portal PWA plan**, building the API and its consumer at the same time.

## Recommendation

Option 1 — a small dedicated plan. The dead end blocks API-level operability regardless of UI or Keycloak timing, and every convention it needs is already established. Option 2 risks coupling a small local-MVP gap to a much larger integration.

## Decision Needed

- When to schedule the follow-up plan.
- Whether `OrganisationOwner`-held `users.manage` may create users only in the caller's own organisation while `SystemAdmin` may target any organisation in the tenant (mirroring the Milestone D scoping asymmetry).

## Related Documents

- [ADR-0013 — Cloud Identity and Local POS Authentication Strategy](../../adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md)
- [ADR-0015 — Tenant Isolation and Session Token Mechanism](../../adr/accepted/ADR-0015-tenant-isolation-and-session-token-mechanism.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)
- [Local smoke test](../../testing/local-smoke-test.md) (documents the dead end)
