# Docker Deployment — Daxa POS

## Purpose

This document defines Docker deployment direction for Daxa POS.

Daxa POS must support:

- Local development.
- Daxa Local deployment.
- Daxa Hybrid deployment.
- Daxa Cloud deployment patterns.
- Local server operation.
- Worker processes.
- PostgreSQL persistence.
- Reverse proxy/TLS.
- Backup/restore.
- Health checks.

Docker must support development and local/on-prem deployment. Cloud deployment may use containers or managed services depending on the selected cloud infrastructure.

---

# Deployment Modes

## Daxa Cloud

Cloud-hosted deployment.

```text
Daxa Cloud
├─ API
├─ Database
├─ Worker services
├─ Admin/Back Office
├─ Reporting
├─ Sync endpoints
├─ Payment provider integrations
└─ Monitoring/logging
```

## Daxa Local

On-prem/local deployment.

```text
Daxa Local Server
├─ API container
├─ Database container
├─ Worker container(s)
├─ Reverse proxy container
├─ Optional monitoring/logging containers
└─ Persistent volumes
```

## Daxa Hybrid

Local server plus cloud sync.

```text
Daxa Local Server
├─ API
├─ Database
├─ Workers
├─ Sync worker
└─ Reverse proxy

        ⇅

Daxa Cloud
├─ Sync endpoints
├─ Central reporting
├─ Backup/export
└─ Remote management
```

---

# Expected Local Stack

The expected local Docker stack should include:

```text
docker compose
  db
  api
  worker
  proxy
  optional monitoring/logging
```

## Services

| Service | Purpose |
|---|---|
| `db` | PostgreSQL database |
| `api` | ASP.NET Core API |
| `worker` | Background jobs, sync, print queue, scheduled tasks |
| `proxy` | Reverse proxy/TLS termination |
| `monitoring` optional | Local metrics/logs |
| `backup` optional | Backup job/container |

---

# Phase 1 Local Development Stack

## Services

| Service | Image | Port | Purpose |
|---|---|---:|---|
| `db` | `postgres:16-alpine` or newer | `127.0.0.1:5432` dev only | PostgreSQL database |
| `api` | built from `src/DaxaPos.Api/Dockerfile` | internal `8080` | ASP.NET Core API |
| `worker` | built from `src/DaxaPos.Workers/Dockerfile` | internal | Background jobs |
| `proxy` | `nginx:1.27-alpine` or equivalent | `80`, `443` | Reverse proxy / TLS termination |

## Optional future services

| Service | Purpose |
|---|---|
| `seq` | Local structured log viewing |
| `prometheus` | Metrics |
| `grafana` | Dashboards |
| `backup` | Scheduled DB backup |
| `keycloak` | Local identity server if used |
| `redis` | Cache/message backplane if required |

---

# Suggested Quick Start

```bash
cd deploy
cp .env.example .env
# Edit .env values
docker compose up --build
```

Health check:

```bash
curl http://localhost/api/health
```

If HTTPS is enabled:

```bash
curl https://localhost/api/health
```

---

# Environment Variables

A real `.env` file must never be committed.

Use:

```text
deploy/.env.example
```

## Required variables

```text
POSTGRES_DB
POSTGRES_USER
POSTGRES_PASSWORD
DB_HOST
DB_PORT
DB_NAME
DB_USER
DB_PASSWORD
ASPNETCORE_ENVIRONMENT
DAXA_DEPLOYMENT_MODE
DAXA_PUBLIC_BASE_URL
```

## Security-related variables

```text
JWT_SIGNING_KEY
KEYCLOAK_AUTHORITY
KEYCLOAK_REALM
KEYCLOAK_CLIENT_ID
KEYCLOAK_CLIENT_SECRET
PAYMENT_SECRET_ENCRYPTION_KEY
SYNC_CLIENT_ID
SYNC_CLIENT_SECRET
BACKUP_ENCRYPTION_KEY
```

## Optional variables

```text
DAXA_CLOUD_SYNC_ENDPOINT
DAXA_BACKUP_ENDPOINT
DAXA_LICENCE_ENDPOINT
DAXA_UPDATE_ENDPOINT
LOG_LEVEL
PRINTER_WORKER_ENABLED
SYNC_WORKER_ENABLED
BACKUP_WORKER_ENABLED
PAYMENT_WEBHOOK_BASE_URL
```

---

# Deployment Mode Configuration

Use deployment mode config:

```text
DAXA_DEPLOYMENT_MODE=Cloud
DAXA_DEPLOYMENT_MODE=Local
DAXA_DEPLOYMENT_MODE=Hybrid
```

## Cloud mode

Expected:

- Cloud-hosted API.
- Cloud database.
- Cloud workers.
- Cloud admin/back office.
- Venue devices connect to cloud.

## Local mode

Expected:

- Local API.
- Local database.
- Local workers.
- Local printer/payment routing.
- Optional backup/export.
- No required cloud dependency during trading.

## Hybrid mode

Expected:

- Local API/database/workers.
- Cloud sync enabled.
- Cloud management/reporting enabled.
- Local trading continues if internet drops.
- Sync catches up when connectivity returns.

---

# Volumes

Recommended volumes:

| Volume | Purpose |
|---|---|
| `db_data` | PostgreSQL data |
| `api_logs` | API logs if file logging is used |
| `worker_logs` | Worker logs if file logging is used |
| `backup_data` | Local backup output |
| `certs` | Local TLS certificates |
| `config` | Local runtime config if needed |

Database volume must be persistent across restarts.

---

# Ports

Development example:

| Port | Purpose |
|---:|---|
| `80` | HTTP reverse proxy |
| `443` | HTTPS reverse proxy |
| `5432` | PostgreSQL dev-only local binding |
| `8080` | API internal container port |

Production local deployments should not expose PostgreSQL outside the Docker network unless specifically required and secured.

---

# HTTPS

HTTPS should be supported locally where feasible.

## Local TLS options

- Self-signed certificate.
- Locally trusted certificate.
- Customer-provided certificate.
- Internal CA certificate.
- Reverse proxy TLS termination.

## Example cert path

```text
deploy/nginx/certs/pos.crt
deploy/nginx/certs/pos.key
```

## HTTPS requirements before production

- TLS termination documented.
- Certificate renewal documented.
- Local hostname documented.
- Browser trust procedure documented.
- API base URLs documented.

---

# Local Hostname

Suggested local hostname options:

```text
https://daxa.local
https://pos.local
https://daxapos.local
```

The actual hostname must be configurable.

---

# Health Checks

Required health checks:

| Health check | Purpose |
|---|---|
| API health | API is alive |
| Database health | DB connection works |
| Migration health | DB schema current |
| Worker health | Workers running |
| Sync health | Hybrid/local sync status |
| Printer worker health | Print queue processing |
| Payment provider health | Optional provider connectivity |
| Disk space health | Local server safety |
| Backup health | Last successful backup |

Example endpoint:

```text
GET /api/health
GET /api/health/ready
GET /api/health/live
```

---

# Workers

Worker services may include:

- Print queue worker.
- Sync worker.
- Backup worker.
- Reporting worker.
- Payment webhook/status worker.
- Scheduled cleanup worker.
- Stock movement worker if needed.
- Notification worker later.

Workers must be idempotent where possible.

---

# Backup

Local database backups may be:

- Customer-managed.
- Daxa-managed value-added service.
- Exported encrypted offsite.
- Synced to Daxa Cloud.
- Synced to customer data lake/storage.
- Restorable to same or replacement local server.

## Backup requirements

- Scheduled local DB backup.
- Manual backup command.
- Backup encryption option.
- Backup retention configuration.
- Backup status visible.
- Backup failure alert/log.
- Restore procedure documented.
- Restore test procedure documented.

---

# Restore

Restore docs must include:

- Stop services.
- Select backup.
- Restore database.
- Re-run migrations if required.
- Restart services.
- Verify health.
- Verify tenant/location data.
- Verify recent orders/payments if applicable.
- Verify device registrations.
- Verify printer/payment terminal mappings.
- Verify sync state.

---

# Updates

Updates should be deployed to local server using a documented process.

Possible update models:

- Docker image pull and restart.
- Installer script.
- Managed Daxa update service later.
- Customer-managed update.

## Update requirements

- Pre-update backup.
- Migration check.
- Rollback option.
- Version compatibility.
- PWA refresh/update notes.
- MAUI app update path documented separately.
- Worker update behaviour documented.

---

# Daxa Terminal and Docker

Daxa Terminal is a Windows MAUI app and is not expected to run inside Docker for production.

Docker hosts:

- API.
- Database.
- Workers.
- Reverse proxy.
- Optional supporting services.

Daxa Terminal connects to:

```text
Daxa Cloud API
or
Daxa Local Server API
or
Daxa Hybrid Local Server API
```

---

# Daxa Back Office and PWA Hosting

Daxa Back Office can be hosted by:

- API/static web host.
- Reverse proxy.
- Separate frontend container.
- Cloud static hosting in cloud mode.

This must be decided by ADR when implementation starts.

---

# Docker Smoke Tests

Docker deployment smoke tests must verify:

- `docker compose up --build` succeeds.
- API starts.
- Database starts.
- Worker starts.
- Proxy starts.
- Health endpoint works.
- API can connect to DB.
- Migrations apply.
- Logs are visible.
- Volume persistence works.
- Restart does not lose DB data.

---

# Required Documentation Before Production

Before production use, deployment docs must include:

- Install steps.
- Upgrade steps.
- Rollback steps.
- Backup steps.
- Restore steps.
- Environment variable reference.
- Secret reference.
- Volume reference.
- Port reference.
- Health check reference.
- Local hostname/TLS setup.
- Troubleshooting guide.
- Support access process.
- Monitoring/logging process.

---

# Local Server Recommendation

Recommended deployment target:

- Linux mini PC or small server.
- Wired Ethernet.
- UPS recommended.
- Docker installed.
- Persistent storage.
- Local database volume backed up.
- Optional remote backup service.
- Restricted admin access.

Possible hardware:

- Intel NUC-style mini PC.
- Small business server.
- Industrial mini PC for harsh environments.
- Cloud VM for cloud deployment.

---

# Open Questions

Create open issues for:

- Which reverse proxy should be default: nginx, Caddy, Traefik?
- Should local Keycloak be bundled for local/hybrid?
- Should Redis be included early?
- Should local server support Windows Docker or Linux only?
- How should MAUI app updates be delivered?
- How should customer data lake backup/export be configured?
- What is the default local hostname?
