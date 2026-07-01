# PLAN-0003 — Identity, Tenancy, Locations, and Devices

## Status

Draft — Rewritten 2026-07-01 to align with ADR-0013. The previous version of this plan was written against the superseded ADR-0009 and modelled POS staff PIN login as a Keycloak/OIDC flow, which ADR-0013 explicitly rejects. See "Handoff Notes" for what changed.

## Goal

Implement the identity, tenancy, multi-location, and device registration foundation using the mixed authentication model accepted in ADR-0013:

- Keycloak (or an equivalent identity provider) for cloud, back-office, admin, support, and external identity scenarios.
- A Daxa WebAPI-owned staff ID + PIN flow for local POS terminal sessions — **not** an OIDC/Keycloak login.
- A Daxa WebAPI-owned username/password flow for local manager/admin login (MVP).
- One normalised authorization context (`AuthContext`) applied consistently regardless of which authentication method produced it.

## Scope

- Keycloak integration (OIDC), scoped strictly to cloud/back-office/admin/support/external identity use cases (e.g. tenant admin login, employee self-service, support access) — not POS terminal staff sessions.
- Daxa WebAPI-native staff ID + PIN authentication service for POS terminal staff sessions.
- Daxa WebAPI-native username/password authentication for local manager/admin login (MVP).
- Short-lived POS staff session model: tied to terminal, location, staff member, and a role/permission snapshot taken at session start, with full audit context.
- Multi-tenant middleware and query filters.
- Organisation / Location / Terminal hierarchy (API and database).
- Role-based and permission-based access control, applied uniformly across all authentication methods via the shared `AuthContext`.
- Device registration flow (ADR-0008): device identity is registered and trusted independently of any user identity.
- Local mode: staff PIN login must work with no internet connectivity.
- Hybrid mode: staff/role/permission data synced from cloud is used for local authentication during outages.
- Cloud mode: the Daxa WebAPI remains the authority that issues and validates POS staff sessions, even when the venue is cloud-hosted — Keycloak is not inserted into the per-transaction POS staff PIN flow in any deployment mode.
- Support/admin access with audit.

## Non-goals

- Business domain modules (orders, products, payments).
- PWA or MAUI UI.
- Full inventory or reporting.
- Local Keycloak deployment — explicitly rejected for MVP by ADR-0013.
- Using Keycloak/OIDC for POS terminal staff PIN login — explicitly rejected by ADR-0013.

## Context Read

- `docs/plans/active/PLAN-0002-platform-skeleton.md`
- `docs/adr/accepted/ADR-0003-multi-location-by-default.md`
- `docs/adr/accepted/ADR-0008-device-identity-vs-user-identity.md`
- `docs/adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md`
- `docs/architecture/tenancy.md`
- `docs/architecture/security.md`
- `docs/issues/closed/OI-0002-identity-provider-local-cloud-hybrid.md` (resolved by ADR-0013)
- `docs/issues/closed/OI-0010-local-keycloak-vs-cloud-keycloak.md` (resolved by ADR-0013)

## Files Likely To Change

```
src/DaxaPos.Domain/          (User, Role, Permission, Device, Location, Organisation, StaffMember, PosStaffSession)
src/DaxaPos.Application/     (identity use cases, AuthContext normalisation, IDomainEventDispatcher consumers for audit)
src/DaxaPos.Infrastructure/  (Keycloak client for cloud/admin auth, JWT middleware, staff PIN hashing, session token issuance)
src/DaxaPos.Persistence/     (tenant filter, location queries, staff PIN credential storage, synced staff/role/permission cache for hybrid offline auth)
src/DaxaPos.Api/             (cloud/admin auth endpoints, POS staff PIN session endpoints, device registration)
```

## Architecture Assumptions

- Keycloak (or equivalent) runs via Docker Compose and is used **only** for cloud/admin/back-office/support/external identity login (ADR-0013). It is not on the critical path for local POS trading and must not be a dependency of the staff PIN session flow.
- JWT bearer tokens issued by Keycloak are used for cloud/admin/back-office API calls.
- POS terminal staff sessions use a separate, short-lived session token issued directly by the Daxa WebAPI after staff ID + PIN validation on a trusted registered device. This token is not an OIDC/Keycloak token and does not require a round trip to Keycloak.
- Local manager/admin login (MVP) uses Daxa WebAPI-owned username/password, not Keycloak.
- All authentication methods (`CloudIdentityProvider`, `LocalUsernamePassword`, `LocalStaffPin`, `DeviceToken`, `SupportAccess`, per ADR-0013) are normalised into one shared `AuthContext` before any authorization check runs.
- Module-to-module communication (e.g. publishing an audit event when a staff session starts) follows ADR-0014 once accepted — direct calls for synchronous needs, in-process domain events for fan-out such as audit logging.

## Domain Assumptions

- `Tenant → Organisation → Region → Country → Location → Terminal` hierarchy.
- Device identity and user identity are separate (ADR-0008).
- A POS staff session requires all of: a trusted registered device context (ADR-0008), a location context, a valid staff ID/PIN pair, and a role/permission snapshot taken at session start.
- POS staff sessions are short-lived and scoped to operational actions only — creating orders, editing open orders, sending orders to preparation stations, taking payments, clock-on/clock-off where enabled, and low-risk manager-approved actions — per ADR-0013's Staff ID/PIN Login Rules. They must never grant access to tax configuration, payment provider settings, user/role/permission management, payroll or personal staff data, financial report export, or cloud administration features.
- Local mode: staff PIN login must succeed with zero internet connectivity, using locally stored staff/role/permission data.
- Hybrid mode: staff/role/permission data is synced from cloud to local (per ADR-0007 and OI-0006's conflict rules) so local staff PIN login can authenticate against synced data during an outage, then reconcile once connectivity returns. A local emergency-disable path must exist for disabling a compromised staff credential without waiting on cloud sync.
- Cloud mode: even when the venue is cloud-hosted, the Daxa WebAPI — not Keycloak — issues and validates POS staff sessions.
- Manager approval actions use manager PIN for low-risk overrides (e.g. void unsent item, apply discount) or require full username/password for sensitive actions (e.g. refund paid order, edit catalogue/tax config, manage users, export reports), per the action-risk table in ADR-0013.

## Risks

- Two authentication code paths (Keycloak-backed and WebAPI-native) must both normalise into the same `AuthContext` and be checked by the same authorization layer without permission logic diverging between them.
- Staff PIN storage/hashing and brute-force/lockout protection must be implemented independently of Keycloak — there is no reuse of Keycloak's password policy machinery for this path.
- Hybrid sync of staff/role/permission data must propagate a cloud-side "user disabled" fast enough that a disabled user cannot keep trading locally for an extended period during an outage.
- Multi-tenant EF Core query filters must be applied to every query, across both the Keycloak-backed and WebAPI-native code paths.
- This plan depends on ADR-0014 (inter-module communication) being accepted, since audit-event publishing for login/session activity should follow that pattern rather than being invented ad hoc here.

## Implementation / Documentation Steps

1. Implement the shared `AuthContext` model and an authorization middleware that consumes it, per ADR-0013's conceptual model (`ClientId`, `OrganisationId`, `LocationId`, `UserId`, `StaffMemberId`, `DeviceId`, `AuthMethod`, `Roles`, `Permissions`).
2. Configure a Keycloak realm/client scoped to cloud/admin/back-office/support/external identity login only; implement JWT bearer auth middleware for that path.
3. Implement the Daxa WebAPI-native staff ID + PIN authentication endpoint: validates staff ID + PIN + trusted device context + location context, issues a short-lived POS session token, and normalises the result into `AuthContext` with `AuthMethod = LocalStaffPin`.
4. Implement Daxa WebAPI-native local manager/admin username + password authentication (MVP path, not Keycloak), normalised into `AuthContext` with `AuthMethod = LocalUsernamePassword`.
5. Implement multi-tenant context extraction and EF Core global query filters for tenant isolation, applied uniformly regardless of `AuthMethod`.
6. Implement Organisation, Location, Region, Country APIs.
7. Implement Terminal/Device registration API (ADR-0008), issuing `AuthMethod = DeviceToken` context where relevant and confirming a device token alone never grants user permissions.
8. Implement staff user management API (staff records, PIN reset/rotation, role assignment).
9. Implement RBAC middleware operating purely against the normalised `AuthContext`, never against the original authentication mechanism.
10. Implement local storage of synced staff/role/permission data for hybrid offline PIN login, plus the local emergency-disable path described in ADR-0013.
11. Write tenant isolation, staff-PIN, RBAC, and offline-login integration tests.
12. Update `docs/architecture/tenancy.md` and confirm `docs/architecture/security.md` still matches what was implemented (it already documents the ADR-0013 model).

## Tests To Run Later

- Tenant isolation tests (cannot access another tenant's data).
- Location isolation tests.
- Device registration tests, including confirming a device token alone cannot authenticate a user.
- Staff ID/PIN login tests: correct PIN, wrong PIN, untrusted device, session expiry, and scope limits (a staff session must be rejected by tax-config, payroll, and report-export endpoints).
- Local offline staff PIN login test — no internet, synced local data only.
- Manager PIN vs. username/password action-risk tests, matching the table in ADR-0013.
- RBAC tests (correct roles required for each endpoint) across both Keycloak-issued and WebAPI-issued sessions, verifying identical authorization behaviour.
- Support access audit tests.
- Hybrid sync test: a cloud-side user disable propagates to local and blocks further local staff PIN login for that user.

## Documentation To Update

- `docs/architecture/tenancy.md`
- `docs/architecture/security.md` (already reflects ADR-0013 at the documentation level; confirm it still matches the implementation)
- `docs/architecture/multi-location.md`
- `docs/modules/devices.md`

## ADRs Required

- ADR-0003, ADR-0008, ADR-0013 (all already accepted). No new ADR is required for this plan's identity model.
- ADR-0014 (inter-module communication, proposed) should be accepted before step 1, since audit-event publishing for session activity depends on it.

## Open Issues Required

- None. OI-0002 and OI-0010 are already resolved by ADR-0013.

## Commit Sequence

```
feat(identity): add shared AuthContext model and authorization middleware
feat(identity): add Keycloak integration scoped to cloud/admin/back-office auth
feat(identity): add WebAPI-native staff ID/PIN authentication for POS sessions
feat(identity): add WebAPI-native local manager/admin username+password auth
feat(tenancy): add multi-tenant middleware and EF Core query filters
feat(location): add organisation/location/terminal hierarchy
feat(devices): add device registration API
feat(identity): add hybrid offline staff PIN login using synced local data
test(identity): add tenant isolation, staff-PIN, RBAC, and offline-login tests
docs: update tenancy, security, and device docs
```

## Handoff Notes

This plan depends on PLAN-0002 (Platform Skeleton) and on ADR-0014 (inter-module communication) being accepted, since `AuthContext` propagation and session/audit-event publishing depend on that pattern.

**What changed in this rewrite (2026-07-01):** the previous version of this plan cited the superseded ADR-0009, framed the entire identity model around "Keycloak integration (OIDC)" as the primary mechanism (including a realm-per-tenant-vs-shared-realm decision for POS PIN login), and listed "Resolve OI-0002 and OI-0010" as its first step. All of that is now resolved by ADR-0013: Keycloak is scoped to cloud/admin/back-office/support/external identity only; POS terminal staff sessions are a Daxa WebAPI-native staff ID + PIN flow that must keep working offline. A worker picking up the old version of this plan would very likely have built POS staff PIN login as an OIDC flow, which would have required significant rework once ADR-0013 was checked against the implementation.

After completing this plan, the identity and tenancy foundation is in place. The next worker can implement the product catalogue (PLAN-0004) with proper tenant/location context available.
