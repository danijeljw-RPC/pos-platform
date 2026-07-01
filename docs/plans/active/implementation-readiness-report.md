# Implementation Readiness Report — Daxa POS

## Status

Updated 2026-07-01 after a documentation-only correction pass. No code, `.sln`, or `src/` was created in either pass that produced this report.

## Purpose

All 13 ADRs from the first pass were accepted (ADR-0009 superseded by ADR-0013) and all 10 open issues were closed. This report originally reviewed `docs/plans/active/PLAN-0001` through `PLAN-0008` and found several gaps between the accepted decisions and what the plans actually said. This update records the documentation correction pass that resolved those gaps and gives a final blocked/unblocked verdict for `PLAN-0002`.

---

## Blocked Before Implementation

This is the authoritative list of what was blocking code work, and its current status.

| # | Blocker | Status | Detail |
|---|---|---|---|
| 1 | Inter-module communication pattern undecided (PLAN-0001 step 8) | **Drafted, awaiting human acceptance** | [ADR-0014 — Inter-Module Communication Pattern](../adr/proposed/ADR-0014-inter-module-communication.md) created under `docs/adr/proposed/`. Recommends direct in-process calls for synchronous work, in-process domain events for fan-out/decoupling, no external broker for MVP. Per CLAUDE.md, ADRs move to `accepted/` only after human approval — **this is the one remaining gate before PLAN-0002's event-dispatcher scaffolding step should be built.** |
| 2 | PLAN-0003 modelled POS staff PIN login as a Keycloak/OIDC flow, contradicting ADR-0013 | **Resolved** | `PLAN-0003` rewritten in full: Keycloak is now scoped to cloud/admin/back-office/support/external identity only; POS staff PIN login is a Daxa WebAPI-native, offline-capable flow; hybrid/cloud mode behaviour is spelled out per ADR-0013. |
| 3 | PLAN-0002 referenced the superseded ADR-0009 and gave no ADR-0013 context for its Keycloak Docker Compose step | **Resolved** | Context Read and "ADRs Required" now point to ADR-0013 (and ADR-0014); step 5 now states Keycloak there is scoped to cloud/admin auth and the skeleton/health check must not depend on it. |
| 4 | `docs/architecture/overview.md` said "all ADRs are proposed" and listed identity under the superseded ADR-0009 | **Resolved** | Corrected to "accepted," ADR-0013 and ADR-0014 added to the decision table. |
| 5 | `docs/planning/05-suggested-dotnet-solution-structure.md` used stale `PosPlatform.*` naming | **Resolved** | Renamed to `DaxaPos.*` throughout; file now points to `docs/architecture/overview.md` as the canonical solution structure. |
| 6 | Broken link: `ADR-0008`'s Related Documents pointed to `ADR-0009-keycloak-or-identity-provider-strategy.md` in the wrong folder | **Resolved** | Fixed to point to `ADR-0013` (noted as superseding ADR-0009). |
| 7 | No sub-plan existed for the first payment adapter, despite ADR-0005/OI-0001 already naming Stripe Terminal | **Resolved** | [PLAN-0009 — First Payment Adapter: Stripe Terminal](PLAN-0009-first-payment-adapter-stripe-terminal.md) created, separating cash/manual EFTPOS (no adapter) from the Stripe Terminal adapter, with later Tyro/Zeller/Square/Windcave adapters explicitly deferred to their own follow-up plans. PLAN-0005 cross-links to it. |

### Final verdict: is PLAN-0002 unblocked?

**Yes, with one narrow caveat.**

PLAN-0002's actual file list (`DaxaPos.Api`, `Domain`, `Application`, `Infrastructure`, `Persistence`, Docker Compose, health check, initial `Tenant/Organisation/Location/Terminal` migration) creates no `Modules.*` projects and has no hard dependency on the inter-module communication decision — that decision only matters once module-to-module boundaries exist (starting around PLAN-0004/PLAN-0005). **The solution scaffolding, Docker Compose stack, health check endpoint, and initial migration in PLAN-0002 can start now.**

The one piece that should wait is the `IDomainEventDispatcher` abstraction this report recommends adding to PLAN-0002's skeleton (§3 below) — that should not be built until [ADR-0014](../adr/proposed/ADR-0014-inter-module-communication.md) is accepted, so it isn't built against a pattern that gets overturned in review. If ADR-0014 acceptance is delayed, PLAN-0002 can proceed without that one abstraction and it can be added in a small follow-up commit once the ADR is accepted.

PLAN-0003 is now unblocked for whenever its turn comes after PLAN-0002.

---

## 1. Repository state

The repository is **100% greenfield**: no `.sln`, no `.csproj`, no `src/`, no `docker-compose*.yml` exist anywhere in the tree. Everything produced so far, in both passes, is documentation (`docs/`).

---

## 2. Recommended execution order

Unchanged from the first pass:

```
PLAN-0001 (Architecture Foundation)
  └─ PLAN-0002 (Platform Skeleton)
       └─ PLAN-0003 (Identity, Tenancy, Locations, Devices)
            └─ PLAN-0004 (Catalog, Menu, Tax, Pricing)
                 ├─ PLAN-0005 (Payments, Receipts, Printing)
                 │     ├─ PLAN-0006 (Terminal/MAUI, Display, PWA)
                 │     └─ PLAN-0009 (First Payment Adapter: Stripe Terminal) — alongside/after PLAN-0005's non-adapter work
                 └─ PLAN-0007 (Sync, Local, Hybrid)  — can start once 0002 + 0004 exist, parallel to 0005/0006
PLAN-0008 (Testing, Security, Deployment) — cross-cutting; test scaffolding starts as early as PLAN-0002, hardening/CI work runs parallel to 0006/0007
```

PLAN-0001's outstanding deliverable (inter-module communication pattern) is now drafted as ADR-0014; once accepted, PLAN-0001 is fully satisfied by existing documentation and PLAN-0002 can proceed without caveats.

---

## 3. First concrete implementation task

Unchanged in substance from the first pass, updated for ADR-0014's current state:

1. **Get ADR-0014 accepted or revised.** It's drafted and proposed at `docs/adr/proposed/ADR-0014-inter-module-communication.md` — this is now a human decision, not a documentation gap.
2. Begin PLAN-0002 as written: `dotnet new sln`, scaffold `DaxaPos.Api/Domain/Application/Infrastructure/Persistence`, add `Tenant/Organisation/Location/Terminal` entities + first EF Core migration, `docker-compose.yml` with `api`, `db` (PostgreSQL), and `/health` endpoint. This does not need to wait on step 1.
3. Once ADR-0014 is accepted, add the `IDomainEventDispatcher` abstraction (interface in `DaxaPos.Application`, in-process implementation in `DaxaPos.Infrastructure`) as a small follow-up commit to PLAN-0002's skeleton.
4. First test: an EF Core integration test asserting the multi-tenant global query filter actually excludes another tenant's `Location` row.

---

## 4. Phase 1 .NET solution structure

Unchanged from the first pass — `docs/architecture/overview.md` § Solution Structure remains the authoritative reference, and is now internally consistent (`docs/planning/05` was corrected to match its `DaxaPos.*` naming rather than the other way around).

Phase 1 (MVP) subset, trimmed from the full module list, now including the Stripe Terminal payment provider scoped by PLAN-0009:

```
src/
  DaxaPos.Api/
  DaxaPos.Workers/
  DaxaPos.Domain/
  DaxaPos.Application/
  DaxaPos.Infrastructure/
  DaxaPos.Persistence/

  DaxaPos.Modules.Catalog/
  DaxaPos.Modules.Orders/
  DaxaPos.Modules.Payments/
  DaxaPos.Modules.Tax/
  DaxaPos.Modules.Pricing/
  DaxaPos.Modules.Receipts/
  DaxaPos.Modules.Devices/
  DaxaPos.Modules.Audit/

  DaxaPos.PaymentProviders.StripeTerminal/   ← first and only provider adapter for MVP (ADR-0005 addendum, OI-0001, PLAN-0009)

tests/
  DaxaPos.UnitTests/
  DaxaPos.IntegrationTests/
  DaxaPos.Api.Tests/
  DaxaPos.Tax.Tests/
```

`DaxaPos.PosMaui`, `DaxaPos.AdminPwa`, `DaxaPos.KdsPwa`, `DaxaPos.Modules.Sync`, `DaxaPos.Modules.Inventory`, `DaxaPos.Modules.Reporting`, and the remaining `PaymentProviders.*` projects belong to PLAN-0006/0007 and Phase 2+ and shouldn't be scaffolded empty up front.

---

## 5. Documentation correction pass — summary of changes made

- Created `docs/adr/proposed/ADR-0014-inter-module-communication.md` and added it to `docs/adr/index.md` under Proposed.
- Rewrote `docs/plans/active/PLAN-0003-identity-tenancy-locations-devices.md` in full to align with ADR-0013.
- Corrected `docs/plans/active/PLAN-0002-platform-skeleton.md`'s stale ADR-0009 references (Context Read and ADRs Required) and clarified the Keycloak Docker Compose step's ADR-0013 scope.
- Fixed `docs/architecture/overview.md`'s stale "all ADRs proposed" wording and its ADR decision table (ADR-0013 replacing ADR-0009, ADR-0014 added).
- Renamed `PosPlatform.*` to `DaxaPos.*` throughout `docs/planning/05-suggested-dotnet-solution-structure.md` and pointed it at `docs/architecture/overview.md` as canonical.
- Fixed a broken relative link in `docs/adr/accepted/ADR-0008-device-identity-vs-user-identity.md` (pointed at ADR-0009 in the wrong folder; now points at ADR-0013).
- Created `docs/plans/active/PLAN-0009-first-payment-adapter-stripe-terminal.md` and cross-linked it from `PLAN-0005`'s Non-goals, Open Issues Required, ADRs Required, and Handoff Notes.
- Updated `docs/README.md`'s ADR summary paragraph and Active Plans list (added PLAN-0009 and this report).

No file was renamed. No application code was written.

---

## 6. Recommended first implementation commit sequence

```
docs: this documentation correction pass (already applied — see §5)
```

Once ADR-0014 is reviewed and either accepted or revised:

```
chore: scaffold Daxa POS .NET solution (DaxaPos.Api/Domain/Application/Infrastructure/Persistence)
infra: add Docker Compose for local dev stack (api, db)
feat(api): add health check endpoint
feat(persistence): add Tenant/Organisation/Location/Terminal entities and initial EF Core migration
test(persistence): add multi-tenant query filter isolation test
feat(application): add IDomainEventDispatcher abstraction and in-process implementation (once ADR-0014 accepted)
docs: update deployment/docker.md for Docker Compose setup
```

---

## Human review required

- **Accept, revise, or reject ADR-0014** (`docs/adr/proposed/ADR-0014-inter-module-communication.md`) — the one remaining decision gating full PLAN-0002 scaffolding (see §Blocked Before Implementation, item 1).
- Confirm the rewritten `PLAN-0003` matches intended identity/session behaviour before it's picked up.
- Confirm the Phase 1 solution structure trim in §4 (deferring Sync/Inventory/Reporting/MAUI/PWA projects, and all payment providers except Stripe Terminal, until their owning plans start) matches intended MVP scope.
- Confirm `PLAN-0009`'s scope (cash + manual EFTPOS + Stripe Terminal only, all other providers deferred) matches intended MVP payment scope.
