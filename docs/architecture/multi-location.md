# Multi-Location Architecture — Daxa POS

Every tenant in Daxa POS supports multiple locations by default.

---

## Examples

**Single-location:**

```text
Tenant: Main Street Bakery
Organisation: Main Street Bakery
Country: Australia
Location: Main Street Bakery
Terminal: Front Counter 1
```

**Multi-location chain:**

```text
Tenant: Example Hospitality Group
Organisation: Example Hospitality Group
Region: APAC
Country: Australia
Locations:
  - Sydney CBD
  - Bondi
  - Parramatta
  - Newcastle
Terminals:
  - Sydney CBD / Bar 1
  - Sydney CBD / Bar 2
  - Bondi / Front Counter
  - Parramatta / Restaurant POS
```

---

## Location-Level Features

The following can be configured or overridden at the location level:

- Product availability (some products active at some locations only).
- Menu availability (different menus per location).
- Prices (location-level price overrides on top of organisation defaults).
- Tax profile (if different countries or tax jurisdictions).
- Payment provider (different provider per location).
- Printer routing (printers are per location).
- Device configuration.
- Staff access (staff may be restricted to specific locations).
- Reporting scope.
- Cash reconciliation.

---

## Cross-Location Features (Later)

The following are planned for Phase 3+:

- Cross-location gift cards.
- Cross-location customer profiles.
- Central inventory transfers.
- Franchise-style access restrictions.
- Multi-location executive dashboards.

---

## API Design

All APIs include location context. Queries are automatically scoped to the requesting user's permitted locations.

```text
GET /api/v1/menus
→ Returns menus for the authenticated user's current location

GET /api/v1/reports/sales?locationId=xxx
→ Returns sales report scoped to specified location (if permitted)
```

---

## Related Documents

- [ADR-0003 — Multi-Location by Default](../adr/accepted/ADR-0003-multi-location-by-default.md)
- [Architecture: Tenancy](tenancy.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)

### Implementation status (PLAN-0003 Milestone D, 2026-07-02)

`Location` and `Terminal` now have real create/read/rename/deactivate/reactivate endpoints (`/api/v1/locations`, `/api/v1/terminals`) — see [Architecture: Tenancy](tenancy.md)'s Milestone D note for the authorization model. No hard delete — an `IsActive` flag with dedicated deactivate/reactivate actions is used instead, since these entities are referenced by audit logs, sessions, devices, and (later) orders/payments/reports/sync data. Location/Terminal CRUD is not yet location-aware for staff (no `StaffMember` exists until Milestone F) — these are admin/management endpoints only, and explicitly reject a `LocalStaffPin` session (`rejectStaffPin: true`) even though no such session can exist yet.

### PLAN-0003 closeout (Milestone H, 2026-07-03)

PLAN-0003 is complete for the identity/tenancy/device slice of multi-location support: `StaffMember` (Milestone F) has a single home `Location`, and staff-PIN login cross-checks it against the trusted device's location. Multi-location staff assignment (a staff member belonging to more than one `Location`) remains out of scope for this plan by explicit MVP decision (Human Decisions Needed #5) but was deliberately modelled so it can be added later via a new assignment table, without reshaping `StaffMember` itself. `IsActive` deactivation of a `Location`/`Terminal` still does not cut off its devices' or staff's authentication — tracked as [OI-0012](../issues/open/OI-0012-inactive-parent-lifecycle-vs-device-staff-authentication.md), not solved here. `Region`/`Country` remain out of scope (see [Architecture: Tenancy](tenancy.md)).
