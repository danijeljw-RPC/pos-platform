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
