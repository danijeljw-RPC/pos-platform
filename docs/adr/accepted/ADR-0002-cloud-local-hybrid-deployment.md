# ADR-0002 — Cloud, Local, and Hybrid Deployment Modes

## Status

Accepted

## Context

Different venues have different infrastructure requirements. Venues with reliable internet may operate fully in the cloud. Venues that require operational continuity during internet outages (e.g. food trucks, remote sites, high-volume counters) need a local server. Some organisations want the control benefits of a local server combined with the management and reporting benefits of a cloud connection.

The platform must accommodate all three scenarios without forking the codebase.

## Decision

Daxa POS will support three named deployment modes:

**Daxa Cloud** — Fully cloud-hosted. All operational data, APIs, and reporting run in Daxa-managed cloud infrastructure. Venue devices connect to the cloud to process orders and payments.

**Daxa Local** — Local/on-premises. A Daxa Local Server runs inside the venue's network. The local server operates as the authoritative runtime for that site during trading. Internet connectivity is not required for day-to-day operations.

**Daxa Hybrid** — Combines cloud and local. The cloud provides central management, multi-location reporting, backups, and updates. The local server provides operational continuity, local device control, local payment routing, and resilience when internet access fails. Data syncs between local and cloud.

All three modes use the same codebase, same domain model, and same API concepts.

## Consequences

**Positive:**
- Venues choose the deployment model that fits their infrastructure and business risk tolerance.
- Internet outages do not automatically stop trading for Local and Hybrid venues.
- Central reporting and management remain available in Hybrid and Cloud modes.
- Single codebase and test suite apply across all modes.

**Negative:**
- All three modes must be designed for and tested, increasing upfront design complexity.
- Sync and conflict resolution (Hybrid mode) adds engineering effort.
- Local server hardware must be specified and supported.

## Alternatives Considered

1. **Cloud-only** — Rejected. Does not meet the needs of food trucks, remote locations, or venues with unreliable internet.
2. **Local-only** — Rejected. Does not meet the needs of multi-location chains needing central reporting.

## Open Questions

- See [OI-0003 — Local Server Reference Hardware](../../issues/open/OI-0003-local-server-reference-hardware.md)
- See [OI-0008 — Cloud Data Region Strategy](../../issues/open/OI-0008-cloud-data-region-strategy.md)
- How will deployment mode be detected and enforced at runtime?
- Will Daxa Local require a Daxa-issued device certificate for trust?

## Related Documents

- [ADR-0001 — Single Codebase](ADR-0001-single-codebase.md)
- [ADR-0007 — Local/Hybrid Sync Principles](ADR-0007-local-hybrid-sync-principles.md)
- [PLAN-0007 — Sync, Local, Hybrid](../../plans/active/PLAN-0007-sync-local-hybrid-planning.md)
- [Architecture: Deployment Modes](../../architecture/deployment-modes.md)

---

## Appended Resolution — Open Questions

This section answers the open questions in this ADR and records the current deployment-mode decisions that relate to the referenced open issues.

The original ADR content above remains unchanged. These additions refine how the deployment modes are implemented and enforced.

## Resolution: OI-0003 — Local Server Reference Hardware

Daxa Local and Daxa Hybrid deployments require a local Linux server running inside the venue network.

The local server is responsible for hosting:

- the Daxa POS server application
- the local PostgreSQL database
- local identity services, where required
- local sync services
- local background workers
- local scheduled jobs
- local device integration services
- local printer, display, and payment-routing integrations

The local server must run Linux and must use Docker-based deployment.

### Minimum Local Server Specification

The minimum supported production specification is:

- x86-64 Intel/AMD CPU
- 4 CPU cores
- 8 GB RAM
- 256 GB NVMe SSD
- wired Ethernet
- Linux operating system
- Docker runtime
- reliable local power
- local network access to POS terminals, displays, printers, and payment devices

This is the minimum supportable configuration for small venues and low-to-medium transaction volume environments.

### Recommended Local Server Specification

The recommended production specification is:

- x86-64 Intel/AMD CPU
- 4 or more CPU cores
- 16 GB RAM
- 512 GB NVMe SSD
- wired Ethernet
- Linux operating system
- Docker runtime
- reliable local power
- optional UPS
- local network access to POS terminals, displays, printers, and payment devices

This is the preferred configuration for venues that use multiple terminals, kitchen displays, bar displays, printers, background sync, local reporting, and local identity services.

### Unsupported Production Hardware

Raspberry Pi and other ARM-based single-board computers are not supported for production Daxa Local or Daxa Hybrid deployments.

They may be used for development experiments only, but they must not be treated as supported venue hardware.

The reason is that Daxa Local requires predictable support for Docker images, PostgreSQL, identity services, sync services, and background workers. ARM compatibility and memory limits introduce avoidable support risk.

### Hardware Supply Model

Daxa may support both:

1. Daxa-supplied or Daxa-recommended reference hardware.
2. Venue-supplied hardware that meets or exceeds the minimum supported specification.

Venue-supplied hardware is acceptable only if it meets the supported specification and can run the required Linux and Docker deployment model.

Daxa support documentation must clearly distinguish between:

- minimum supported specification
- recommended specification
- Daxa-tested reference devices
- unsupported hardware

If a venue uses hardware below the minimum supported specification, that installation is outside the supported production baseline.

## Resolution: OI-0008 — Cloud Data Region Strategy

For the AU/NZ launch, Daxa Cloud will initially use an Australian cloud region.

The initial preferred region is:

- AWS `ap-southeast-2` Sydney

This gives Daxa a pragmatic AU-hosted launch region for cloud-hosted tenants, hybrid sync, backup, administration, and reporting.

### Initial AU/NZ Region Strategy

The initial AU/NZ strategy is:

- Use a single Australian primary region for AU/NZ launch.
- Store AU/NZ tenant operational data in the Australian primary region by default.
- Keep backups and audit data in the Australian region by default.
- Use multi-availability-zone infrastructure within the selected Australian region where practical.
- Revisit NZ-specific data residency before any NZ enterprise or regulated customer launch that requires NZ-specific commitments.

This ADR does not approve a separate NZ cloud region yet.

A later ADR is required before introducing:

- a separate NZ-hosted region
- cross-country active/active replication
- cross-region tenant movement
- customer-selectable data residency
- non-Australian storage of AU/NZ tenant operational data

### Data Residency Commitment

For the initial AU/NZ launch, Daxa should commit that AU/NZ tenant operational data is stored in the Australian primary cloud region unless a customer agreement, later ADR, or required third-party integration explicitly allows otherwise.

Operational data includes:

- tenant configuration
- venue/location configuration
- product catalogue data
- menu and pricing configuration
- order records
- payment/refund records held by Daxa
- audit logs
- sync logs
- backup data

Payment provider data remains subject to the payment provider's own storage, processing, and compliance model.

### Disaster Recovery and Backup

The initial disaster recovery approach is regional resilience within the Australian primary region.

This includes:

- backups
- database snapshots
- infrastructure-as-code redeployment
- multi-availability-zone services where practical
- documented restoration procedures

Cross-region disaster recovery is not approved by this ADR.

A later ADR must define any secondary region, replication model, RPO/RTO targets, and data residency impacts before cross-region disaster recovery is implemented.

## Resolution: Runtime Deployment Mode Detection and Enforcement

Deployment mode is detected and enforced at server runtime, not by maintaining separate codebases.

Deployment mode must not be inferred from:

- the Git branch
- the source folder
- the compiled binary
- the Docker image name alone
- the client operating system
- the type of POS terminal connected to the server

Deployment mode is resolved from:

1. local installation configuration
2. environment variables
3. licence activation response
4. signed cloud configuration manifest
5. locally persisted signed configuration manifest when offline

### Bootstrap Behaviour

The local Linux server starts with local bootstrap configuration.

The bootstrap configuration contains enough information for the server to start, including:

- deployment mode
- local database connection
- local hostname or IP address
- licence key or installation token
- cloud base URL or activation endpoint, where applicable

If the installation is cloud-connected, the local server activates against the Daxa cloud service and retrieves a signed configuration manifest.

If the local server is offline after a successful activation, it may continue using the most recent valid persisted manifest.

If the local server has never been activated and requires cloud entitlement, it must remain in an unactivated or restricted setup state until activation is completed.

### Signed Configuration Manifest

The signed configuration manifest may define:

- deployment mode
- tenant ID
- location ID
- cloud region
- cloud API base URL
- enabled features
- licence entitlements
- sync policy
- backup policy
- device trust requirements
- allowed client types
- maintenance schedule
- manifest expiry/refresh rules

The local server must validate the manifest before applying it.

Invalid, expired, tampered, or tenant-mismatched manifests must not be applied silently.

### Runtime Enforcement

The application enforces deployment mode through:

- startup validation
- options/configuration binding
- feature flags
- dependency injection/service registration
- background worker registration
- API authorization policies
- entitlement checks
- sync service enablement/disablement
- audit logging

Examples:

- Daxa Cloud enables cloud tenant identity, cloud APIs, cloud-hosted admin, and cloud-hosted POS access.
- Daxa Local enables local trading, local database, local device control, local reporting, and optional backup.
- Daxa Hybrid enables local trading plus cloud sync, cloud backup, remote administration, and central reporting.
- Daxa Local must not require internet connectivity for day-to-day trading.
- Daxa Hybrid must continue trading locally if cloud sync is unavailable.
- Daxa Cloud requires internet connectivity because the operational endpoint is cloud-hosted.

Deployment mode changes must be explicit, audited, and controlled by licence/manifest update.

The application must not allow a venue to silently switch deployment modes by editing an arbitrary local setting without validation.

## Resolution: Daxa-issued Device Certificate for Trust

Daxa Local and Daxa Hybrid installations that connect to Daxa Cloud require a Daxa-issued device certificate or equivalent Daxa-issued device credential.

This credential establishes trust between the local Linux server and Daxa Cloud.

The credential is used for server-to-server communication only.

It must not be used as an end-user login token.

It must not be installed on individual POS terminals, tablets, kitchen displays, or bar displays.

### Certificate Purpose

The Daxa-issued device certificate or credential is used to:

- identify the local server installation
- bind the local server to a tenant
- bind the local server to a venue/location
- authenticate local-to-cloud sync
- authenticate configuration manifest refresh
- authenticate cloud backup where enabled
- support revocation if a device is lost, stolen, replaced, or compromised

### Certificate Scope

The certificate or credential is scoped to:

- one tenant
- one venue/location
- one local server installation
- one deployment environment
- one cloud region, where applicable

It should not grant broad access across tenants, venues, or regions.

### Local Mode

Pure Daxa Local deployments that do not use Daxa Cloud for sync, backup, activation refresh, or remote administration may operate without an always-online cloud trust channel.

However, the installation should still have a local installation identity.

If offline activation is required, Daxa may provide an offline signed activation file or equivalent signed licence manifest.

### Hybrid Mode

Daxa Hybrid requires a Daxa-issued device certificate or equivalent device credential because the local server syncs with Daxa Cloud.

Hybrid sync must use server-to-server trust.

Hybrid sync must not use end-user session tokens.

### Cloud Mode

Daxa Cloud does not require a local server device certificate unless a local edge/cache node is deployed.

Cloud-hosted user authentication and tenant access are handled through the cloud identity model, including Keycloak.

### Storage and Rotation

The local Linux server must store the certificate or credential securely.

The credential must support:

- rotation
- expiry
- renewal
- revocation
- replacement during hardware failure
- audit logging of trust changes

The exact certificate storage mechanism may be defined in a later security or deployment ADR.

## Implementation Guidance

Implementation agents, including Claude Code, must treat this appended section as part of ADR-0002.

The following constraints apply:

- Do not create separate codebases for Cloud, Local, and Hybrid deployment modes.
- Do not assume Windows, iOS, or Android devices can act as the local server.
- Assume the local server target is Linux.
- Assume the local server is deployed through Docker.
- Enforce deployment mode through configuration, licence activation, signed manifests, feature flags, service registration, and authorization checks.
- Keep Local mode operational without internet connectivity.
- Keep Hybrid mode operational locally during cloud outages.
- Use the sync principles from ADR-0007 for Hybrid sync.
- Use idempotency keys for sync operations.
- Audit sync events.
- Do not silently overwrite sync conflicts.
- Treat cloud configuration as authoritative for menus, pricing, tax, and device configuration in Hybrid mode.
- Treat locally created operational data, such as orders and payments, as authoritative on the local server until successfully synced.
- Use Daxa-issued server-to-server trust for cloud-connected local servers.
- Do not use end-user session tokens for local-to-cloud sync.
- Use AWS `ap-southeast-2` Sydney as the initial AU/NZ cloud region unless superseded by a later ADR.
- Do not implement cross-region disaster recovery without a later ADR.
- Do not support Raspberry Pi or ARM-based single-board computers as production local server hardware.

## Updated Open Question Status

The original open questions in this ADR are answered as follows:

- **OI-0003 — Local Server Reference Hardware:** Resolved for ADR-0002 purposes. Minimum and recommended local server specifications are defined above.
- **OI-0008 — Cloud Data Region Strategy:** Resolved for initial AU/NZ launch. AWS `ap-southeast-2` Sydney is the initial preferred cloud region.
- **How will deployment mode be detected and enforced at runtime?** Resolved. Deployment mode is resolved by configuration, licence activation, signed manifest, and runtime enforcement controls.
- **Will Daxa Local require a Daxa-issued device certificate for trust?** Resolved. Cloud-connected Local/Hybrid installations require a Daxa-issued device certificate or equivalent server credential.
