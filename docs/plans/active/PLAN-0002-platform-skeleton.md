# PLAN-0002 — Platform Skeleton

## Status

Partially complete — neutral skeleton implemented 2026-07-01 (solution, projects, EF Core migration, Docker Compose db/keycloak, health check, CI skeleton, minimal domain-event abstraction). Not yet committed to git (pending explicit commit instruction). Business logic, identity/RBAC, and module projects remain out of scope by design — see PLAN-0003 onward.

## Goal

Stand up a working but empty Daxa POS platform skeleton. This includes the .NET solution, the API host, the PostgreSQL database with EF Core, Docker Compose for local development, and a basic health check endpoint. No business logic yet — just a running, connected system.

## Scope

- .NET solution with project structure from PLAN-0001.
- ASP.NET Core API host (`DaxaPos.Api`).
- PostgreSQL via Docker Compose.
- EF Core `DbContext` with initial migrations.
- Keycloak via Docker Compose (basic setup).
- Docker Compose file for local dev stack.
- Health check endpoint (`/health`).
- Basic CI pipeline skeleton.

## Non-goals

- Business logic (orders, payments, tax, etc.).
- Full identity/RBAC implementation.
- UI (MAUI or PWA).
- Payment provider integrations.

## Context Read

- `docs/plans/active/PLAN-0001-architecture-foundation.md`
- `docs/adr/accepted/ADR-0001-single-codebase.md`
- `docs/adr/accepted/ADR-0013-cloud-identity-and-local-pos-authentication-strategy.md`
- `docs/adr/accepted/ADR-0012-docker-local-deployment-strategy.md`
- `docs/deployment/docker.md`

## Files Likely To Change

```text
DaxaPos.sln
Directory.Build.props
src/DaxaPos.Api/
src/DaxaPos.Domain/
src/DaxaPos.Application/
src/DaxaPos.Infrastructure/
src/DaxaPos.Persistence/
tests/DaxaPos.Api.Tests/
deploy/docker-compose.yml
deploy/.env.example
.github/workflows/ci.yml
```

## Architecture Assumptions

- .NET 9 Web API (`net9.0` TFM). `Directory.Build.props` sets `RollForward=LatestMajor` as a build-environment accommodation for hosts that only have a later runtime installed — it does not change the declared TFM.
- PostgreSQL 16 (`postgres:16-alpine`).
- EF Core / Npgsql provider pinned to 9.0.4 (the lowest mutually-compatible patch set at implementation time — see Handoff Notes).
- Keycloak (`quay.io/keycloak/keycloak:26.0`, dev mode). Present in Docker Compose but **not wired into any application code yet** — no JWT bearer middleware, no `[Authorize]` usage. That arrives with PLAN-0003.
- Docker Compose v2, compose file at `deploy/docker-compose.yml` (not repo root), matching the existing convention in `docs/deployment/docker.md`.
- No `DaxaPos.Workers`, reverse proxy, or API container/Dockerfile in this pass — only `db` and `keycloak` run in Compose; the API runs via `dotnet run` against them. These arrive in later plans (see `docs/deployment/docker-deployment.md`'s fuller target stack).

## Domain Assumptions

- Tenant, Organisation, Location, Device, Terminal tables exist in the initial schema. Device and Terminal are modelled as separate concepts: a `Terminal` has an optional (nullable) `DeviceId`; a `Device` has no required link back to a terminal.
- Multi-tenant query filters are explicitly **out of scope for this plan** (descoped 2026-07-01) — they require the identity/tenancy context built in PLAN-0003 and are implemented there, not here. This plan only creates the tenant-scoped tables and FK constraints.

## Risks

- ~~Keycloak setup complexity on first run.~~ Mitigated: Keycloak has no application code depending on it yet, so it cannot block the skeleton; it can even be left stopped entirely (verified).
- EF Core multi-tenant configuration requires careful design early — **descoped from this plan** to PLAN-0003 (see Domain Assumptions); tracked as a risk to *that* plan instead.
- `.env` secrets management must be correct from the start — no real secrets exist yet (dev-only placeholder DB/Keycloak passwords); revisit once real credentials (JWT signing keys, provider secrets) are introduced.

## Implementation / Documentation Steps

1. ✅ Create .NET solution file (`DaxaPos.sln`, classic `.sln` format — the .NET 10 SDK on the build machine defaults `dotnet new sln` to the newer `.slnx` format; `.sln` was selected explicitly via `--format sln` for wider tooling compatibility).
2. ✅ Create project scaffolding (Api, Domain, Application, Infrastructure, Persistence) with the reference graph Api → {Application, Infrastructure, Persistence}, Infrastructure → {Application, Domain}, Persistence → Domain, Application → Domain, per ADR-0014.
3. ✅ Configure `Program.cs` (no Startup.cs; minimal hosting, no controllers).
4. ✅ Add PostgreSQL Docker Compose service (`deploy/docker-compose.yml`, service `db`).
5. ✅ Add Keycloak Docker Compose service (service `keycloak`). Per ADR-0013, scoped to cloud/admin/back-office/support/external identity — verified by smoke test that the API starts and `/health` reports healthy with the `keycloak` container not running at all.
6. ✅ Configure EF Core with PostgreSQL (`DaxaDbContext` in `DaxaPos.Persistence`, registered via `AddDaxaPersistence`).
7. ✅ Create initial migration (Tenant, Organisation, Location, Device, Terminal) — `InitialCreate`, verified against a real Postgres container (`dotnet ef database update` + `\dt` schema check).
8. ✅ Add health check endpoint (`GET /health`, DB-check only — no Keycloak check).
9. ✅ Add `deploy/.env.example` — scoped to the `db`/`keycloak` variables this pass actually uses; the fuller variable set in `docs/deployment/docker-deployment.md` is added incrementally as those features are built.
10. ✅ `.gitignore` — verified the existing root `.gitignore` (standard `dotnet new gitignore` template) already covers `bin/`, `obj/`, and `.env` (including `deploy/.env`); no changes were needed.
11. ✅ Document Docker Compose startup in `docs/deployment/docker.md` / `docker-deployment.md` / `local.md`.
12. ⬜ Update `docs/plans/active/planning-session-worker-notes.md` — not updated this pass; this plan file and the readiness report serve as the handoff record instead.
13. ✅ (Added, not in original plan) Minimal `IDomainEvent`/`IDomainEventDispatcher`/in-process dispatcher/DI registration, per ADR-0014's Acceptance Addendum.
14. ✅ (Added, not in original plan) Basic CI skeleton (`.github/workflows/ci.yml`) with a Postgres service container, matching PLAN-0008's "real Postgres, no mocks" testing philosophy.
15. ✅ (Added, not in original plan) `DaxaPos.Api.Tests` with a `WebApplicationFactory`-based health check integration test.

## Tests To Run Later

- ✅ `dotnet build DaxaPos.sln` passes (0 warnings, 0 errors).
- ✅ `docker compose up -d db` (from `deploy/`) brings up PostgreSQL; `keycloak` was deliberately left down for the verification pass to prove independence, and separately confirmed to start cleanly via `docker compose up -d keycloak`.
- ✅ `GET /health` returns 200 "Healthy" — verified twice: via the `DaxaPos.Api.Tests` integration test (`WebApplicationFactory`) and via a manual `dotnet run` + `curl` smoke test, in both cases with the `keycloak` container not running.
- ✅ EF Core migration applies cleanly (`dotnet ef database update` against the real `db` container; confirmed via `psql \dt` that `tenants`, `organisations`, `locations`, `devices`, `terminals`, and `__EFMigrationsHistory` all exist).
- ⬜ CI workflow (`.github/workflows/ci.yml`) has not been run on GitHub itself — only the equivalent steps were run locally.

## Documentation To Update

- `docs/deployment/docker.md`
- `docs/deployment/local.md`

## ADRs Required

- ADR-0001, ADR-0012, ADR-0013, ADR-0014 (all already accepted). Per ADR-0014's Acceptance Addendum, this plan's communication scaffolding is limited to `IDomainEvent`/`IDomainEventDispatcher`/a simple in-process dispatcher/DI registration — no real events, handlers, or outbox table yet.

## Open Issues Required

- None new; see existing open issues.

## Commit Sequence

Not yet committed — no commit has been made for this work; it remains in the working tree pending an explicit commit instruction. Recommended sequence once approved:

```text
chore: scaffold Daxa POS .NET solution and project skeleton
feat(application,infrastructure): add minimal domain event dispatcher (ADR-0014)
feat(persistence): add Tenant/Organisation/Location/Device/Terminal entities and initial migration
feat(api): add health check endpoint
infra: add Docker Compose for local dev stack (db, keycloak)
ci: add basic GitHub Actions build/test workflow
test(api): add health check integration test
docs: update deployment docs and readiness report for platform skeleton
```

## Handoff Notes

This plan depends on PLAN-0001 (Architecture Foundation). The neutral skeleton is implemented and verified locally (build, migration, health check with and without Keycloak running) but **not committed**.

Environment note for whoever runs this next: the build machine used for this pass had only the .NET 10 runtime installed, no net9.0 runtime component. `Directory.Build.props` at the repo root sets `RollForward=LatestMajor` to work around that without changing the net9.0 TFM; remove it once a net9.0 runtime is confirmed present, or intentionally retarget to net10.0 later.

EF Core / Npgsql package versions were pinned to 9.0.4 across all projects — 9.0.0 (the initial default) triggered `NU1605` package-downgrade errors because `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4 requires `Microsoft.EntityFrameworkCore` ≥ 9.0.1. Keep all EF Core-family packages on the same patch version to avoid this.

Deliberately deferred to later plans (do not add in a "small follow-up" to this plan without a plan refresh, per CLAUDE.md's three-change rule):

- Multi-tenant EF Core query filters, JWT/Keycloak auth wiring, staff PIN login, RBAC — PLAN-0003.
- `DaxaPos.Workers`, reverse proxy, API Dockerfile/container — later infra plan, once something needs them.
- Real domain events, event handlers, outbox table — first plan that actually raises a business event (expected around PLAN-0005/PLAN-0007), per ADR-0014.

The next worker should build on this skeleton to implement identity, tenancy enforcement, and staff sessions (PLAN-0003).
