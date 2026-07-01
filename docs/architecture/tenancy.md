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

- Every API call is scoped to a tenant via the JWT claim.
- EF Core global query filters enforce tenant isolation on every query.
- Cross-tenant data access is not permitted.
- Support access by Daxa staff is audited.

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
