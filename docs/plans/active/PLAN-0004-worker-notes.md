# PLAN-0004 Worker Notes — Planning Pass (2026-07-03)

## Session Purpose

Turn the architecture-level PLAN-0004 draft (written during the 2026-06-29 documentation session) into an implementation-ready, milestone-by-milestone plan, following the exact process PLAN-0003 used for its own initial planning pass. No product code, migrations, or `src/` changes were made in this session — planning/documentation only, per explicit instruction to stop after the plan and wait for approval.

## What Was Read

Full pass over: `PLAN-0003-identity-tenancy-locations-devices.md` (final approved state, all milestone sections) and `PLAN-0003-worker-notes.md` (all milestone reports A–H, in full — this is where every reusable convention below was sourced from, not invented fresh); `ADR-0003`, `ADR-0006`, `ADR-0010`, `ADR-0011`, `ADR-0015` (all accepted); `ADR-0016` (proposed); `docs/architecture/tax-engine.md`, `docs/architecture/multi-location.md`; `docs/modules/catalog.md`, `menus.md`, `tax.md`, `pricing.md`, `surcharges.md`; `docs/testing/tax-tests.md`, `docs/testing/security-tests.md`; `docs/issues/closed/OI-0007-tax-configuration-editing-permissions.md`; `docs/issues/open/OI-0015-permission-metadata-for-staff-pin-eligibility.md`; current source — `Permissions.cs`, `RequirePermissionFilter.cs`, `DaxaDbContext.cs`, `AuthContext.cs`, `LocationEndpoints.cs` (used as the literal template for every CRUD endpoint file this plan proposes), `AuditEvent.cs`, `RbacTestSeeder.cs`, `Location.cs`.

## What Was Produced

1. `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md` — rewritten from the original architecture-level draft into 8 named milestones (A–H) with concrete entity field lists, 6 named migrations, ~85 endpoints across the milestones, 4 new permission codes (with a `Permission.Category` classification), explicit `rejectStaffPin` calls per endpoint group, and a full commit sequence.
2. This file.

Not yet touched: `docs/adr/index.md`, `docs/README.md`, `docs/issues/index.md` — no ADR or issue status actually changed yet (OI-0015 closure is *proposed* as Milestone A of the plan, not yet executed).

## Design Decisions Worth Flagging to a Future Reader

- **No new `Modules.*` project**, same reasoning PLAN-0003 used: the skeleton still has zero `Modules.*` projects; catalog/tax/pricing/menu code lives directly in the five existing layered projects. Explicitly required by the task's scope guard, but also the right call independent of that — nothing in this plan needs a second DB-touching domain-event consumer outside `Api`, so ADR-0015 §4 stays unrevisited.
- **`TaxDefinitionTemplate` (global, unfiltered) vs. `TaxDefinition` (tenant-owned, editable)** is the single biggest schema decision in this plan and is not explicitly spelled out anywhere in the existing docs — it's an inference from reconciling two things that would otherwise conflict: (a) OI-0007's closed Decision explicitly allows manager-level catalogue users to edit tax *rates*, not just category assignment ("Tax rates can be changed by manager-level users or higher"), which rules out a single shared/global `TaxDefinition` row (one tenant's edit would leak into every other tenant's AU GST rate); (b) the architecture doc's existing `TaxRate`/`TaxCategory` split implies *some* shared reference data should exist so every new AU tenant doesn't start from a completely blank tax config. The template/instance split mirrors the already-established `Role`/`Permission` (global) vs. `RolePermission`/`UserRole` (tenant-owned) pattern in the same codebase — reusing a shape the human has already approved once, not inventing a new one.
- **`catalog.manage` deliberately covers both product CRUD and tax category/definition CRUD in one permission code**, directly because OI-0007's closed Decision says so in as many words ("keeps tax configuration aligned with catalogue management instead of making it a separate ... function"). This directly overrides OI-0015's own speculative example of `tax.manage` as PLAN-0004's likely new code — OI-0015 was written before OI-0007 closed with this specific ruling, so the speculative example is superseded by the later, authoritative decision. Flagged so a future reader doesn't wonder why the "expected" `tax.manage` code from OI-0015 doesn't appear.
- **`catalog.sold-out-toggle` is the first `Operational`-category, staff-PIN-eligible permission in the codebase.** Every permission PLAN-0003 created was `AdminSensitive` (the `Staff` role holds zero permissions today). This is a deliberate, load-bearing test of the very mechanism Milestone A builds (`Permission.Category`, closing OI-0015) — if the category split doesn't work correctly, this is the first place it would be caught by a real staff-accessible endpoint rather than only by a unit test asserting the enum value exists.
- **The resolved-menu read endpoint requires no permission code at all**, only `.RequireAuthorization()`. This was the hardest call in the whole plan and is flagged as Human Decision #1 rather than assumed. The reasoning: every other PLAN-0004 endpoint is *configuration* (setting up what to sell), gated `rejectStaffPin: true` because configuration is financially sensitive per ADR-0013/OI-0007. The resolved-menu endpoint is different in kind — it's *what a POS operator reads to do their job* — and if this plan gated it like every other endpoint, a staff-PIN session (the normal, expected POS operator credential) would get 403 trying to see the menu, which would make the product unusable for its stated purpose from day one of PLAN-0005. No existing ADR or doc states this rule explicitly; it was derived from ADR-0013's own operational/sensitive split applied to a read rather than a write.
- **The per-order 20-tax-component design limit (ADR-0006) is explicitly not enforced by this plan** — `Order` doesn't exist until PLAN-0005, and the limit is an aggregate across multiple order lines, not something a per-line tax engine can check. Recorded in three places in the plan doc (Architecture Assumptions, Global Tax Design Constraints, Handoff Notes) specifically so it doesn't quietly fall through the gap between PLAN-0004 and PLAN-0005 — the kind of thing that's easy to assume "someone else's plan handles" and have neither plan actually build.
- **Archive-and-replace (OI-0007) is scoped to `Product` only in this plan**, not generalised into a shared mechanism/base class/interface for future entities. Flagged as a "candidate ADR" rather than built speculatively — consistent with CLAUDE.md's instruction not to design for hypothetical future requirements. If a second entity needs the same treatment in a later plan, that's the trigger to extract a shared pattern, not before.
- **Surcharges are explicitly out of scope**, even though `docs/modules/surcharges.md` lists PLAN-0004 as its "Related Plan." The original architecture-level PLAN-0004 draft's own Scope section never mentioned surcharges, and the CLAUDE.md phase roadmap places the surcharge engine in Phase 2 alongside the rest of the advanced pricing rule engine. Treated as a pre-existing doc cross-reference that overstated this plan's scope, not a new descoping decision — flagged so a future reader checking `surcharges.md` doesn't assume it was silently dropped mid-plan.

## Open Items Requiring the User's Explicit Sign-Off

See "Human Decisions Needed" in the plan itself — summarized: (1) confirm the resolved-menu endpoint's no-permission-code design; (2) confirm `catalog.manage`/`pricing.manage` as two codes rather than one; (3) confirm manual (not auto-provisioned-at-org-creation) AU tax defaults; (4) confirm the archive-and-replace concurrency race is an accepted MVP risk, matching OI-0013; (5) confirm `VenueTaxConfiguration` absence should 404 rather than silently default; (6) confirm accepting ADR-0016 now (formalizing its constraints, not implementing it); (7) confirm the menu merge-precedence and `MenuAvailabilityRule` day/time shape.

## Recommended Next Session (superseded — see Milestone A Report below)

1. ~~Human reviews and (dis)approves the plan~~ — done 2026-07-03, all 7 items approved as recommended.
2. ~~On approval, implement Milestone A first~~ — done, see report below.
3. Update this plan's Status section with milestone checkboxes as work proceeds — no more than 3 commits without a refresh, per CLAUDE.md's plan-refresh rule, exactly as PLAN-0003 did throughout Milestones A–H.

---

## Milestone A Report (2026-07-03)

Human approved all 7 "Human Decisions Needed" items as recommended (see the plan's updated Approval Record). Milestone A implemented per the plan using strict TDD: wrote the two new `StaffPinLoginTests.cs` tests plus fixed the one existing test that referenced `Permissions.AdminSensitive` first, confirmed RED via `dotnet build` (15 compile errors — all "does not contain a definition for" the not-yet-created symbols, the expected reason), then implemented production code, then confirmed GREEN.

### Files changed

New:
- `src/DaxaPos.Domain/Enums/PermissionCategory.cs`
- `src/DaxaPos.Persistence/Migrations/20260703120121_AddPermissionCategory.cs` (+ `.Designer.cs`)

Modified:
- `src/DaxaPos.Domain/Entities/Permission.cs` — added `Category` property.
- `src/DaxaPos.Persistence/Configurations/PermissionConfiguration.cs` — `Category` column mapping (required) + seed data for all 12 permissions (8 existing backfilled to `AdminSensitive`, 4 new).
- `src/DaxaPos.Persistence/Configurations/RolePermissionConfiguration.cs` — `catalog.manage`/`pricing.manage`/`menus.manage`/`catalog.sold-out-toggle` granted to `SystemAdmin`/`OrganisationOwner`/`VenueManager`; `catalog.sold-out-toggle` also granted to `Staff` (its first-ever permission grant).
- `src/DaxaPos.Persistence/Seed/RbacSeedIds.cs` — 4 new fixed permission GUIDs (`...0009`–`...0012`).
- `src/DaxaPos.Application/Identity/Permissions.cs` — added `CatalogManage`, `PricingManage`, `MenusManage`, `CatalogSoldOutToggle` constants; **deleted** the `AdminSensitive` hard-coded set and its doc comment.
- `src/DaxaPos.Api/Endpoints/Identity/AuthEndpoints.cs` — `StaffPinLoginAsync`'s permission query now selects `{ p.Code, p.Category }` instead of just `p.Code`; the sensitive-permission guard checks `assignedPermissions.Any(p => p.Category == PermissionCategory.AdminSensitive)` instead of `permissionCodes.Any(Permissions.AdminSensitive.Contains)`.
- `tests/DaxaPos.Api.Tests/StaffPinLoginTests.cs` — 2 new tests added; `StaffSession_MisconfiguredWithSensitivePermissions_IsStillRejectedByRejectStaffPinEndpoints` updated to list the 8 permission codes explicitly (its only other caller of the now-deleted `AdminSensitive` set — this fix was a required corollary of the deletion, not unrelated cleanup).
- `docs/issues/closed/OI-0015-permission-metadata-for-staff-pin-eligibility.md` (moved from `open/`, Decision/Outcome/Status Update sections added).
- `docs/issues/index.md`, `docs/CHANGELOG.md`, `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md` (Status line + Milestone A status marker + Approval Record).

No catalog/product/menu/tax/pricing entities, endpoints, or DTOs were added — Milestone A is metadata-only, exactly as scoped.

### Migration created

`20260703120121_AddPermissionCategory` — adds `permissions.Category` (`integer`, not null, default `0`), `UpdateData` for the 8 pre-existing permission rows (→ `1`/`AdminSensitive`), `InsertData` for the 4 new `Permission` rows and their 13 `RolePermission` seed rows (4 codes × 3 roles + 1 for `Staff`). Verified to apply cleanly in sequence from an empty database (all 7 migrations, using a disposable throwaway Postgres database — not the shared dev database — then dropped).

### Tests added

2 new integration tests in `tests/DaxaPos.Api.Tests/StaffPinLoginTests.cs`:
- `Login_WhenAssignedRoleGrantsOnlyOperationalPermissions_Succeeds` — assigns `Staff` role, logs in via staff PIN, asserts 200 OK and `catalog.sold-out-toggle` present in the response's `Permissions`. This is the load-bearing proof of OI-0015's resolution: an `Operational`-category permission does not trip the staff-PIN rejection, and `Staff` holding a permission at all is new behaviour this milestone introduces deliberately.
- `PermissionCatalogue_ClassifiesPLAN0004MilestoneAPermissions_ByCategory` — queries the DB directly for 5 permission codes (1 pre-existing + 4 new) and asserts each has the expected `Category`, proving the classification is real seed data.

1 existing test corrected (not new behaviour, a compile-fix): the `PermissionSnapshot` array in `StaffSession_MisconfiguredWithSensitivePermissions_IsStillRejectedByRejectStaffPinEndpoints`.

The existing `Login_WhenAssignedRoleGrantsSensitivePermissions_IsRejectedAndAudited` test (VenueManager → staff.manage etc.) was **not modified** and re-ran green unmodified — this is the regression proof that the guard refactor didn't weaken existing behaviour.

### Commands run

```
dotnet build tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj      (RED: 15 compile errors, expected symbols)
dotnet build DaxaPos.sln                                           (GREEN after implementation, 0 warnings/errors)
dotnet ef migrations add AddPermissionCategory --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~StaffPinLoginTests"
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~RbacTests|FullyQualifiedName~RequirePermissionFilterTests|FullyQualifiedName~LocalUserLoginTests"
dotnet test DaxaPos.sln
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "CREATE DATABASE daxapos_migration_check;"
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api --connection "...daxapos_migration_check..."   (clean-database migration re-verification)
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "DROP DATABASE daxapos_migration_check;"
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` — **373/373 passed** (82 unit tests + 291 API tests), against real Postgres, 0 failed, 0 skipped.
All 7 migrations (6 from PLAN-0003 + this one) verified to apply cleanly in sequence from an empty database.

### Deviations from the written plan (flagged, not silently made)

1. **Deleted `Permissions.AdminSensitive` had a second caller** the plan's Milestone A description didn't mention: a test (`StaffSession_MisconfiguredWithSensitivePermissions_IsStillRejectedByRejectStaffPinEndpoints`) that deliberately seeds a misconfigured session's `PermissionSnapshot` from that set, to prove the endpoint-level `rejectStaffPin` net holds independently of the login-time guard. The plan said "confirm no other caller depends on it first" — this was that check; fixed by listing the 8 codes explicitly rather than leaving the test broken or keeping the dead set around for one caller.
2. **No separate `StaffPinSensitivePermissionGuardTests.cs` unit-test file** was created (the plan's Tests line offered this as one option, "or extend the existing staff-pin-login test file" as the other). The guard logic is a one-line `Any()` check inline in `AuthEndpoints.StaffPinLoginAsync`, not extracted into a separately-testable pure class — extracting a one-liner into its own file/class for unit-testability would have been an unrequested abstraction (CLAUDE.md: no abstractions beyond what the task requires). Extended `StaffPinLoginTests.cs` instead, which already exercises the real DB-backed path end-to-end.
3. **`Permissions.cs`'s doc comment was rewritten, not just had one line removed.** The plan said "remove the TEMPORARY MECHANISM note"; doing that in isolation would have left a doc comment describing a set that no longer exists. Rewrote the class-level summary to point at `Permission.Category` instead.
4. **Migration timestamp is `20260703120121`, not literally `AddPermissionCategory` with no timestamp** — this is just how `dotnet ef migrations add` names files (`{timestamp}_{Name}.cs`); noted only because the plan's prose sometimes referred to migrations by name alone.

None of these required backing out or redoing anything — all were caught either while writing the tests first (1) or while doing the doc-update pass (2–4).

### ADR-0016 status check (per approved Human Decision #6)

Checked during this session: `ADR-0016` is still at `docs/adr/proposed/ADR-0016-multi-language-and-localisation-strategy.md`, not `accepted/`. Per the conditional approval ("accept now if not already accepted, otherwise flag rather than move silently"), **it was not moved** — Milestone A does not touch any column ADR-0016 constrains, so nothing here depended on its status. Flagged in the plan doc's Approval Record and here for whoever picks up Milestone B (the first milestone that actually creates translatable-in-future columns) to either move it then, or move it separately as a standalone one-line docs commit whenever convenient.

### Blockers before Milestone B

None. `Permission.Category` exists and is seeded correctly for all 12 current permission codes; `dotnet build`/`dotnet test` are clean; migrations verified clean from empty. Milestone B (tax foundation: `TaxDefinitionTemplate`, `TaxDefinition`, `TaxCategory`, `TaxCategoryDefinition`, and the pure `TaxCalculationEngine`) can start on request.

One heads-up for whoever starts Milestone B: it is the first milestone with genuinely financial logic (the tax calculation engine) — CLAUDE.md's Testing Rules make TDD mandatory specifically for that engine, not just good practice. Recommend starting Milestone B with the `TaxCalculationEngineTests` AU/NZ mixed-basket assertions before writing a single line of `TaxCalculationEngine` itself, exactly as the plan's own Milestone B "Tests" line already specifies.
