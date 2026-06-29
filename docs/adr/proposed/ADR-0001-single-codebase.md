# ADR-0001 — Single Codebase for All Deployment Modes

## Status

Proposed

## Context

Daxa POS must support cloud, local (on-premises), and hybrid deployments. Early in the product's design, a decision was needed on whether to maintain separate codebases for each deployment mode or to use one shared codebase controlled by configuration and infrastructure.

Separate codebases would allow simpler per-mode specialisation but would immediately create divergence, increased maintenance burden, inconsistent feature delivery, and fragmentation of the product identity.

Daxa POS also needs to support different runtime environments:

- Local and hybrid deployments run a local server at each venue/location.
- The local server runs on Linux.
- The local server hosts the server application, local database, sync services, scheduled jobs, and local integration services required by that venue/location.
- Client devices may run Windows, iOS, or Android.
- Client devices connect to the local Linux server or, in cloud deployments, to the Daxa-hosted cloud service.

This means deployment mode must be determined by server-side configuration and licence activation, not by the client device operating system or by separate product codebases.

## Decision

Daxa POS will use a **single codebase** for all deployment modes.

- Daxa Cloud, Daxa Local, and Daxa Hybrid are deployment modes, not separate products.
- Deployment mode differences are handled through configuration, environment variables, infrastructure setup, licence activation, cloud configuration manifests, service registration, and feature flags.
- All tenants, regardless of deployment mode, share the same domain model, API contracts, and data schema.
- Differences in behaviour between modes, such as a local server endpoint versus a cloud API endpoint, are abstracted through configuration and service registrations, not separate source trees.
- Local and hybrid server deployments run on Linux.
- Windows, iOS, and Android devices are client platforms only and do not determine deployment mode.
- Client applications may have platform-specific builds where required, but they must use the same server API contracts and shared domain model.

## Deployment Modes

Daxa POS supports three deployment modes:

- Local
- Hybrid
- Cloud

The deployment mode defines where the operational server runs, where the source-of-truth data is held, and whether cloud synchronisation is enabled.

### Local Mode

In Local mode, each venue/location runs a local Daxa POS server connected to a local database.

The local Linux server is the operational endpoint for the venue/location.

All local devices connect to this local server, including:

- POS terminals
- MAUI applications
- PWA terminals
- ordering devices
- kitchen display screens
- bar display screens
- printer-connected consoles
- other local peripherals and integrations

In this mode, the local server and local database are the source of truth.

Local mode must continue operating without internet connectivity.

Remote backup is optional. A customer may configure their own third-party backup provider, or they may use Daxa-managed backup services where licensed.

### Hybrid Mode

In Hybrid mode, each venue/location runs a local Daxa POS server connected to a local database.

The local Linux server remains the operational endpoint for in-store devices. POS terminals, kitchen displays, bar displays, printers, ordering devices, and local consoles continue to connect locally.

The cloud service provides:

- central management
- cloud backup
- remote administration
- tenant/account configuration
- bidirectional data synchronisation
- disaster recovery support

The local database is synchronised with the cloud account associated with the licence key.

Changes made locally are pushed to the cloud.

Changes made in the cloud admin portal are pulled down to the relevant local server.

This requires a sync service that can:

- identify the tenant
- identify the venue/location
- authenticate the local server against the cloud
- download the cloud configuration manifest
- pull cloud-side changes into the local database
- push local changes to the cloud database
- manage conflict detection and resolution
- run scheduled sync and maintenance jobs

Hybrid mode must continue operating locally during internet outages, although cloud sync, remote administration, and cloud backup will be delayed until connectivity is restored.

### Cloud Mode

In Cloud mode, Daxa hosts and manages the cloud application stack.

Customers access a Daxa-hosted cloud web application for administration and, where enabled, cloud-hosted POS terminal access.

In this mode, POS terminals may connect directly to the Daxa-hosted PWA/API instead of a local venue server.

Cloud mode uses the same application codebase, domain model, API contracts, and data schema as Local and Hybrid modes, but is deployed into Daxa-managed cloud infrastructure.

Cloud mode requires central identity and tenant isolation. Keycloak will be used for cloud-hosted authentication, tenant-aware access control, and identity separation.

Where a local server is also deployed for a cloud customer, it acts as a local edge/cache node rather than a separate product or separate codebase.

## Runtime Deployment Mode Resolution

Deployment mode is communicated to the application through installation configuration, environment variables, licence activation, and a cloud-provided configuration manifest.

The application must not infer deployment mode from:

- the source code branch
- the compiled binary
- the Docker image name alone
- the client operating system
- the presence of Windows, iOS, or Android client devices

Deployment mode is resolved at application startup using the following order of precedence:

1. Local installation configuration
2. Environment variables
3. Licence activation response
4. Cloud-provided signed configuration manifest

The local installation configuration provides the minimum required settings for the Linux server to start.

Environment variables may override installation defaults for containerised deployment.

The licence activation response determines tenant association, location association, entitlement, and whether cloud connectivity is enabled.

The cloud-provided configuration manifest refines runtime behaviour by providing tenant/location-specific configuration, sync schedules, backup settings, enabled features, cloud API endpoints, and maintenance job settings.

At installation time, the local server is configured with:

- `DeploymentMode`
- `LicenceKey`
- `CloudBaseUrl` or cloud region, where applicable
- local database connection details
- local network hostname or IP address, such as `pos.local`
- optional sync settings
- optional backup settings

The deployment mode value may be one of:

- `Local`
- `Hybrid`
- `Cloud`

## Licence and Cloud Configuration

The licence key is the primary activation mechanism for a local installation.

After installation, the administrator enters the licence key into the local server configuration screen.

The local server uses this licence key to activate the installation and determine:

- whether the installation is Local, Hybrid, or Cloud-connected
- which tenant account it belongs to
- which venue/location it represents
- which cloud region it should connect to
- which features are enabled
- whether cloud sync is enabled
- whether Daxa-managed backup is enabled
- whether cloud administration is enabled

The licence key itself should not contain all configuration.

Instead, the licence key allows the local server to securely retrieve a signed configuration manifest from the Daxa cloud service.

The local server persists this configuration locally so it can continue operating during internet outages.

The cloud configuration manifest may include:

- tenant ID
- location ID
- enabled features
- sync schedules
- backup schedules
- cloud API endpoints
- allowed device types
- printer routing configuration
- kitchen display routing configuration
- bar display routing configuration
- licence entitlements
- maintenance job configuration

The cloud configuration manifest must be treated as server-side configuration. It must not become a replacement for domain data, tenant data, or operational POS records.

## Docker Image Strategy

Daxa POS local installations will run as Docker containers on a Linux server.

The default approach is to use a single Linux-based Docker image for the server application.

The same server application image may be deployed as:

- a Local mode server
- a Hybrid mode local server
- a cloud-connected local edge node
- a cloud-hosted application node, where practical
- a cloud-hosted PWA/API instance, where practical

Mode-specific behaviour is controlled through:

- environment variables
- `appsettings` configuration
- licence activation
- signed cloud configuration manifests
- feature flags
- dependency injection/service registration

Separate Docker images may be produced for infrastructure convenience, hosting optimisation, or security hardening, but they must be built from the same source code and must not create separate product codebases.

Client applications may have separate platform-specific builds where required, such as MAUI builds for Windows, iOS, or Android.

These client builds must use the same server API contracts and must not introduce separate deployment-mode-specific product logic.

## Client Device Model

Client devices connect to either:

- the local Linux server, for Local and Hybrid deployments; or
- the Daxa-hosted cloud service, for Cloud deployments.

Supported client platforms include:

- Windows
- iOS
- Android

Supported client types may include:

- Windows POS terminals
- iOS tablets or terminals
- Android tablets or terminals
- MAUI applications
- PWA/browser terminals
- kitchen display screens
- bar display screens
- ordering tablets
- printer-connected consoles

Client devices do not host the source-of-truth database.

The client operating system does not determine deployment mode.

Deployment mode is determined by the server configuration, licence activation, and cloud configuration manifest.

## Identity and Authentication

Local and Hybrid deployments must support local authentication so the venue can continue operating if internet access is unavailable.

Cloud-hosted authentication and central administration will use Keycloak.

In Cloud mode, Keycloak provides:

- user authentication
- tenant-aware access control
- role/group mapping
- admin portal login
- cloud PWA login
- identity separation between tenants

In Local and Hybrid modes, local users may authenticate against the local server using configured login methods such as:

- username/password
- username/PIN
- device-specific credentials

Cloud sync between the local server and Daxa cloud must use server-to-server credentials.

Cloud sync must not use end-user session tokens.

## Sync and Backup Boundary

Sync is a service boundary, not a separate product implementation.

Local, Hybrid, and Cloud modes must share the same core domain logic, data schema, validation rules, and API contracts.

Hybrid mode introduces additional sync behaviour, but this must be implemented as a service layer around the shared domain model.

The sync service is responsible for:

- detecting local changes that need to be pushed to cloud
- detecting cloud changes that need to be pulled locally
- preserving tenant and location boundaries
- recording sync state
- recording sync failures
- supporting retry behaviour
- supporting conflict detection
- applying conflict resolution rules

Backup is separate from sync.

A backup service may provide recoverability, but it must not be treated as the primary operational sync mechanism.

## Scheduled Jobs

Local and Hybrid deployments may require scheduled jobs on the local Linux server.

Scheduled jobs may include:

- cloud sync
- local backup
- cloud backup
- maintenance tasks
- log cleanup
- health checks
- licence refresh
- configuration manifest refresh

These jobs may use cron-style scheduling, hosted workers, background services, or container orchestration depending on the deployment environment.

The ADR does not require direct use of Linux `cron`, but it permits Linux-native scheduling where appropriate.

## Consequences

### Positive

- All deployment modes receive the same features when they are ready.
- Bug fixes and improvements apply across all modes simultaneously.
- A single test suite can cover shared behaviour across all modes.
- Product identity remains unified.
- Long-term maintenance cost is reduced.
- Local venues can continue operating during internet outages.
- Cloud-connected customers can centrally manage data and recover from site loss.
- Licence activation controls entitlement, tenant association, and sync behaviour.
- Daxa can offer local-only, hybrid, and fully cloud-hosted options without fragmenting the product.
- Cloud-hosted deployments can use Keycloak for stronger tenant isolation and central identity management.
- Linux-based server deployment provides a consistent local server target.

### Negative

- Configuration and environment handling becomes more complex.
- Engineers must be careful not to introduce mode-specific hard-coding.
- Some deployment-mode-specific behaviour must be carefully tested per mode.
- Sync logic becomes a core product concern.
- Conflict handling must be designed carefully.
- Local and cloud schemas must remain compatible.
- Configuration activation must be secure and auditable.
- Support tooling must clearly identify tenant, location, deployment mode, and sync state.
- Offline local authentication and cloud authentication introduce separate identity concerns that must be reconciled carefully.
- Linux server deployment narrows the supported server operating system target and requires clear installation/support documentation.

## Alternatives Considered

1. **Separate codebases per deployment mode** — Rejected. Creates divergence, duplication, inconsistent feature delivery, inconsistent domain logic, and increased maintenance burden.
2. **Monorepo with separate server apps per mode** — Partially considered. Some code sharing is possible, but separate apps still encourage divergence of core domain logic and deployment-specific behaviour.
3. **Cloud-only deployment** — Rejected. Does not meet the requirement for local/offline venue operation.
4. **Local-only deployment** — Rejected. Does not support central administration, cloud backup, cloud-hosted management, or disaster recovery scenarios.
5. **Client-hosted server model** — Rejected. Windows, iOS, and Android devices are clients only. The local server target is Linux.

## Implementation Guidance

Implementation agents, including Claude Code, must treat this ADR as establishing the following constraints:

- Do not create separate server codebases for Local, Hybrid, and Cloud modes.
- Do not create Windows-hosted, iOS-hosted, or Android-hosted server assumptions.
- Assume the local server runs on Linux.
- Assume local server deployment is containerised with Docker.
- Treat Windows, iOS, and Android as client platforms only.
- Use configuration, licence activation, feature flags, and service registration to control deployment-mode behaviour.
- Keep domain models, database schema, validation rules, and API contracts shared across deployment modes.
- Implement cloud sync as a service boundary, not as duplicated cloud/local business logic.
- Keep Local mode functional without internet connectivity.
- Keep Hybrid mode operational locally during internet outages.
- Use server-to-server credentials for local-to-cloud sync.
- Do not use end-user login sessions for sync authentication.
- Do not use the client operating system to determine deployment mode.
- Do not fork business logic by deployment mode unless explicitly approved by a later ADR.

## Resolved Questions

### How will deployment mode be communicated to the application at runtime?

Deployment mode is communicated through local installation configuration and environment variables, then refined by licence activation and a cloud-provided signed configuration manifest.

The local server starts with local configuration, activates using its licence key, then retrieves and persists the cloud configuration manifest where applicable.

### Will a single Docker image be used for all modes, or will separate images be produced from the same source?

The default approach is a single Linux-based Docker image built from the same codebase for the server application.

Separate images may be created for deployment convenience, hosting optimisation, or security hardening, but they must be built from the same source and must not introduce separate mode-specific codebases.

## Related Documents

- [ADR-0002 — Cloud, Local, Hybrid Deployment Modes](ADR-0002-cloud-local-hybrid-deployment.md)
- [PLAN-0002 — Platform Skeleton](../../plans/active/PLAN-0002-platform-skeleton.md)
- [OI-0003 — Local Server Reference Hardware](../../issues/open/OI-0003-local-server-reference-hardware.md)
