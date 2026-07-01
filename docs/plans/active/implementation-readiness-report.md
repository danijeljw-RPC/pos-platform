# Implementation Readiness Report — Daxa POS

## Status

Updated 2026-07-01 after PLAN-0002's neutral platform skeleton was implemented and verified. This is the third update to this report; see git history / prior conversation for the first two passes (initial readiness review, then a documentation-only correction pass).

## Purpose

All 14 ADRs are accepted (ADR-0009 superseded by ADR-0013). This report now tracks: what code exists, what was verified, what remains blocked, and what the next plan (PLAN-0003) should do.

---

## What exists now (first code in the repository)

A neutral .NET 9 platform skeleton, scoped and approved for PLAN-0002 only — no business logic, no module projects, no identity/RBAC, no Stripe Terminal.

```
DaxaPos.sln                                (classic .sln format, not .slnx)
Directory.Build.props                      (RollForward=LatestMajor — build-env accommodation, see below)
src/
  DaxaPos.Api/                             ASP.NET Core Empty template, Program.cs only (no controllers)
  DaxaPos.Domain/                          Tenant, Organisation, Location, Device, Terminal entities; IDomainEvent
  DaxaPos.Application/                     IDomainEventDispatcher, IDomainEventHandler<T>
  DaxaPos.Infrastructure/                  InProcessDomainEventDispatcher, AddDaxaInfrastructure() DI extension
  DaxaPos.Persistence/                     DaxaDbContext, entity Fluent configs, AddDaxaPersistence() DI extension,
                                            Migrations/20260701064029_InitialCreate.cs
tests/
  DaxaPos.Api.Tests/                       HealthCheckTests (WebApplicationFactory, real Postgres)
deploy/
  docker-compose.yml                       db (postgres:16-alpine) + keycloak (quay.io/keycloak/keycloak:26.0) only
  .env.example
.github/workflows/ci.yml                   checkout, setup .NET 9, postgres service container, restore/build/test
```

Project reference graph (per ADR-0014): `Api → {Application, Infrastructure, Persistence}`, `Infrastructure → {Application, Domain}`, `Persistence → Domain`, `Application → Domain`. No `Modules.*` projects exist yet, so ADR-0014's module-boundary rule has nothing to enforce yet — it becomes load-bearing starting PLAN-0004/0005.

**Nothing has been committed to git.** Per explicit instruction, no automatic commit was made; the recommended commit sequence is in `PLAN-0002`'s "Commit Sequence" section.

---

## Verification performed (commands run, results)

| Check | Command | Result |
|---|---|---|
| Solution builds | `dotnet build DaxaPos.sln` | **Pass** — 0 warnings, 0 errors, all 6 projects |
| Migration generates | `dotnet ef migrations add InitialCreate --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api` | **Pass** |
| Migration applies to real Postgres | `docker compose up -d db` then `dotnet ef database update ...` | **Pass** — confirmed via `psql \dt`: `tenants`, `organisations`, `locations`, `devices`, `terminals`, `__EFMigrationsHistory` all present with correct FKs |
| Automated test suite | `dotnet test DaxaPos.sln` (against the real `db` container) | **Pass** — 1/1 (`HealthCheckTests.Health_ReturnsHealthy_WhenDatabaseIsReachable`) |
| Manual health check, Keycloak absent | `docker compose ps` (keycloak not running) + `dotnet run --project src/DaxaPos.Api` + `curl http://localhost:5299/health` | **Pass** — `200 Healthy`, with the `keycloak` container never started at any point in the session |
| Teardown | `docker compose down -v` | Clean — no leftover containers/volumes |

This directly satisfies the user's decision #4: *"The API must start successfully if Keycloak is unavailable. GET /health must return healthy for the API/database path without requiring Keycloak."*

---

## Environment notes (for whoever runs this next)

1. **Runtime mismatch:** this build machine has only the .NET 10 runtime installed, no net9.0 runtime component. `dotnet ef` and `dotnet test` both failed with "You must install or update .NET to run this application" until `Directory.Build.props` set `RollForward=LatestMajor`. This does **not** change any project's declared `net9.0` TargetFramework — it only lets net9.0 executables run on the installed 10.x runtime. Remove it once a net9.0 runtime is actually installed, or if the TFM is deliberately moved to net10.0 later.
2. **Package version pinning:** `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`, `Microsoft.Extensions.DependencyInjection.Abstractions`, and `Microsoft.Extensions.Configuration.Abstractions` are all pinned to **9.0.4** (not 9.0.0) across every project. 9.0.0 triggered `NU1605` package-downgrade build errors because `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4 requires `Microsoft.EntityFrameworkCore` ≥ 9.0.1. Keep the whole EF Core family on one patch version.
3. **`dotnet new sln` default changed:** the .NET 10 SDK defaults to the new `.slnx` XML solution format. `DaxaPos.sln` was created with `dotnet new sln -n DaxaPos --format sln` to get the classic format instead, for wider tooling compatibility (CLAUDE.md and all existing plans assume `.sln`).
4. **Solution/compose file locations:** `DaxaPos.sln` is at the repo root (not under `src/`); Docker Compose lives at `deploy/docker-compose.yml` (not repo root) — this matches the location already documented in `docs/deployment/docker.md`'s Quick Start, which predates this session.

---

## Blocked Before Implementation — updated status

| # | Blocker | Status |
|---|---|---|
| 1 | Inter-module communication pattern undecided | **Resolved.** [ADR-0014](../adr/accepted/ADR-0014-inter-module-communication.md) accepted, with a Handler I/O Rule added before acceptance: domain event handlers must not perform slow/unreliable/external I/O directly — that work must go through a durable outbox/work item processed by `DaxaPos.Workers`. `IDomainEvent`/`IDomainEventDispatcher`/a simple in-process dispatcher/DI registration are implemented; no real events, handlers, or outbox table yet (correctly deferred). |
| 2 | PLAN-0003 modelled staff PIN login as Keycloak/OIDC | **Resolved** (previous pass). |
| 3–7 | Stale doc references, PLAN-0009 sub-plan, etc. | **Resolved** (previous pass). |
| 8 (new) | PLAN-0002 scope needed tightening before code was written | **Resolved.** User approved a neutral-skeleton-only scope (decisions #2–#7 in this session); implemented and verified exactly to that scope. Multi-tenant query filters, Keycloak auth wiring, staff PIN login, RBAC, and the Stripe Terminal adapter were deliberately **not** built — see PLAN-0002's Handoff Notes for the explicit deferral list. |

### Final verdict: is PLAN-0002 complete?

**Partially complete, by design.** Every item on the user's explicit allow-list for this pass is implemented and verified: .NET 9 solution, five projects, PostgreSQL + Keycloak in Docker Compose, `/health`, initial EF Core migration (Tenant/Organisation/Location/Device/Terminal, Device and Terminal kept as separate concepts), `.env.example`, `.gitignore` (already sufficient, unchanged), basic CI skeleton, and Docker/local deployment doc updates. Nothing on the disallow-list was touched (no order/tax/payment logic, no payment abstractions, no module projects, no Stripe Terminal, no staff PIN login, no full identity/RBAC, no sync).

What remains for PLAN-0002 to be *fully* complete, per the original plan document (now superseded in part by this session's tighter scope): multi-tenant query filters were explicitly **descoped to PLAN-0003** rather than deferred by accident — this is a deliberate, documented change to PLAN-0002's original Domain Assumptions, not an oversight.

---

## Recommended execution order (unchanged)

```
PLAN-0001 (Architecture Foundation)                — satisfied by existing docs + ADR-0014
  └─ PLAN-0002 (Platform Skeleton)                  — neutral skeleton done; not committed
       └─ PLAN-0003 (Identity, Tenancy, Locations, Devices, Staff Sessions)  — next
            └─ PLAN-0004 (Catalog, Menu, Tax, Pricing)
                 ├─ PLAN-0005 (Payments, Receipts, Printing)
                 │     ├─ PLAN-0006 (Terminal/MAUI, Display, PWA)
                 │     └─ PLAN-0009 (First Payment Adapter: Stripe Terminal) — accepted as a future
                 │           plan, explicitly not executed yet (user decision #6)
                 └─ PLAN-0007 (Sync, Local, Hybrid)
PLAN-0008 (Testing, Security, Deployment) — cross-cutting
```

---

## Next concrete step: PLAN-0003

PLAN-0003 (already rewritten to align with ADR-0013 in the prior pass) is next. It should:

1. Add multi-tenant EF Core global query filters on top of the schema this plan created (`TenantId` scoping via `Organisation` → `Location` → `Device`/`Terminal`), with a test asserting cross-tenant isolation — this was the first test recommended in the original readiness report and is still pending.
2. Implement the shared `AuthContext` model and authorization middleware.
3. Implement the Daxa WebAPI-native staff ID + PIN flow (not Keycloak/OIDC) and scope Keycloak wiring strictly to cloud/admin/back-office auth, consistent with what this skeleton already verified (API works with Keycloak absent).
4. Only then touch the `keycloak` service meaningfully in code — right now it is inert infrastructure.

---

## Human review required

- Review the neutral skeleton before it is committed (no commit has been made).
- Confirm the package-version pinning approach (§ Environment notes #2) is acceptable, or specify a different EF Core patch version to standardize on.
- Confirm `Directory.Build.props`'s `RollForward=LatestMajor` is acceptable for this environment, or indicate that a net9.0 runtime should be installed instead.
- Confirm PLAN-0003 is ready to execute next with the scope described above.
