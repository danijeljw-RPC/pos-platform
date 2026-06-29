# ADR-0003 — Multi-Location by Default

## Status

Proposed

## Context

Daxa POS targets both single-location businesses (e.g. a single bakery) and multi-location chains (e.g. a franchise or hospitality group with many sites). If multi-location support is retrofitted later, it creates significant technical debt and schema migrations under live data.

## Decision

Every tenant in Daxa POS supports multi-location by default.

- A single-location business is simply a tenant with one location record.
- No special single-location business logic will be introduced that conflicts with or would need to be unpicked for multi-location operation.
- The data hierarchy is: `Tenant → Organisation → Region → Country → Location → Terminal`.
- Products, prices, taxes, payment providers, devices, and reports may have organisation-level defaults and location-level overrides.

## Consequences

**Positive:**
- Single-location customers can be onboarded today.
- When they grow, no migration is required — add a location record.
- Reporting, configuration, and access control are already designed for multiple locations.
- Franchise and chain customers are supported from day one.

**Negative:**
- Data model is slightly more complex than a purely single-location model.
- Queries must always include location context.
- UI must expose location selection where relevant.

## Alternatives Considered

1. **Single-location first, multi-location later** — Rejected. Multi-location is a core product promise and retrofitting it is expensive and risky.
2. **Multi-tenancy only, no location tier** — Rejected. Many chains operate multiple venues under one organisation and need location-level isolation for stock, cash, and reporting.

## Open Questions

- Should MVP UI expose location switching, or assume a single active location per session?
- Should cross-location product catalogue be a Phase 1 or Phase 3 feature?

## Related Documents

- [ADR-0001 — Single Codebase](ADR-0001-single-codebase.md)
- [Architecture: Tenancy](../architecture/tenancy.md)
- [Architecture: Multi-Location](../architecture/multi-location.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)
