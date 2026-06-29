# ADR-0003 — Multi-Location by Default

## Status

Proposed

## Context

Daxa POS targets both single-location businesses, such as a single bakery, and multi-location chains, such as a franchise or hospitality group with many sites.

If multi-location support is retrofitted later, it creates significant technical debt and schema migrations under live data.

Daxa POS must therefore treat multi-location as a first-class architectural concern from the start, even when onboarding a customer that only has one location.

The platform must also support Local, Hybrid, and Cloud deployment modes:

- In Local mode, the location is served by a local Linux server installed at the venue.
- In Hybrid mode, the location is served operationally by the local Linux server, while cloud services provide central management, sync, reporting, and backup.
- In Cloud mode, the location is served directly by Daxa-managed cloud infrastructure.

This means the location context must be explicit and reliable across authentication, session state, device registration, catalogue access, reporting, sync, and operational workflows.

## Decision

Every tenant in Daxa POS supports multi-location by default.

- A single-location business is simply a tenant with one location record.
- No special single-location business logic will be introduced that conflicts with or would need to be unpicked for multi-location operation.
- The data hierarchy is: `Tenant → Organisation → Region → Country → Location → Terminal`.
- Products, prices, taxes, payment providers, devices, and reports may have organisation-level defaults and location-level overrides.
- Every operational session must have an explicit active location context.
- Local server deployments are assigned to one primary location at a time.
- Cross-location catalogue capability will be included early in the data model to avoid major schema changes later.

## Location Context Model

Location context is mandatory for operational use.

The active location determines:

- available terminals
- available menus
- applicable product catalogue overrides
- applicable prices
- applicable tax settings
- applicable surcharge rules
- payment provider configuration
- printer routing
- kitchen display routing
- bar display routing
- stock/cash/reporting boundaries
- local sync scope
- local device access

The application must not rely on implicit single-location assumptions.

Even when a tenant has only one location, the active location must still be represented in the database, API contracts, session context, audit logs, reports, and device registration model.

## Location Assignment on Local Server Hardware

For Daxa Local and Daxa Hybrid deployments, the active location is primarily set on the local server installed at that location.

The local Linux server is bound to:

- one tenant
- one organisation, where applicable
- one venue/location
- one deployment mode
- one licence/activation record
- one local operational database

Client devices connect to the local server and inherit their available operating context from that server.

Client devices do not independently determine the venue/location for local trading.

This applies to:

- POS terminals
- MAUI applications
- PWA terminals
- kitchen display screens
- bar display screens
- ordering tablets
- printer-connected consoles
- payment-routing integrations

## Location Switching

Location switching must be supported carefully because all local clients connect to the location's server.

Changing the active location for a local server affects all connected clients and local operational data scope.

For that reason, changing the active server location is an administrative action, not a normal POS operator workflow.

### Local Mode Location Switching

In Local mode, the local server is the source of truth for that installation.

Changing the local server's assigned location requires:

1. administrative permission
2. all connected clients to be logged out or disconnected
3. active orders to be completed, cancelled, or safely held according to operational rules
4. local background jobs to be paused or drained where required
5. the local server to switch to the selected location context
6. location-specific data to be loaded from the local database
7. terminals and devices to reconnect under the new location context
8. the change to be written to the audit log

If the local database does not contain the required data for the selected location, the location switch must be blocked unless an approved import/restore process is performed.

Local mode must not depend on the cloud to complete the switch.

### Hybrid Mode Location Switching

In Hybrid mode, the local server remains the operational endpoint, but the cloud is available for central configuration and data sync.

Changing the local server's assigned location requires:

1. administrative permission
2. all connected clients to be logged out or disconnected
3. active orders to be completed, cancelled, or safely held according to operational rules
4. pending local sync to be checked
5. the local server to request the selected location configuration from the cloud
6. the local server to download required location data
7. the local server to apply location-specific configuration locally
8. the local server to update its persisted location binding
9. terminals and devices to reconnect under the new location context
10. the change to be written to the audit log

Hybrid switching must download and persist the relevant location data before the location becomes active locally.

The local server must not switch into a location context if required configuration cannot be downloaded or validated.

The sync service must ensure that local data from the previous location is not mixed with data from the new location.

### Cloud Mode Location Switching

In Cloud mode, location switching is a session-level and authorization-controlled action.

A user may switch to another location if:

- the user belongs to the same tenant or authorised organisation scope
- the user has permission to access the target location
- the target location is active
- the target location is available in the cloud region/session context

Cloud mode does not require local data download before switching, because the cloud service is the operational endpoint.

The application should reload the user's active session context, available terminals, catalogue, prices, tax settings, reports, and device permissions for the selected location.

### Client Behaviour During Location Switching

When a local server changes location, connected clients must not continue operating under stale location context.

Clients must either:

- be logged out before the switch; or
- be forcibly disconnected by the server; or
- be required to reload and re-authenticate after the switch.

Clients must clear or refresh:

- active location context
- terminal context
- cached catalogue data
- cached prices
- cached tax settings
- cached device routing
- cached reporting filters
- cached authorization claims where applicable

Clients must not keep using old location-scoped data after a server-side location switch.

## Cross-Location Catalogue

Cross-location catalogue support must be included as early as possible.

The goal is to reduce future schema changes and avoid retrofitting catalogue hierarchy after live customer data exists.

Daxa POS should prefer migrations that add functionality, not migrations that change existing schema because of avoidable early design gaps.

### Catalogue Scope

The catalogue model must support:

- tenant-level catalogue records
- organisation-level catalogue defaults
- region-level overrides, where required
- country-level overrides, where required
- location-level availability
- location-level pricing overrides
- location-level tax/surcharge overrides
- location-level menu visibility
- location-level printer/KDS routing
- location-level stock tracking where required

A product may exist centrally while being enabled, disabled, priced, routed, or taxed differently per location.

### Phase Decision

Cross-location catalogue must be treated as a Phase 1 data model requirement.

The full management UI can be phased, but the database model and API contracts must support cross-location catalogue from the beginning.

This means Phase 1 should include the core schema required for:

- central product definitions
- location product availability
- location-specific prices
- location-specific tax/surcharge configuration
- location-specific menu assignment
- location-specific reporting
- location-specific audit history

Phase 1 does not need to include every advanced catalogue-management screen, but it must not ship with a single-location-only catalogue schema.

### Catalogue Defaults and Overrides

The preferred catalogue model is default-and-override.

Common catalogue data should be defined at the highest useful level, then overridden where required.

Example:

- tenant defines the base product
- organisation defines the standard menu/category structure
- country defines tax behaviour where required
- location defines availability, local price, printer routing, stock behaviour, and surcharges

This prevents duplication while still supporting real venue differences.

## Data Model Requirements

The database schema must include location context from the start.

The following areas must be location-aware:

- terminals
- devices
- users and permissions
- orders
- payments
- refunds
- cash movements
- shifts
- audit logs
- stock records
- printers
- kitchen display routing
- bar display routing
- menus
- product availability
- prices
- tax configuration
- surcharge configuration
- reports
- sync state

Queries that operate on operational venue data must include tenant and location context.

Where organisation, region, or country scope is supported, the query model must make scope explicit.

## API and Session Requirements

API requests that operate on location-scoped resources must include or resolve an active location context.

The active location may be resolved from:

- authenticated user session
- selected location claim
- local server binding
- terminal/device registration
- explicit API route parameter
- request body, where appropriate
- server-side authorization context

The API must reject requests where the user, terminal, device, or local server is not authorised for the requested location.

Session state must include the active location for operational workflows.

Switching active location must refresh authorization checks and cached location-specific data.

## Reporting Requirements

Reporting must be location-aware from the start.

Reports should support:

- single location reporting
- organisation-wide reporting
- region-level reporting
- country-level reporting
- tenant-wide reporting, where authorised

A single-location tenant is treated as a tenant with one location, not as a special reporting mode.

## Sync Requirements

Hybrid sync must preserve location boundaries.

The sync service must not mix data between locations.

For Hybrid deployments:

- cloud configuration may flow down to the local server
- local operational data may flow up to the cloud
- sync scope must include tenant and location identity
- sync logs must include location identity
- sync conflicts must include location identity

A local server switching location must not reuse unresolved sync state from a previous location as if it belongs to the new location.

## Consequences

**Positive:**

- Single-location customers can be onboarded immediately.
- When a customer grows, no major migration is required; a new location record can be added.
- Reporting, configuration, and access control are designed for multiple locations from the beginning.
- Franchise and chain customers are supported from day one.
- Catalogue design can support central defaults and local overrides.
- Future catalogue enhancements are more likely to be additive migrations rather than structural rewrites.
- Local, Hybrid, and Cloud deployment modes can all resolve location context consistently.
- Local server hardware has a clear location binding.

**Negative:**

- Data model is more complex than a purely single-location model.
- Queries must always include tenant and location context where applicable.
- UI must expose location selection where relevant.
- Local server location switching requires operational controls and client logout/disconnection.
- Hybrid location switching requires cloud download and local persistence before activation.
- Cross-location catalogue adds upfront schema and API design work.
- Permission checks must consistently validate location access.

## Alternatives Considered

1. **Single-location first, multi-location later** — Rejected. Multi-location is a core product promise and retrofitting it is expensive and risky.
2. **Multi-tenancy only, no location tier** — Rejected. Many chains operate multiple venues under one organisation and need location-level isolation for stock, cash, terminals, devices, users, payments, and reporting.
3. **Allow each client to select its own local-server location independently** — Rejected for Local and Hybrid deployments. The local server is the operational authority for the venue/location, and independent client-side location switching would risk stale data, incorrect routing, incorrect prices, and mixed operational records.
4. **Delay cross-location catalogue until a later phase** — Rejected. The management UI can be phased, but the database model and API contracts must support cross-location catalogue early to avoid disruptive schema redesign under live data.

## Resolved Questions

### Should MVP UI expose location switching, or assume a single active location per session?

MVP UI should assume a single active location per operational session.

For Local and Hybrid deployments, the location is set primarily on the local server hardware. POS clients, kitchen displays, bar displays, ordering devices, and printer-connected consoles connect to that server and inherit its active location context.

Changing the location of a local server is an administrative action that requires connected clients to be logged out, disconnected, or forced to reload before the new location becomes active.

For Cloud deployments, authorised users may switch location at login or within the session where permitted. Switching location reloads the active location context from the cloud.

For Local deployments, switching location loads the selected location data from the local database.

For Hybrid deployments, switching location requires the local server to download, validate, and persist the selected location data from the cloud before the new location becomes active locally.

### Should cross-location product catalogue be a Phase 1 or Phase 3 feature?

Cross-location product catalogue must be a Phase 1 data model and API requirement.

The full catalogue administration UI may be phased, but the schema must support cross-location catalogue from the beginning.

This avoids avoidable future migrations where existing tables need to be structurally changed because the original model assumed single-location catalogue data.

Migrations should primarily add functionality, not repair early schema decisions that failed to account for known multi-location requirements.

## Implementation Guidance

Implementation agents, including Claude Code, must treat this ADR as establishing the following constraints:

- Do not build single-location-only assumptions into the domain model.
- Do not create special single-location logic that must later be removed.
- Always model a single-location customer as a tenant with one location.
- Include `LocationId` or an equivalent location context on operational records that belong to a venue/location.
- Include tenant and location context in relevant API contracts.
- Include tenant and location context in audit records.
- Include tenant and location context in sync state and sync conflict records.
- Treat local server location binding as a server-side setting, not a client-side preference.
- Do not allow local clients to independently switch the local server's active location.
- Require logout, disconnect, or forced reload of local clients when the local server's active location changes.
- For Hybrid mode, download and validate location data before activating a new local server location.
- For Cloud mode, allow location switching only when the user is authorised for the target location.
- Build cross-location catalogue support into Phase 1 schema and API contracts.
- Phase advanced catalogue UI if required, but do not delay the underlying cross-location catalogue model.
- Prefer default-and-override catalogue modelling over duplicated per-location product records.

## Related Documents

- [ADR-0001 — Single Codebase](ADR-0001-single-codebase.md)
- [Architecture: Tenancy](../../architecture/tenancy.md)
- [Architecture: Multi-Location](../../architecture/multi-location.md)
- [PLAN-0003 — Identity, Tenancy, Locations, Devices](../../plans/active/PLAN-0003-identity-tenancy-locations-devices.md)
