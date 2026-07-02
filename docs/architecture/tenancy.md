# Tenancy Architecture — Daxa POS

Daxa POS is multi-tenant from the ground up. Every tenant supports multi-location by default.

See [ADR-0003](../adr/accepted/ADR-0003-multi-location-by-default.md) for the decision record.

---

## Hierarchy

```text
Tenant
└─ Organisation
   └─ Region (optional grouping)
      └─ Country
         └─ Location / Venue
            └─ Terminal
```

A single-location business is a tenant with one Organisation, one Country, one Location, and one or more Terminals.

---

## Tenant Isolation

- Every API call is scoped to a tenant via the authenticated session/device context (never a client-supplied route or body value — see ADR-0015's Context Provenance rule).
- EF Core global query filters enforce tenant isolation on every query.
- Cross-tenant data access is not permitted.
- Support access by Daxa staff is audited.

### Implementation status (PLAN-0003 Milestone B, 2026-07-01)

Tenant isolation is implemented via a denormalized `TenantId` column on every tenant-owned table (`Organisation`, `Location`, `Device`, `Terminal` so far) plus a fail-closed EF Core global query filter in `DaxaDbContext`, fed by `ICurrentTenantProvider` (`DaxaPos.Domain.Tenancy`). A missing tenant context (`TenantId == null`) matches zero rows, never an unfiltered query — see [ADR-0015](../adr/accepted/ADR-0015-tenant-isolation-and-session-token-mechanism.md) for the full mechanism and rationale. `Tenant` itself is the isolation root and is not filtered. Cross-tenant isolation is covered by `tests/DaxaPos.Api.Tests/TenantIsolationTests.cs`. The JWT-claim-based tenant resolution mentioned above is the eventual `AuthMethod.CloudIdentityProvider` path (not yet wired — see ADR-0015 Follow-Up Work); the WebAPI-native `AuthContext`-based resolution is what's implemented today.

### Implementation status (PLAN-0003 Milestone D, 2026-07-02)

`Organisation`/`Location`/`Terminal` create/read/rename/deactivate/reactivate endpoints (`/api/v1/organisations`, `/api/v1/locations`, `/api/v1/terminals`) implement ADR-0015's Context Provenance rule concretely: `organisations.manage` is scoped to the caller's tenant only (via the query filter above), while `locations.manage`/`terminals.manage` add a second, stricter check against `AuthContext.OrganisationId` — the literal ADR-0015 example — since those permissions are also granted to organisation-scoped roles (`OrganisationOwner`, `VenueManager`), not just `SystemAdmin`. A `Terminal` has no `OrganisationId` column, so its check walks `Terminal → Location → OrganisationId`. Every mismatch (wrong tenant *or* wrong organisation within the same tenant) returns 404, never 403, so a caller cannot learn that a record exists elsewhere. A client-supplied `TenantId` in any request body is rejected with 400, never silently accepted or merely ignored.

These endpoints also introduced an `IsActive` lifecycle flag on all three entities (migration `AddIsActiveToOrganisationLocationTerminal`) — **deliberately kept out of the tenant-isolation query filter**. Tenant isolation and lifecycle visibility are separate concerns: an inactive row is still fully visible to its own tenant/organisation; each endpoint decides for itself whether inactive rows are included (list endpoints exclude them by default, single-record `GET`/deactivate/reactivate do not). See `docs/plans/active/PLAN-0003-worker-notes.md`'s Milestone D report for the full design and test coverage (`OrganisationEndpointsTests.cs`, `LocationEndpointsTests.cs`, `TerminalEndpointsTests.cs`).

### Implementation status (PLAN-0003 Milestone E, 2026-07-02)

Two new tenant-owned tables, `device_credentials` and `device_registration_pins` (migration `AddDeviceCredentialsAndRegistrationPins`), follow the standard pattern: denormalized `TenantId` column + fail-closed global query filter in `DaxaDbContext`. The documented `IgnoreQueryFilters()` bootstrap call sites are now a small fixed set, each of which runs **before** any tenant context can exist and is what establishes it: user-by-email at login, session-by-token-hash at session validation, device-credential-by-id (plus its `Device`/`Location`, explicitly pinned to the credential's own `TenantId`) at device-token validation, and the registration-PIN candidate scan during pre-auth device registration. No other code bypasses the filters. **Checklist reminder for every new tenant-owned entity:** add the `TenantId` column + FK + index, add one filter line in `DaxaDbContext.OnModelCreating`, and add a cross-tenant invisibility test.

### Implementation status (PLAN-0003 Milestone F, 2026-07-02)

Two new tenant-owned tables, `staff_members` and `staff_member_roles` (migration `AddStaffMembers`, which also added the deferred `auth_sessions.staff_member_id` FK), follow the standard pattern: denormalized `TenantId` column + fail-closed global query filter. Notably, staff PIN login needs **no** `IgnoreQueryFilters()` bootstrap call site: the login endpoint runs after `DeviceTokenAuthenticationHandler` has already established the device's tenant context, so the `StaffMember` lookup happens under the normal fail-closed filter — the documented bootstrap set remains the four Milestone C–E call sites. Staff-member endpoints apply the same organisation cross-check as Locations (`AuthContext.OrganisationId`, 404 on mismatch, including a different organisation within the same tenant).

---

## Organisation

Organisations group venues under a business entity. Most tenants have one organisation. Multi-brand groups may have multiple organisations under one tenant.

---

## Region

Region is an optional grouping level for multi-country chains (e.g. APAC, EMEA). It is used for reporting and configuration grouping, not for data isolation.

---

## Location

A `Location` (also called a Venue) is a physical trading location.

Location-level data includes:
- Venue name and address.
- Country and currency.
- Time zone.
- Tax profile.
- Payment provider configuration.
- Printer and device configuration.
- Menu availability.
- Stock levels.

---

## Terminal

A `Terminal` is a registered POS device at a Location. Terminals have:
- Terminal type (POS, KDS, admin, display).
- Printer mapping.
- Payment terminal mapping.
- Display configuration.

Device identity and user identity are separate (see [ADR-0008](../adr/accepted/ADR-0008-device-identity-vs-user-identity.md)).

---

## Configuration Inheritance

```text
Organisation
├─ Default product catalogue
├─ Default pricing
└─ Default tax profile

Location (inherits organisation defaults)
├─ Location-specific product availability
├─ Location-specific price overrides
└─ Location-specific payment provider
```

---

## Related Documents

- [ADR-0003 — Multi-Location by Default](../adr/accepted/ADR-0003-multi-location-by-default.md)
- [ADR-0008 — Device Identity vs User Identity](../adr/accepted/ADR-0008-device-identity-vs-user-identity.md)
- [Architecture: Multi-Location](multi-location.md)
- [Architecture: Security](security.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)
