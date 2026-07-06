# Deployment: Docker and Docker Compose

Daxa POS uses Docker and Docker Compose for local development and on-premises (Daxa Local) deployment.

See [ADR-0012](../adr/accepted/ADR-0012-docker-local-deployment-strategy.md) for the decision record.

For the full detailed deployment reference, see [docker-deployment.md](docker-deployment.md) — this file contains the comprehensive Docker deployment guide including all environment variables, volumes, ports, health checks, backup procedures, and worker configuration.

---

## Full Local Dev Stack (PLAN-0010)

As of PLAN-0010, the repo root has a `compose.yaml` that runs the **full local dev stack**:
`db`, `keycloak`, a one-shot `migrations` service, `api`, `worker`, and `web` (the PLAN-0006
Blazor WebAssembly PWA, `DaxaPos.Web`). This is local-dev only — plain HTTP, no reverse proxy/TLS,
dev-only default credentials.

Quick start:

```bash
cp .env.example .env
# Edit .env with your local values if needed — the defaults work out of the box
docker compose up -d --build
```

Once the stack is healthy, run the local demo setup helper (PLAN-0011) to prepare the minimum
data the PWA needs — a demo location, a demo staff member with a PIN, and a device registration
PIN:

```bash
./scripts/setup-local-demo.sh
```

The script requires only `curl` and `jq`. It logs in as the bootstrap admin, then reuses or
creates a location named `Local Demo Venue` and a staff member `TEST01` under the bootstrap
organisation, and always issues a fresh, single-use device registration PIN (raw PINs expire
after 15 minutes and are shown only once). It never prints the admin password or admin session
token. It is safe to rerun: rerunning reuses the same location and staff member, resets the
staff member's PIN, and prints the new one — the previous staff PIN stops working. All defaults
are overridable via environment variables (`API_URL`, `PWA_URL`, `ADMIN_EMAIL`,
`ADMIN_PASSWORD`, `DEMO_LOCATION_NAME`, `DEMO_STAFF_CODE`, `DEMO_STAFF_NAME`, `DEMO_STAFF_PIN`).

The script's final output block gives you everything needed to reach the PWA's staff-PIN login:

```text
PWA URL, location ID, device registration PIN + expiry, staff code, current staff PIN
```

1. Open `http://localhost:8080/device-setup` and enter the printed registration PIN.
2. Then log in at `http://localhost:8080/login` with the printed staff code and PIN.

This is local-development tooling only — it makes real API calls and creates real rows in your
local database; it does not seed anything at application startup and does not touch
PostgreSQL directly. See
[docs/testing/local-smoke-test.md](../testing/local-smoke-test.md) for the detailed manual
API walkthrough this script automates the fast path of, and
[docs/plans/active/PLAN-0011-local-demo-setup-helper.md](../plans/active/PLAN-0011-local-demo-setup-helper.md)
for the design/implementation record.

Fresh start (wipes the Postgres volume — use if migrations get into a bad state):

```bash
docker compose down -v --remove-orphans
docker compose up -d --build
```

Check commands:

```bash
docker compose ps
docker compose logs -f migrations
docker compose logs -f api
docker compose logs -f worker
curl http://localhost:5118/health
```

Open the PWA at **<http://localhost:8080/>**. It calls the API at `http://localhost:5118/` — this
is baked into the `web` image via `deploy/web/appsettings.docker.json` at build time, since the
Blazor WebAssembly app runs in the *browser*, which cannot resolve Compose service names. The
`api`/`worker`/`migrations` services, by contrast, talk to Postgres over the Compose network
using the service name `db` (`Host=db;Port=5432;...`) — never `localhost`.

There is no seeded staff member or device-registration PIN yet — Back Office (PLAN-0006
Milestone B) doesn't exist. Run `./scripts/setup-local-demo.sh` (see above) for the fast path, or
call the admin API directly (bootstrap admin credentials come from `DAXA_BOOTSTRAP_ADMIN_EMAIL`
/`_PASSWORD` in `.env.example`) to reach the PWA's Staff PIN login screen after a fresh stack
comes up:

1. `POST http://localhost:5118/api/v1/auth/local/login` with the bootstrap admin email/password.
2. `GET /api/v1/auth/me` with that session token → `OrganisationId`.
3. `POST /api/v1/locations` `{ "Name": "...", "OrganisationId": "<step 2>" }` → `LocationId`.
4. `POST /api/v1/device-registration-pins` `{ "LocationId": "<step 3>" }` → one-time PIN.
5. `POST /api/v1/staff-members` `{ "DisplayName": "...", "StaffCode": "...", "Pin": "...", "LocationId": "<step 3>" }`.
6. `POST /api/v1/staff-members/{id}/roles` `{ "RoleId": "00000000-0000-0000-0001-000000000004" }`
   (the seeded `Staff` role — the only one that's staff-PIN-eligible).
7. In the browser, use the PIN from step 4 on `/device-setup`, then the staff code/PIN from
   step 5 on `/login`.

`deploy/docker-compose.yml` (below) is the older, infra-only PLAN-0002 file (`db`+`keycloak`
only) and is unchanged — it still works for the `dotnet run` host-process workflow. Don't run
both stacks at once; they both bind host port 5432 for Postgres.

---

## Legacy Infra-Only Compose File (PLAN-0002)

`deploy/docker-compose.yml` runs **only** `db` (PostgreSQL) and `keycloak`, for developers who
prefer to run `DaxaPos.Api`/`DaxaPos.Workers` directly on the host via `dotnet run`.

```bash
cd deploy
cp .env.example .env
# Edit .env with your local values
docker compose up -d db          # keycloak is optional: add "keycloak" to also start it
cd ..
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet run --project src/DaxaPos.Api
```

Health check:

```bash
curl http://localhost:5118/health
```

(Port depends on the `ASPNETCORE_URLS`/launch profile the API binds to when run directly.)

---

## Docker Compose Services

| Service | Purpose | Status |
|---------|---------|--------|
| `db` | PostgreSQL database | Implemented (PLAN-0002) |
| `keycloak` | Identity provider (cloud/admin/back-office only, ADR-0013) | Implemented (PLAN-0002), not yet used by application code |
| `migrations` | One-shot EF Core `database update` | Implemented (PLAN-0010), root `compose.yaml` only |
| `api` | ASP.NET Core Web API (DaxaPos.Api) | Containerised (PLAN-0010), root `compose.yaml` only |
| `worker` | Background workers (DaxaPos.Workers) | Containerised (PLAN-0010), root `compose.yaml` only |
| `web` | Blazor WebAssembly PWA (DaxaPos.Web) | Containerised (PLAN-0010), root `compose.yaml` only |
| `proxy` | Reverse proxy / TLS termination | Not yet created — out of scope, local-dev is plain HTTP |

---

## Deployment Mode

Set the deployment mode via environment variable:

```text
DAXA_DEPLOYMENT_MODE=Cloud
DAXA_DEPLOYMENT_MODE=Local
DAXA_DEPLOYMENT_MODE=Hybrid
```

---

## Related Documents

- [docker-deployment.md](docker-deployment.md) — full reference
- [Deployment: Local](local.md)
- [Deployment: Hybrid](hybrid.md)
- [ADR-0012 — Docker Local Deployment Strategy](../adr/accepted/ADR-0012-docker-local-deployment-strategy.md)
- [PLAN-0002 — Platform Skeleton](../plans/active/PLAN-0002-platform-skeleton.md)
- [PLAN-0010 — Docker Compose Local Dev Stack](../plans/active/PLAN-0010-docker-compose-local-dev-stack.md)
