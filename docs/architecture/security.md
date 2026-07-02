# Security Architecture — Daxa POS

## Identity and Authentication

Daxa POS uses a mixed authentication strategy, as defined in [ADR-0013](../adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md). ADR-0009 (single Keycloak strategy) is superseded.

| Use case | Authentication method | Owner |
|---|---|---|
| Cloud admin / back office | Keycloak or equivalent identity provider | Cloud identity provider |
| Cloud employee self-service | Username/password via Keycloak or equivalent | Cloud identity provider |
| Local POS staff login | Staff ID + PIN on trusted registered device | Daxa WebAPI |
| Local manager/admin login | Username/password through Daxa WebAPI (MVP) | Daxa WebAPI |
| Device registration | Device registration PIN/token | Daxa WebAPI |

Local POS staff authentication must continue to work during internet outages. Local Keycloak is not required for MVP.

See [ADR-0013](../adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md) for the full authentication model.

---

## Staff PIN Login Rules

Staff ID/PIN login is for local POS operational use only.

Permitted actions:

- Creating orders.
- Editing open orders.
- Sending orders to preparation stations.
- Taking payments.
- Clock-on/clock-off where enabled.
- Low-risk manager-approved operational actions.

Not permitted:

- Editing tax configuration.
- Editing payment provider settings.
- Editing users, roles, or permissions.
- Accessing payroll or employee personal data.
- Exporting financial data or reports.
- Accessing cloud administration features.

---

## Authorization

Authentication method may vary by deployment mode. Authorization must remain consistent.

The Daxa WebAPI owns application-level authorization.

All successful authentication methods are normalised into a common authorization context:

```text
AuthContext
- ClientId
- OrganisationId
- LocationId
- UserId
- StaffMemberId
- DeviceId
- AuthMethod (CloudIdentityProvider / LocalUsernamePassword / LocalStaffPin / DeviceToken / SupportAccess)
- Roles
- Permissions
```

Roles include: `SystemAdmin`, `OrganisationOwner`, `VenueManager`, `Staff`, `SupportAccess`.

Financial operations (refunds, voids, discounts) require elevated roles.

### Implementation status (PLAN-0003 Milestone C, 2026-07-02)

Local username/password login (`AuthMethod.LocalUsernamePassword`) is implemented: `POST /api/v1/auth/local/login`, `POST /api/v1/auth/logout`, `GET /api/v1/auth/me`. Sessions are opaque, server-hashed bearer tokens (`AuthSession`, not JWT — see [ADR-0015](../adr/accepted/ADR-0015-tenant-isolation-and-session-token-mechanism.md)), capped at 12 hours absolute lifetime with an 8-hour idle timeout. Failed-login lockout is 5 attempts / 15 minutes (`LoginLockoutPolicy`). The `AuthContext` field named `ClientId` above corresponds to `TenantId` in the implemented model (the domain's persisted entity is `Tenant`; the field naming reconciliation was recorded during PLAN-0003 planning). `AuthMethod.LocalStaffPin` (staff PIN login) is not implemented yet — see PLAN-0003 Milestone F. `AuthMethod.CloudIdentityProvider` (Keycloak) remains unwired — see ADR-0015 Follow-Up Work. The permission catalogue seeded so far (`organisations.manage`, `locations.manage`, `terminals.manage`, `devices.manage`, `devices.register`, `staff.manage`, `users.manage`, `sessions.manage`) is intentionally minimal, not the full eventual set.

### Implementation status (PLAN-0003 Milestone E, 2026-07-02)

`AuthMethod.DeviceToken` is implemented. A registered device authenticates with `Authorization: Device {credentialId}.{secret}` — validated by `DeviceTokenAuthenticationHandler` against a salted-HMAC-hashed `DeviceCredential` (constant-time verify; the raw secret is never stored, per ADR-0015 §3). A default policy scheme forwards by Authorization-header prefix (`Bearer …` → `Session`, `Device …` → `DeviceToken`), so both schemes work on any authenticated endpoint. A device token yields a **partial** `AuthContext` — tenant/organisation/location/device populated from server-side rows, no user/staff identity, empty roles/permissions — so every permission-gated endpoint rejects it with 403: trusted device context only, never admin access (ADR-0013's rule that a device credential "must not grant user permissions by itself"). Device enrolment uses admin-issued, hashed, 15-minute 6-digit registration PINs; the pre-auth registration endpoint is rate-limited (10/minute per remote IP, HTTP 429). Credential rotation invalidates the old credential immediately; device revocation is terminal (re-register as a new device). See `docs/modules/devices.md` for the endpoint list and audit events.

### Implementation status (PLAN-0003 Milestone F, 2026-07-02)

`AuthMethod.LocalStaffPin` is implemented — Daxa WebAPI-native, never Keycloak/OIDC (ADR-0013). `POST /api/v1/auth/staff-pin/login` requires a trusted `DeviceToken` context first (anonymous → 401, a `Bearer` session → 403); tenant/organisation/location/device scope comes exclusively from the device's `AuthContext`, and the body's `locationId` must match both the device's registered location and the staff member's home location. Staff identify with a `StaffCode` (human-enterable, uppercase alphanumeric, 2–20 chars, unique per organisation — never the primary key) plus a PIN (digits, 4–10, PBKDF2-hashed, never stored raw; lockout 5 attempts / 15 minutes). Staff sessions are ordinary `AuthSession` rows with a deliberately shorter expiry than admin sessions: **8 hours absolute / 30 minutes idle** (`StaffSessionExpiryPolicy`). Every login failure returns the same generic 401 (never disclosing whether the code or PIN was wrong) but is audited with its specific reason — including unknown staff codes, since the device context supplies the tenant. Defense-in-depth: login is refused outright if the role snapshot would include any admin-sensitive permission (`Permissions.AdminSensitive`), beneath the endpoint-level `rejectStaffPin` check; **recorded follow-up** — this is a hard-coded current-catalogue list for now; permission metadata/category should eventually define staff-PIN eligibility per permission. PIN resets are server-generated (raw PIN returned once) and revoke the staff member's active sessions; emergency disable (`POST /api/v1/staff-members/{id}/disable`) deactivates the staff member and revokes their sessions immediately.

### PLAN-0003 closeout (Milestone H, 2026-07-03)

PLAN-0003 is complete: all four authentication methods this plan scoped (`LocalUsernamePassword`, `LocalStaffPin`, `DeviceToken`, plus the still-unwired `CloudIdentityProvider` placeholder) and the `AuthContext`/RBAC/audit model above are implemented and committed, verified end-to-end with Keycloak stopped (`HybridOfflineLoginTests.cs`) and consolidated across every protected endpoint (`RbacTests.cs`). Still explicitly out of scope, tracked as open issues rather than silently missing: local manager/admin user management endpoints ([OI-0011](../issues/open/OI-0011-user-management-endpoints.md)), inactive-parent lifecycle vs. device/staff authentication ([OI-0012](../issues/open/OI-0012-inactive-parent-lifecycle-vs-device-staff-authentication.md)), tenant-less security-event auditing for unknown-identifier attempts ([OI-0014](../issues/open/OI-0014-tenantless-security-event-auditing.md)), and permission metadata for staff-PIN eligibility ([OI-0015](../issues/open/OI-0015-permission-metadata-for-staff-pin-eligibility.md)). `AuthMethod.CloudIdentityProvider`/Keycloak JWT wiring remains deliberately unbuilt, per ADR-0015 Follow-Up Work.

---

## Tenant Isolation

- EF Core global query filters enforce tenant boundary on every database query.
- No cross-tenant data access is permitted via API.
- Tenant ID is extracted from the JWT or authenticated session, not from a URL parameter.

---

## Payment Security

- Payment provider credentials are stored encrypted.
- Provider credentials are never returned in API responses.
- Provider credentials are never logged.
- Integrated payment amounts are sent from the POS to the terminal — staff do not type amounts manually.
- Daxa POS does not store raw card data.

---

## Audit

All security-significant events are written to the audit log:

- Login (success and failure) — all authentication methods.
- Device registration and deregistration.
- Support access.
- Refunds, voids, discounts, and manual overrides.
- Tax configuration changes.
- User, role, and permission changes.
- Receipt reprints.
- Manager approval actions.

---

## Related Documents

- [ADR-0008 — Device Identity vs User Identity](../adr/accepted/ADR-0008-device-identity-vs-user-identity.md)
- [ADR-0013 — Cloud Identity and Local POS Authentication Strategy](../adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md)
- [ADR-0010 — Financial Records, Ledger, and Audit](../adr/accepted/ADR-0010-financial-records-ledger-and-audit.md)
- [Architecture: Tenancy](tenancy.md)
- [Module: Audit](../modules/audit.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)
