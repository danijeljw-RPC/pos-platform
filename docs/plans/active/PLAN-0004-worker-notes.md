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

---

## Milestone B Report (2026-07-04)

Implemented per the plan using strict TDD: wrote all 10 `TaxCalculationEngineTests` first, confirmed RED via `dotnet build` (4 compile errors — `DaxaPos.Application.Tax` namespace and `TaxComponentSnapshot` not found, the expected reason since neither existed yet), then implemented the pure engine, confirmed GREEN, then added the persisted entities/EF configs/migration (schema-only, no additional TDD cycle needed — matches PLAN-0003 Milestone A's precedent of not TDD'ing bare enums/entities with no branching logic).

### Files changed

New:
- `src/DaxaPos.Domain/Enums/TaxJurisdictionType.cs`, `TaxRoundingMode.cs`, `TaxCalculationScope.cs`, `TaxTreatment.cs`
- `src/DaxaPos.Domain/Entities/TaxDefinitionTemplate.cs`, `TaxDefinition.cs`, `TaxCategory.cs`, `TaxCategoryDefinition.cs`
- `src/DaxaPos.Application/Tax/TaxCalculationModels.cs` (`TaxComponentSnapshot`, `TaxableLineRequest`, `TaxLineResult`), `TaxLineCalculationResult.cs` (`TaxCalculationErrorCode`, `TaxLineCalculationResult`), `TaxCalculationEngine.cs`
- `src/DaxaPos.Persistence/Seed/TaxSeedIds.cs`
- `src/DaxaPos.Persistence/Configurations/TaxDefinitionTemplateConfiguration.cs`, `TaxDefinitionConfiguration.cs`, `TaxCategoryConfiguration.cs`, `TaxCategoryDefinitionConfiguration.cs`
- `src/DaxaPos.Persistence/Migrations/20260704120431_AddTaxFoundation.cs` (+ `.Designer.cs`)
- `tests/DaxaPos.UnitTests/Tax/TaxCalculationEngineTests.cs`

Modified:
- `src/DaxaPos.Persistence/DaxaDbContext.cs` — 4 new `DbSet`s; fail-closed query filters added for `TaxDefinition`/`TaxCategory`/`TaxCategoryDefinition` (not `TaxDefinitionTemplate`, which is global/unfiltered like `Role`/`Permission`).
- `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md` (Status line, Milestone B status marker, ADR-0016 re-check note).

No tax configuration endpoints, product/menu/pricing entities, or order-line tax snapshots were added — Milestone B is entities + pure engine only, exactly as scoped.

### Migration created

`20260704120431_AddTaxFoundation` — creates `tax_definition_templates` (global, unfiltered, unique `Code` index), `tax_definitions`, `tax_categories`, `tax_category_definitions` (all three tenant-owned, `TenantId` index + fail-closed filter, `(TenantId, Code)` unique index on the two named-catalogue tables), plus the 5 AU/NZ `HasData` rows on `tax_definition_templates`. Verified to apply cleanly in sequence from an empty database (all 8 migrations, disposable throwaway Postgres database, then dropped — not the shared dev database, which was migrated separately for the working tree).

### Tests added

10 new unit tests in `tests/DaxaPos.UnitTests/Tax/TaxCalculationEngineTests.cs`, covering every item in the task's required list:
1. `CalculateLine_AuGst10Inclusive_FiveFiftyDollarItem_ExtractsFiftyCentsGst` — $5.50 → $0.50 GST.
2. `CalculateLine_AuMixedBasket_TotalGstAcrossThreeLinesEqualsOneThirty` — the CLAUDE.md/ADR-0006 worked example, byte-for-byte: 3 separate `CalculateLine` calls (flat white/cake/bread) summed by the test, not a basket API in the engine (see Design Decisions below).
3. `CalculateLine_GstFreeLine_ProducesAZeroTaxLineResult_NotAnAbsentLine` — proves the 0%-rate case still returns a populated result, per ADR-0006's Global Tax Design Constraints.
4. `CalculateLine_NzGst15Inclusive_ElevenFiftyDollarItem_ExtractsOneFiftyGst` — $11.50 → $1.50 GST (chosen so the division is exact, no rounding ambiguity).
5. `CalculateLine_ExclusiveComponent_AddsTaxOnTopOfTheLineAmount_InsteadOfExtractingIt` — tax-exclusive path.
6. `CalculateLine_RoundsHalfAwayFromZero_AtTheConfiguredPrecision` — $1.65 at 10% exclusive = $0.165 exactly, a genuine 2-decimal midpoint; asserts $0.17 (away-from-zero), which would be $0.16 under .NET's default banker's rounding — proves the rounding choice is deliberate, not a CLR-default accident.
7. `CalculateLine_WithNoComponents_FailsClosed_InsteadOfSilentlyReturningZeroTax` — the task's explicit "fail closed" requirement.
8. `CalculateLine_IsDeterministic_AndTakesNoDependencies` — same input called twice produces equal-by-value output; the class has zero constructor dependencies so there is nothing to inject a database/clock/HTTP context into.

Plus 2 extra covering the plan's own ADR-0006 design limit: `CalculateLine_WithExactlyTenComponents_Succeeds` (boundary) and `CalculateLine_WithMoreThanTenComponents_FailsClosed_InsteadOfThrowing`.

### Commands run

```
dotnet build tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj      (RED: 4 compile errors, expected symbols)
dotnet test tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj --filter "FullyQualifiedName~TaxCalculationEngineTests"   (GREEN: 10/10)
dotnet build DaxaPos.sln                                           (0 warnings/errors, after entities+configs+DbContext)
dotnet ef migrations add AddTaxFoundation --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet test DaxaPos.sln
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "CREATE DATABASE daxapos_migration_check;"
dotnet ef database update ... --connection "...daxapos_migration_check..."   (clean-database migration re-verification, all 8)
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "DROP DATABASE daxapos_migration_check;"
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` — **383/383 passed** (92 unit tests + 291 API tests, up from 373 at Milestone A close — 10 new tax tests, zero regressions), against real Postgres, 0 failed, 0 skipped.
All 8 migrations (7 from PLAN-0003 + Milestone A + this one) verified to apply cleanly in sequence from an empty database.

### Deviations from the written plan (flagged, not silently made)

1. **`TaxComponentSnapshot` does not carry `CalculationScope`.** The plan's Milestone B entity field lists include `CalculationScope` on `TaxDefinitionTemplate`/`TaxDefinition`, which this implementation honours — but the *engine's* per-line calculation is identical regardless of scope (per-line vs. per-component only matters when aggregating one tax component's contribution across multiple order lines, which requires `Order` — PLAN-0005). Including an unread field in the snapshot would be a decorative parameter with no behaviour; the entities still carry `CalculationScope` as configuration metadata for PLAN-0005 to read directly from `TaxDefinition` when it builds order-level aggregation.
2. **The engine fails closed on an empty `Components` list** (`TaxCalculationErrorCode.MissingTaxConfiguration`), which the plan's Milestone B section does not explicitly specify (it only names the `>10` rejection). This directly implements the task prompt's explicit requirement ("Missing tax configuration must fail closed") and the approved Human Decision #5's spirit (`VenueTaxConfiguration` absence should 404/fail, never silently default) applied one layer down, at the pure-engine boundary rather than only at the future DB-resolution layer. Not a plan violation — an interpretation filling a gap the plan itself left open, flagged rather than silently assumed.
3. **`TaxRoundingMode.NearestCent` is implemented as round-half-away-from-zero** (`MidpointRounding.AwayFromZero`), not .NET's default round-half-to-even. Neither ADR-0006 nor the plan specifies which midpoint rule "NearestCent" implies. Away-from-zero was chosen as the common retail/ATO-style cash-rounding convention and is proven deliberately by a dedicated test (`CalculateLine_RoundsHalfAwayFromZero...`) rather than left to whatever the CLR happens to default to. If a different midpoint rule is wanted, this is the one line (`TaxCalculationEngine.Round`) and one test to change — flagged here so a future reader doesn't assume it was unexamined.
4. **`RatePercent` uses `HasPrecision(9, 4)`** on all three rate-carrying tables — not specified in the plan's field lists (which don't give column types). Chosen to allow fractional rates (e.g. a hypothetical 8.375% VAT) without redesigning the column later, while remaining `decimal` throughout (never `double`/`float`) per standard financial-data practice.
5. **Migration timestamp is `20260704120431`, not literally `AddTaxFoundation`** — same naming-convention note as Milestone A's deviation #4 (this is just how `dotnet ef migrations add` names files).

None of these required backing out or redoing anything — all were caught while writing the tests first (2, 3) or while writing the entity configs (1, 4).

### ADR-0016 status re-check (per this session's explicit instruction not to move it silently)

Re-checked: `ADR-0016` is still `docs/adr/proposed/`, not `accepted/`. Milestone B adds the first translatable-in-future columns this plan's own pre-recorded ADR-0016 constraint applies to (`Name`/`ReceiptMarkerLabel` on the template/definition/category tables) — mapped as plain bounded `varchar` invariant/fallback columns, no `{Entity}Translation` tables, no culture-resolution logic, exactly as the plan already committed to doing regardless of ADR-0016's acceptance status. Nothing in Milestone B depends on ADR-0016 being Accepted, so nothing was skipped. Still not moved — remains a one-line action for the human whenever convenient.

### Blockers before Milestone C

None. `TaxDefinitionTemplate`/`TaxDefinition`/`TaxCategory`/`TaxCategoryDefinition` exist, are seeded/query-filtered correctly, and the pure `TaxCalculationEngine` is fully unit-tested. `dotnet build`/`dotnet test` are clean; migrations verified clean from empty. Milestone C (tax configuration endpoints: `TaxDefinitionTemplateEndpoints`, `TaxDefinitionEndpoints`, `TaxCategoryEndpoints`, `TaxCategoryDefinitionEndpoints`, all `catalog.manage` + `rejectStaffPin: true`) can start on request.

One heads-up for whoever starts Milestone C: it's the first milestone to actually exercise `catalog.manage` (seeded in Milestone A but never gated on an endpoint yet) and the first to write a lifecycle domain event + `jsonb` before/after snapshot for a Milestone-B-defined entity — reuse the exact `LocationEndpoints.cs`/Milestone-D CRUD-sextet shape and the Milestone D `jsonb` serialization pitfall note (bare strings/bools must be `JsonSerializer.Serialize`d) already recorded in `PLAN-0003-worker-notes.md`, not re-derived.

---

## Milestone C Report (2026-07-05)

Implemented per the plan's endpoint list exactly (17 endpoints total, matching the plan doc's count: 7 for `TaxDefinition` incl. templates' 1 read, 6 for `TaxCategory`, 3 for `TaxCategoryDefinition`). CRUD-endpoint acceptance-test convention used (not TDD-first) — matching the plan's own "Tests To Run Later" note that TDD is mandated specifically for the financial-logic units (`TaxCalculationEngineTests`/`PriceResolverTests`), not the CRUD endpoint files, and matching how `LocationEndpoints`/`TerminalEndpoints` were built in PLAN-0003 Milestone D.

### Files changed

New:
- `src/DaxaPos.Domain/Events/TaxDefinitionLifecycleDomainEvent.cs`, `TaxCategoryLifecycleDomainEvent.cs`, `TaxCategoryDefinitionChangedDomainEvent.cs`
- `src/DaxaPos.Api/Endpoints/Tax/TaxDefinitionTemplateEndpoints.cs`, `TaxDefinitionEndpoints.cs`, `TaxCategoryEndpoints.cs`, `TaxCategoryDefinitionEndpoints.cs`
- `tests/DaxaPos.Api.Tests/TaxDefinitionEndpointsTests.cs`, `TaxCategoryEndpointsTests.cs`, `TaxCategoryDefinitionEndpointsTests.cs`

Modified:
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` — 3 new handler classes (`TaxDefinitionLifecycleAuditHandler`, `TaxCategoryLifecycleAuditHandler`, `TaxCategoryDefinitionChangedAuditHandler`), same `$"{EntityType}{Action}"` `EventType` convention as Milestone D.
- `src/DaxaPos.Api/Program.cs` — 3 new `AddScoped<IDomainEventHandler<...>>` registrations, 4 new `app.Map...Endpoints()` calls.
- `tests/DaxaPos.Api.Tests/StaffPinLoginTests.cs` — extended `AssertAllSensitiveEndpointsForbiddenAsync` (now takes `organisationId` too, needed to build valid request bodies) with 7 tax-endpoint attempts, matching the file's established "one shared inventory, not per-entity duplication" convention (see its own class remarks, and `TaxDefinitionEndpointsTests`' class remarks pointing back at it).
- `docs/modules/tax.md`, `docs/architecture/tax-engine.md` (implementation-status sections), `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md`, `docs/CHANGELOG.md`.

No product/menu/pricing entities, order integration, or UI were added — Milestone C is tax configuration endpoints only, exactly as scoped. No migration — Milestone B's 4 entities already carried every field this milestone's endpoints needed.

### Endpoints implemented

- `GET /api/v1/tax-definition-templates` (list, read-only).
- `POST /api/v1/tax-definitions`, `POST /api/v1/tax-definitions/from-template`, `GET` (list/by-id), `PATCH`, `POST .../deactivate`, `POST .../reactivate` (7).
- `POST/GET /api/v1/tax-categories`, `GET/PATCH/{id}`, `.../deactivate`, `.../reactivate` (6).
- `POST/GET /api/v1/tax-category-definitions`, `DELETE /{id}` (3, hard delete).

All gated `catalog.manage` + `rejectStaffPin: true`, no exceptions (this milestone has no sold-out-toggle-style staff-accessible endpoint — that pattern doesn't appear until Milestone F).

### Tests added

35 new integration tests across 3 files, covering the full matrix from prior milestones (create/read/update/deactivate/reactivate happy path, 400 on client-supplied `TenantId`, 403 without `catalog.manage`, 404 cross-tenant, 404 cross-organisation, audit-row assertions) plus tax-specific additions:
- `Templates_ListsSeededAuNzRows` — proves the 5 Milestone B seed rows are readable.
- `CreateFromTemplate_ClonesTemplateFields_AndSetsSourceTemplateCode` / `CreateFromTemplate_Fails_ForUnknownTemplateCode`.
- `Create_Rejects_DuplicateCodeWithinSameTenant` (both `TaxDefinition` and `TaxCategory`) — proves the `(TenantId, Code)` unique-index precondition check.
- `Create_Blocked_WhenTaxCategoryBelongsToDifferentOrganisation` / `...WhenTaxDefinitionBelongsToDifferentOrganisation` / `...WhenLocationBelongsToDifferentOrganisation` — `TaxCategoryDefinition` has no `OrganisationId` column of its own, so each of its 3 foreign references needed its own independent cross-organisation proof (mirroring `TerminalEndpoints`' walk-through-`Location` precedent, but with 3 parents instead of 1).
- `Create_FailsWithBadRequest_WhenExceedingTenComponentLimitForSameCategoryAndLocation` — creates exactly `TaxCalculationEngine.MaxComponentsPerLine` (10) mappings for one `(TaxCategory, null-Location)` pair, then proves the 11th is rejected with 400 — the load-bearing proof that ADR-0006's per-line limit is enforced at the config layer, not just documented.
- `StaffPinLoginTests`'s extended inventory — proves staff-PIN rejection for all 4 endpoint groups without per-entity duplication (existing convention).

### Commands run

```
dotnet build DaxaPos.sln
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~TaxDefinitionEndpointsTests|FullyQualifiedName~TaxCategoryEndpointsTests|FullyQualifiedName~TaxCategoryDefinitionEndpointsTests"
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~StaffPinLoginTests"
dotnet test DaxaPos.sln
```

No `dotnet ef migrations add` — no schema change this milestone, so no clean-database migration re-verification was needed either (the existing 8 migrations were not touched).

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` — **418/418 passed** (92 unit tests + 326 API tests, up from 383 at Milestone B close — 35 new tests, zero regressions), against real Postgres, 0 failed, 0 skipped.

### Deviations from the written plan (flagged, not silently made)

1. **`TaxCategoryDefinition` creation enforces ADR-0006's 10-components-per-line limit**, which the plan's Milestone C prose doesn't explicitly call out (it appears only in the plan's "Global Tax Design Constraints" section, written for the engine generally). Enforcing it here — at the one place a `(TaxCategory, Location)` pair's component count is actually assembled — is the natural implementation site; deferring it further (e.g. to a not-yet-built PLAN-0005 resolution step) would let a tenant configure an unenforceable-later state. Not a plan violation, an interpretation filling a gap the plan's endpoint bullet list left open.
2. **No separate `GET /{id}` endpoint for `TaxCategoryDefinition`**, matching the plan's own endpoint-count arithmetic (17 total only balances if this group is exactly 3: create, list, delete) even though its prose bullet reads "POST/GET ... DELETE /{id}" ambiguously. `List` accepts optional `taxCategoryId`/`locationId` query filters (matching `StaffMemberEndpoints.ListAsync`'s existing `locationId?` convention) as the practical substitute for looking up one mapping directly.
3. **`Code`/`CountryCode`/`RegionCode` (on `TaxDefinition`) and `Code` (on `TaxCategory`) are not editable via `PATCH`** — the plan's field lists don't specify which fields "update-limited-fields" means, so this follows the `Location`/`Organisation`/`Terminal` precedent of treating the natural-key-like field as fixed after creation, editing only the fields OI-0007 actually calls out (rate, name, rounding, markers, treatment).
4. **The `CreatedFromTemplate` audit `Action` string is distinct from `Created`** — not specified by the plan, added so OI-0007's audit trail ("old config, new config") can distinguish a from-template clone from a from-scratch definition without inspecting `SourceTemplateCode` separately.

None of these required backing out or redoing anything.

### ADR-0016 status re-check (per this session's explicit instruction not to move it silently)

Re-checked: still `docs/adr/proposed/`, not `accepted/`. Milestone C added no schema changes at all — no new translatable-in-future columns, no columns of any kind — so nothing here could depend on ADR-0016's acceptance status. Still not moved.

### Blockers before Milestone D

None. Tax configuration is fully CRUD-manageable via the API; `dotnet build`/`dotnet test` are clean (418/418). Milestone D (product catalogue foundation: `ProductCategory`, `Product`, archive-and-replace on tax-category-changing updates per OI-0007) can start on request.

One heads-up for whoever starts Milestone D: it's the first milestone to implement OI-0007's archive-and-replace behaviour for real (a `PATCH` that changes `Product.TaxCategoryId` archives the current row and creates a new one) — re-read OI-0007's closed-issue file and this plan's Domain Assumptions section (`Product` bullet) before writing the endpoint, and note the Risks section's already-accepted, already-documented concurrency race (parallel to OI-0013) rather than adding row-locking. `Product.TaxCategoryId` will need to validate against a real `TaxCategory` this milestone builds against — Milestone C's `TaxCategoryEndpoints` is where a valid `TaxCategoryId` for a test now comes from.

---

## Milestone D Report (2026-07-05)

Implemented per the plan's endpoint list exactly. CRUD-endpoint acceptance-test convention used (not TDD-first), same as Milestone C and PLAN-0003 Milestone D.

### Files changed

New:
- `src/DaxaPos.Domain/Entities/ProductCategory.cs`, `Product.cs`
- `src/DaxaPos.Domain/Events/ProductCategoryLifecycleDomainEvent.cs`, `ProductLifecycleDomainEvent.cs`
- `src/DaxaPos.Api/Endpoints/Catalog/ProductCategoryEndpoints.cs`, `ProductEndpoints.cs`
- `src/DaxaPos.Persistence/Configurations/ProductCategoryConfiguration.cs`, `ProductConfiguration.cs`
- `src/DaxaPos.Persistence/Migrations/20260705034151_AddProductCatalogueFoundation.cs`
- `tests/DaxaPos.Api.Tests/ProductCategoryEndpointsTests.cs`, `ProductEndpointsTests.cs`

Modified:
- `src/DaxaPos.Persistence/DaxaDbContext.cs` — 2 new `DbSet`s, 2 new fail-closed query filters (`ProductCategory`, `Product` — both tenant-owned, no bootstrap `IgnoreQueryFilters()` caller needed).
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` — 2 new handler classes (`ProductCategoryLifecycleAuditHandler`, `ProductLifecycleAuditHandler`), same `$"{EntityType}{Action}"` convention as Milestones C/D-prior.
- `src/DaxaPos.Api/Program.cs` — 2 new `AddScoped<IDomainEventHandler<...>>` registrations, 2 new `app.Map...Endpoints()` calls.
- `tests/DaxaPos.Api.Tests/StaffPinLoginTests.cs` — extended `AssertAllSensitiveEndpointsForbiddenAsync` with 4 catalogue-endpoint attempts, same shared-inventory convention as Milestone C.
- `docs/modules/catalog.md`, `docs/modules/tax.md` (implementation-status sections), `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md`, `docs/CHANGELOG.md`.

No variants, modifiers, pricing resolver, menus, order integration, or UI were added — Milestone D is product catalogue foundation only, exactly as scoped.

### Migration created

`20260705034151_AddProductCatalogueFoundation` — creates `product_categories` (`TenantId`/`OrganisationId` indexed + FK-restricted) and `products` (`TenantId`/`OrganisationId`/`ProductCategoryId`/`TaxCategoryId`/`SupersededByProductId` all indexed + FK-restricted, `SupersededByProductId` self-referencing). No `HasData` seed rows — unlike Milestone B/C, there is no system-wide reference catalogue to seed here. Verified to apply cleanly in sequence from an empty database (all 9 migrations, disposable throwaway Postgres database, then dropped — not the shared dev database, which was migrated separately for the working tree).

### Endpoints implemented

- `POST/GET /api/v1/product-categories`, `GET/PATCH/{id}`, `.../deactivate`, `.../reactivate` (6, standard CRUD sextet).
- `POST/GET /api/v1/products`, `GET/PATCH/{id}`, `.../deactivate`, `.../reactivate` (6 routes; `PATCH` branches internally into in-place-update vs. archive-and-replace, the plan's "7th, same route" accounting).

All gated `catalog.manage` + `rejectStaffPin: true`, no exceptions.

### Archive-and-replace behaviour

`UpdateAsync` validates both the new `ProductCategoryId` and `TaxCategoryId` belong to the caller's organisation, rejects any write against an already-archived product (409 Conflict — a detail the plan's prose didn't spell out, added because OI-0007 describes the archived row as a permanent historical record), then branches:

- `request.TaxCategoryId == product.TaxCategoryId` → `UpdateInPlaceAsync`: every other editable field (`Name`, `Description`, `Sku`, `Barcode`, `ProductCategoryId`, `BasePrice`) updates on the same row, single `"Updated"` audit event, 200 OK.
- Otherwise → `ArchiveAndReplaceAsync`: the current row is archived (`IsArchived = true`, `ArchivedAtUtc` set, `SupersededByProductId` set to the new row's `Id`); a brand-new row is created carrying every requested field value (not just the new `TaxCategoryId`) and the old row's `IsActive` state; two audit events are raised (`"Archived"` on the old row, `"CreatedFromReplace"` on the new one — see `ProductLifecycleDomainEvent`'s doc comment for why two events rather than one combined event); the endpoint returns 201 Created pointing at the new row, with `ProductResponse.PreviousProductId` set so the caller can correlate the id they PATCHed against with the id they got back.

Deactivate/reactivate also reject an already-archived product with 409 Conflict, for the same permanent-historical-record reason.

The documented two-simultaneous-tax-category-edits concurrency race (Risks section, Human Decision #4, approved) is implemented exactly as accepted — no row-locking, no optimistic concurrency token added.

### Tests added

27 new integration tests across 2 files:
- `ProductCategoryEndpointsTests.cs` — standard CRUD matrix (as Milestone C/D-prior), plus `Create_AllowsDuplicateName_NoUniquenessConstraint` documenting the deliberate no-dedup decision.
- `ProductEndpointsTests.cs` — standard CRUD matrix, cross-organisation checks for both `ProductCategoryId` and `TaxCategoryId` references independently, `Create_AllowsDuplicateSkuAndBarcode_NoUniquenessConstraint`, and the archive-and-replace battery: `Update_NonTaxAffectingChange_UpdatesInPlace_AndDoesNotArchive`, `Update_TaxCategoryChange_ArchivesOldRow_AndCreatesReplacementWithLink` (asserts the old row's `IsArchived`/`ArchivedAtUtc`/`SupersededByProductId`, the new row's `PreviousProductId`, and that list excludes the old row but includes the new one), `Update_OnAnAlreadyArchivedProduct_IsRejectedWithConflict`, `DeactivateAndReactivate_OnAnArchivedProduct_IsRejectedWithConflict`, and an audit-row test asserting both rows' event types independently.
- `StaffPinLoginTests`'s extended inventory — proves staff-PIN rejection for both new endpoint groups without per-entity duplication (existing convention).

### Commands run

```
dotnet build DaxaPos.sln
dotnet ef migrations add AddProductCatalogueFoundation --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~ProductCategoryEndpointsTests|FullyQualifiedName~ProductEndpointsTests"
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~StaffPinLoginTests"
dotnet test DaxaPos.sln
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "CREATE DATABASE daxapos_migration_check;"
dotnet ef database update ... --connection "...daxapos_migration_check..."   (clean-database migration re-verification, all 9)
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "DROP DATABASE daxapos_migration_check;"
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` — **445/445 passed** (92 unit tests + 353 API tests, up from 418 at Milestone C close — 27 new tests, zero regressions), against real Postgres, 0 failed, 0 skipped.
All 9 migrations verified to apply cleanly in sequence from an empty database.

### Deviations from the written plan (flagged, not silently made)

1. **No uniqueness constraint on `ProductCategory.Name` or `Product.Sku`/`Barcode`.** The plan's field lists don't call either out as a deduplicated business key (unlike `TaxDefinition.Code`/`TaxCategory.Code`, which OI-0007's own precedent and Milestone C treat as unique-per-tenant, or `StaffMember.StaffCode`, unique-per-organisation per PLAN-0003 Decision 10). Treated as following the `Location`/`Organisation` name-only-field precedent (no dedup) rather than inventing a new constraint the plan doesn't ask for. Documented explicitly in `docs/modules/catalog.md` and proven by dedicated tests (`Create_AllowsDuplicateName...`, `Create_AllowsDuplicateSkuAndBarcode...`) so a future milestone doesn't silently change this behaviour without noticing.
2. **Writes against an already-archived product return 409 Conflict** (update, deactivate, and reactivate all guard on `product.IsArchived`) — not explicitly specified by the plan's Milestone D prose, but a direct, necessary consequence of OI-0007's "the archived product remains available for historical ... records" — a historical record that can still be silently edited isn't actually historical. Flagged as an interpretation filling a gap, not a plan violation.
3. **Archive-and-replace raises two separate `ProductLifecycleDomainEvent`s** (`"Archived"` on the old row, `"CreatedFromReplace"` on the new row) rather than one combined event — each `AuditEvent` row has a single `EntityId`, and both rows need their own audit trail entry to be independently queryable by id later (e.g. "show me everything that happened to product X"). Matches the file's existing one-event-per-affected-entity convention (see `TaxCategoryDefinitionEndpoints`' create/delete pair) rather than inventing a new multi-entity event shape.
4. **`ProductResponse.PreviousProductId` is response-only, not a persisted column** — the plan's prose asks for it on the response but the entity field list has no matching column; the old row's `SupersededByProductId` already carries the forward link, so a backward pointer would be redundant to persist. Populated only on the archive-and-replace branch's response; `null` everywhere else.
5. **Archive-and-replace concurrency race is not filed as an open issue in this milestone** — the plan's Open Issues Required section explicitly reserves this for Milestone H ("New issues expected at Milestone H ... archive-and-replace concurrency race"), so filing it now would pre-empt that milestone's own sweep. Documented in `docs/modules/catalog.md`/this report instead, matching the plan's own sequencing.

None of these required backing out or redoing anything.

### ADR-0016 status re-check (per this session's explicit instruction not to move it silently)

Re-checked: still `docs/adr/proposed/`, not `accepted/`. `Product.Name`/`Description` and `ProductCategory.Name` are the first new translatable-in-future columns since Milestone B — mapped as plain invariant/fallback bounded `varchar` columns per the plan's pre-recorded ADR-0016 constraint, exactly as Milestone B did for tax labels. Nothing in Milestone D depends on ADR-0016's acceptance status. Still not moved.

### Blockers before Milestone E

None. Product catalogue foundation is fully CRUD-manageable via the API, including archive-and-replace; `dotnet build`/`dotnet test` are clean (445/445); migrations verified clean from empty. Milestone E (product variants and modifiers: `ProductVariant`, `ModifierGroup`, `Modifier`, `ProductModifierGroup`) can start on request.

One heads-up for whoever starts Milestone E: `ProductVariant`/`Modifier` price fields are deltas (`+`/`-` on the resolved base price), not absolute prices, per the plan's Domain Assumptions — don't reuse `Product.BasePrice`'s absolute-amount validation style (`>= 0`) for delta fields, which may legitimately be negative (a discount modifier). A valid `ProductId` for variant tests and a valid `ModifierGroupId`-assignment target now come from this milestone's `ProductEndpoints`/`ProductCategoryEndpoints`.

---

## Milestone E Report (2026-07-05)

Implemented per the plan's endpoint list exactly (20 endpoints total, matching the plan doc's count: 6 variant + 6 modifier group + 6 modifier + 2 join = 20). CRUD-endpoint acceptance-test convention used (not TDD-first), same as Milestones C/D.

### Files changed

New:
- `src/DaxaPos.Domain/Entities/ProductVariant.cs`, `ModifierGroup.cs`, `Modifier.cs`, `ProductModifierGroup.cs`
- `src/DaxaPos.Domain/Events/ProductVariantLifecycleDomainEvent.cs`, `ModifierGroupLifecycleDomainEvent.cs`, `ModifierLifecycleDomainEvent.cs`, `ProductModifierGroupChangedDomainEvent.cs`
- `src/DaxaPos.Api/Endpoints/Catalog/ProductVariantEndpoints.cs`, `ModifierGroupEndpoints.cs`, `ModifierEndpoints.cs`, `ProductModifierGroupEndpoints.cs`
- `src/DaxaPos.Persistence/Configurations/ProductVariantConfiguration.cs`, `ModifierGroupConfiguration.cs`, `ModifierConfiguration.cs`, `ProductModifierGroupConfiguration.cs`
- `src/DaxaPos.Persistence/Migrations/20260705041146_AddVariantsAndModifiers.cs`
- `tests/DaxaPos.Api.Tests/ProductVariantEndpointsTests.cs`, `ModifierGroupEndpointsTests.cs`, `ModifierEndpointsTests.cs`, `ProductModifierGroupEndpointsTests.cs`

Modified:
- `src/DaxaPos.Persistence/DaxaDbContext.cs` — 4 new `DbSet`s, 4 new fail-closed query filters.
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` — 4 new handler classes, same `$"{EntityType}{Action}"` convention.
- `src/DaxaPos.Api/Program.cs` — 4 new `AddScoped<IDomainEventHandler<...>>` registrations, 4 new `app.Map...Endpoints()` calls.
- `tests/DaxaPos.Api.Tests/StaffPinLoginTests.cs` — extended `AssertAllSensitiveEndpointsForbiddenAsync` with 5 catalogue-endpoint attempts, same shared-inventory convention.
- `docs/modules/catalog.md`, `docs/modules/pricing.md` (implementation-status sections), `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md`, `docs/CHANGELOG.md`.

No pricing resolver, menus, order integration, inventory, sold-out toggle, or UI were added — Milestone E is variants and modifiers only, exactly as scoped.

### Migration created

`20260705041146_AddVariantsAndModifiers` — creates `modifier_groups` (`TenantId`/`OrganisationId` indexed + FK-restricted, organisation-owned directly), `product_variants` (`TenantId`/`ProductId` indexed + FK-restricted, no `OrganisationId` column), `modifiers` (`TenantId`/`ModifierGroupId` indexed + FK-restricted, no `OrganisationId` column), `product_modifier_groups` (`TenantId`/`ProductId`/`ModifierGroupId` indexed + FK-restricted). No `HasData` seed rows. Verified to apply cleanly in sequence from an empty database (all 10 migrations, disposable throwaway Postgres database, then dropped).

### Endpoints implemented

- `POST/GET /api/v1/product-variants`, `GET/PATCH/{id}`, `.../deactivate`, `.../reactivate` (6, body carries `ProductId`).
- `POST/GET /api/v1/modifier-groups`, `GET/PATCH/{id}`, `.../deactivate`, `.../reactivate` (6, direct `OrganisationId` comparison).
- `POST/GET /api/v1/modifiers`, `GET/PATCH/{id}`, `.../deactivate`, `.../reactivate` (6, body carries `ModifierGroupId`).
- `POST /api/v1/product-modifier-groups` (assign), `DELETE /{id}` (unassign) (2).

All gated `catalog.manage` + `rejectStaffPin: true`, no exceptions — no sold-out-toggle-style staff-accessible endpoint appears until Milestone F.

### Variant/modifier price-delta behaviour

`ProductVariant.PriceDelta` and `Modifier.PriceDelta` accept any decimal value — positive, zero, or negative — with no `>= 0` guard, unlike `Product.BasePrice`/`TaxDefinition.RatePercent`. Proven by `[Theory]`-driven tests (`Create_AcceptsNegativeZeroAndPositivePriceDeltas`) in both `ProductVariantEndpointsTests.cs` and `ModifierEndpointsTests.cs`, each covering `-x`, `0`, and `+x`.

### Product/modifier-group linking behaviour

`ProductModifierGroupEndpoints.AssignAsync` validates both `ProductId` and `ModifierGroupId` independently belong to the caller's organisation (two separate lookups, each 404 on mismatch — mirroring `TaxCategoryDefinitionEndpoints`' multi-reference pattern from Milestone C). `UnassignAsync` hard-deletes the link after the same organisation check (walked through `Product`, since the link itself has no organisation column). No duplicate-assignment prevention was added (not required by the plan) and no update endpoint exists — `DisplayOrder` changes are unassign-then-reassign by design.

### Tests added

44 new integration tests across 4 files:
- `ProductVariantEndpointsTests.cs`, `ModifierEndpointsTests.cs` — standard CRUD matrix plus the `[Theory]` price-delta-sign tests and a cross-organisation-parent rejection test (`Product`/`ModifierGroup` respectively).
- `ModifierGroupEndpointsTests.cs` — standard CRUD matrix plus `Create_Rejects_SelectionMaxLessThanSelectionMin`.
- `ProductModifierGroupEndpointsTests.cs` — assign/unassign happy path (asserting `DisplayOrder` persisted directly via `DbContext`, since no list endpoint exists), `TenantId` rejection, missing-permission 403, both cross-organisation reference checks independently, cross-organisation unassign 404, and an audit-row test for both `"Assigned"`/`"Unassigned"`.
- `StaffPinLoginTests`'s extended inventory — proves staff-PIN rejection for all 4 endpoint groups without per-entity duplication.

### Commands run

```
dotnet build DaxaPos.sln
dotnet ef migrations add AddVariantsAndModifiers --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~ProductVariantEndpointsTests|FullyQualifiedName~ModifierGroupEndpointsTests|FullyQualifiedName~ModifierEndpointsTests|FullyQualifiedName~ProductModifierGroupEndpointsTests"
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~StaffPinLoginTests"
dotnet test DaxaPos.sln
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "CREATE DATABASE daxapos_migration_check;"
dotnet ef database update ... --connection "...daxapos_migration_check..."   (clean-database migration re-verification, all 10)
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "DROP DATABASE daxapos_migration_check;"
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` — **489/489 passed** (92 unit tests + 397 API tests, up from 445 at Milestone D close — 44 new tests, zero regressions), against real Postgres, 0 failed, 0 skipped.
All 10 migrations verified to apply cleanly in sequence from an empty database.

### Deviations from the written plan (flagged, not silently made)

1. **`TenantId` added directly to `ProductVariant`, `Modifier`, and `ProductModifierGroup`**, despite the plan's terse field lists omitting it for all three (only `ModifierGroup`'s bullet lists `TenantId` explicitly). This is a structural requirement, not an added feature: every tenant-owned entity in the codebase carries a direct `TenantId` for the fail-closed EF Core query filter (ADR-0015 §1) — without it, these tables would have no tenant isolation at all, which no interpretation of the plan could have intended. Matches the `Terminal` precedent exactly (`Terminal` has no `OrganisationId` column but does have `TenantId`).
2. **`ProductModifierGroup.CreatedAtUtc` added**, despite the plan's bullet listing only `ProductId`, `ModifierGroupId`, `DisplayOrder`. Every other entity in the codebase carries this column; omitting it here would have been the anomaly, and the audit event's own `OccurredAtUtc` doesn't substitute for a queryable creation timestamp on the row itself. `IsActive`/archive fields were *not* added, matching the plan's clear intent that this join has no lifecycle beyond assign/unassign.
3. **No list/read/update endpoint for `ProductModifierGroup`**, matching the plan's explicit "2 (assign/unassign...)" endpoint count. Tests assert attachment state and `DisplayOrder` directly against the database rather than via an API call, since none exists for this purpose.
4. **`ModifierGroupEndpoints` validates `SelectionMax >= SelectionMin`** — not specified by the plan's field list, but a minimal sanity check preventing a nonsensical configuration (e.g. `SelectionMin: 3, SelectionMax: 1`), consistent in spirit with `TaxDefinition.RatePercent >= 0`-style guards elsewhere. No cross-validation against `IsRequired` was added (not requested).

None of these required backing out or redoing anything.

### ADR-0016 status re-check (per this session's explicit instruction not to move it silently)

Re-checked: still `docs/adr/proposed/`, not `accepted/`. `ProductVariant.Name`, `ModifierGroup.Name`, and `Modifier.Name` are the newest translatable-in-future columns — mapped as plain invariant/fallback bounded `varchar` per the plan's pre-recorded ADR-0016 constraint, matching every prior milestone. Nothing in Milestone E depends on ADR-0016's acceptance status. Still not moved.

### Blockers before Milestone F

None. Variants and modifiers are fully CRUD-manageable via the API; `dotnet build`/`dotnet test` are clean (489/489); migrations verified clean from empty. Milestone F (location-level catalog overrides and the pricing resolver: `ProductLocationOverride`, `VenueTaxConfiguration`, `PriceResolver`, plus the sold-out toggle) can start on request.

One heads-up for whoever starts Milestone F: it introduces the plan's first genuinely staff-accessible write endpoint (the sold-out toggle, gated `catalog.sold-out-toggle` + `rejectStaffPin: false`, granted to `Staff` since Milestone A) — this is the plan's single highest-risk design call (see Risks section), so double-check the permission/rejectStaffPin combination against the plan's exact wording before writing the endpoint, not just copying the `catalog.manage`/`rejectStaffPin: true` pattern used by every endpoint so far. `VenueTaxConfiguration` absence-handling (404 vs. silent default) was already decided (Human Decision #5, approved: 404/explicit-error) — don't re-litigate it.

---

## Milestone F Report (2026-07-05)

**Permission check before implementation, as instructed:** re-read the Milestone F section's endpoint table before writing any code. It says `pricing.manage` for `ProductLocationOverride`/`VenueTaxConfiguration` — **not `catalog.manage`** — and `catalog.sold-out-toggle` + `rejectStaffPin: false` for the sold-out toggle. Implemented exactly as the plan states, not as a task-prompt summary elsewhere loosely paraphrased it. `PriceResolver` was TDD'd first — tests written and confirmed RED (missing types) before any implementation existed, then implementation written and confirmed GREEN — per CLAUDE.md's mandatory-TDD rule for financial logic (`TaxCalculationEngine`/`PriceResolver` named explicitly in the plan's "Tests To Run Later" section). CRUD endpoints used the acceptance-test-alongside convention, same as every prior milestone.

### Files changed

New:
- `src/DaxaPos.Domain/Entities/ProductLocationOverride.cs`, `VenueTaxConfiguration.cs`
- `src/DaxaPos.Domain/Events/ProductLocationOverrideChangedDomainEvent.cs`, `VenueTaxConfigurationLifecycleDomainEvent.cs`
- `src/DaxaPos.Application/Pricing/PriceResolutionModels.cs` (`ResolvedPrice`), `PriceResolutionResult.cs` (`PriceResolutionErrorCode`, `PriceResolutionResult`), `PriceResolver.cs`
- `src/DaxaPos.Api/Endpoints/Catalog/ProductLocationOverrideEndpoints.cs`, `ProductSoldOutEndpoints.cs`
- `src/DaxaPos.Api/Endpoints/Tax/VenueTaxConfigurationEndpoints.cs` — placed here, not `Endpoints/Catalog/`, matching the plan's own "Files Likely To Change" section which explicitly lists `VenueTaxConfigurationEndpoints` under `Endpoints/Tax/` alongside the other tax-config endpoint files.
- `src/DaxaPos.Persistence/Configurations/ProductLocationOverrideConfiguration.cs`, `VenueTaxConfigurationConfiguration.cs`
- `src/DaxaPos.Persistence/Migrations/20260705051120_AddLocationOverridesAndVenueTaxConfig.cs`
- `tests/DaxaPos.UnitTests/Pricing/PriceResolverTests.cs`
- `tests/DaxaPos.Api.Tests/ProductLocationOverrideEndpointsTests.cs`, `VenueTaxConfigurationEndpointsTests.cs`, `ProductSoldOutEndpointsTests.cs`

Modified:
- `src/DaxaPos.Persistence/DaxaDbContext.cs` — 2 new `DbSet`s, 2 new fail-closed query filters.
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` — 2 new handler classes.
- `src/DaxaPos.Api/Program.cs` — 2 new `AddScoped<IDomainEventHandler<...>>` registrations, 3 new `app.Map...Endpoints()` calls.
- `tests/DaxaPos.Api.Tests/StaffPinLoginTests.cs` — extended `AssertAllSensitiveEndpointsForbiddenAsync` with the `pricing.manage` endpoints (never the sold-out toggle, which has its own dedicated positive-allow tests instead).
- `docs/modules/catalog.md`, `docs/modules/pricing.md`, `docs/modules/tax.md`, `docs/architecture/tax-engine.md`, `docs/architecture/multi-location.md` (implementation-status sections), `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md`, `docs/CHANGELOG.md`.

No menus, order-price snapshotting, receipts, or UI were added — Milestone F is location overrides and the pricing resolver only, exactly as scoped.

### Migration created

`20260705051120_AddLocationOverridesAndVenueTaxConfig` — creates `product_location_overrides` (unique `(LocationId, ProductId)` index — one override per pair, since two would be ambiguous which applies) and `venue_tax_configurations` (unique `LocationId` index — one row per location, per the plan's own text). Neither carries an `OrganisationId` column; both derive it via `LocationId`. No `HasData` seed rows. Verified to apply cleanly in sequence from an empty database (all 11 migrations, disposable throwaway Postgres database, then dropped).

### Endpoints implemented

- `POST/GET /api/v1/product-location-overrides`, `GET/PATCH/{id}`, `DELETE /{id}` (5, `pricing.manage`, `rejectStaffPin: true`) — hard delete (a pure config override, not a financial record).
- `POST /api/v1/products/{productId}/locations/{locationId}/sold-out` (1, `catalog.sold-out-toggle`, `rejectStaffPin: false`).
- `POST/GET /api/v1/venue-tax-configurations`, `GET/PATCH/{id}` (4, `pricing.manage`, `rejectStaffPin: true`) — no delete/deactivate/reactivate (no `IsActive` on the entity).

10 total, matching the plan's exact count.

### ProductLocationOverride behaviour

Create/Update validate that both the referenced `Product` and `Location` belong to the caller's organisation (two independent lookups, 404 on either mismatch). A duplicate `(LocationId, ProductId)` pair on create returns 409 Conflict — not specified by the plan's prose, but a necessary consequence of the unique index (which one would even apply otherwise). `PriceOverride` must be non-negative when supplied (an absolute venue-set price, unlike variant/modifier deltas). Delete is a genuine hard delete — removing the row reverts to the organisation-wide `Product` defaults per ADR-0003, exactly as the plan's Domain Assumptions describe override absence.

### VenueTaxConfiguration behaviour

One row per location (unique index + a pre-check `Conflict` on duplicate create, mirroring `ProductLocationOverride`). `GetByIdAsync` for a genuinely-missing id returns 404 like any other entity — there is no special "compute a default" code path anywhere in this milestone; the fail-closed behaviour Human Decision #5 asked for is simply the absence of any auto-provisioning logic, not a bespoke mechanism. `TaxCalculationMode`'s type is the existing `TaxCalculationScope` enum (`PerLine`/`PerComponent`) — the plan's field list names a "`TaxCalculationMode` enum" without defining distinct values, and this concept is identical to the one `TaxDefinition.CalculationScope` already models, so introducing a second near-duplicate enum was judged unnecessary.

### PriceResolver behaviour

`PriceResolver.Resolve(Product, ProductVariant?, IReadOnlyList<Modifier>, ProductLocationOverride?, VenueTaxConfiguration?)` → `PriceResolutionResult` (mirroring `TaxCalculationEngine`'s typed-result shape exactly). Resolution order: `Product.BasePrice + (variant?.PriceDelta ?? 0) + modifiers.Sum(m => m.PriceDelta)`, then `locationOverride?.PriceOverride` **replaces** that computed total outright when set (never adds to it — the plan's explicit Design Decision), then `IsTaxInclusive` is read from `venueTaxConfiguration.TaxInclusivePricing`. If `venueTaxConfiguration` is `null`, the function returns `Failure(PriceResolutionErrorCode.MissingVenueTaxConfiguration)` immediately, before computing anything — the same fail-closed-first-not-last pattern `TaxCalculationEngine` uses for `MissingTaxConfiguration`. Pure: no DB dependency, no constructor parameters, deterministic (proven by a repeat-call test). `ProductLocationOverride` being `null` is not a failure condition — it is the expected default-and-override path (ADR-0003), unlike a missing `VenueTaxConfiguration`.

### Sold-out toggle behaviour

`ProductSoldOutEndpoints.SetSoldOutAsync` is deliberately separate code from `ProductLocationOverrideEndpoints` — it may only ever set `IsSoldOut`; the request DTO (`SetSoldOutRequest`) has no `PriceOverride`/`IsAvailable` field at all, so there is no code path by which a staff-PIN session could reach those fields even by accident. It upserts: if no `ProductLocationOverride` exists yet for the `(product, location)` pair, one is created with `IsAvailable = true`, `PriceOverride = null`; if one exists, only `IsSoldOut` is touched. Beyond the plan's literal wording, this endpoint also checks `authContext.LocationId is not null && authContext.LocationId != locationId` → 404 — a location-bound session (staff PIN, from its registered device) may only toggle its own location, checked independently of the organisation match, the same way `rejectStaffPin` is checked independently of the permission code. An organisation-scoped admin session (`LocationId` null) has no such restriction. The domain event carries both `UserId` and `StaffMemberId` (only one populated per event) since this is the plan's first staff-PIN-accessible catalogue write, and its `Action` (`"SoldOutToggled"`) is kept distinct from the CRUD endpoint's `"Created"`/`"Updated"`/`"Deleted"` so an auditor can tell which surface made a given change.

### Tests added

44 new tests: 12 unit (`PriceResolverTests.cs`, TDD-first) + 32 integration across 3 files:
- `ProductLocationOverrideEndpointsTests.cs`, `VenueTaxConfigurationEndpointsTests.cs` — standard CRUD/authorization matrix (as prior milestones), plus duplicate-pair/duplicate-location `Conflict` tests and, for venue tax config, `Get_MissingConfiguration_ReturnsNotFound_InsteadOfSilentlyDefaulting` — the explicit fail-closed proof.
- `ProductSoldOutEndpointsTests.cs` — the full staff-PIN scenario chain (device registration → staff member → `Staff` role assignment → PIN login), proving: the toggle **succeeds** for a staff session with `catalog.sold-out-toggle`; the *same* session still gets 403 on the `pricing.manage`-gated `PATCH` (the plan's explicit asymmetry-proof pair); upsert-creates with safe defaults; upsert-updates without touching `PriceOverride`/`IsAvailable`; a session lacking `catalog.sold-out-toggle` (seeded directly, since every current role includes it) is rejected; cross-organisation and cross-location (same organisation, different location) are both blocked; and an audit row is written with `StaffMemberId` set, `UserId` null.
- `StaffPinLoginTests`'s extended inventory — the `pricing.manage` endpoints only.

### Commands run

```
dotnet build tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj      (RED: 2 compile errors, expected symbols)
dotnet test tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj --filter "FullyQualifiedName~PriceResolverTests"   (GREEN: 12/12)
dotnet build DaxaPos.sln
dotnet ef migrations add AddLocationOverridesAndVenueTaxConfig --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~ProductLocationOverrideEndpointsTests|FullyQualifiedName~VenueTaxConfigurationEndpointsTests|FullyQualifiedName~ProductSoldOutEndpointsTests"
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~StaffPinLoginTests"
dotnet test DaxaPos.sln
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "CREATE DATABASE daxapos_migration_check;"
dotnet ef database update ... --connection "...daxapos_migration_check..."   (clean-database migration re-verification, all 11)
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "DROP DATABASE daxapos_migration_check;"
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` — **533/533 passed** (104 unit tests + 429 API tests, up from 489 at Milestone E close — 44 new tests, zero regressions), against real Postgres, 0 failed, 0 skipped.
All 11 migrations verified to apply cleanly in sequence from an empty database.

### Deviations from the written plan (flagged, not silently made)

1. **The sold-out toggle additionally enforces location-scoping for staff-PIN sessions** (`authContext.LocationId != locationId` → 404) — not specified anywhere in the plan's prose, which only calls out the organisation check implicitly (via the general "cross-organisation is blocked" pattern). Without this, any staff PIN at any location in an organisation could toggle sold-out state at every other location in that organisation, which doesn't match the operational reality this endpoint exists for (a POS terminal managing its own counter's stock). Flagged as a security-relevant addition, not assumed silently — a different reasonable reading could omit it, but leaving it out seemed like the greater risk.
2. **`VenueTaxConfigurationEndpoints.cs` lives under `Endpoints/Tax/`, not `Endpoints/Catalog/`** — matching the plan's own "Files Likely To Change" section (written during the original planning pass) rather than where a first guess might place it alongside `ProductLocationOverrideEndpoints`.
3. **`TaxCalculationMode` reuses `TaxCalculationScope`** rather than a new enum — see VenueTaxConfiguration behaviour above.
4. **Duplicate-pair/duplicate-location `Conflict` checks** on both `ProductLocationOverride` and `VenueTaxConfiguration` creation — not specified by the plan's field lists, but required by the unique indexes both entities need (documented under each entity's behaviour above).
5. **`ProductLocationOverrideChangedDomainEvent` carries both `UserId` and `StaffMemberId`** — the first Milestone F/earlier lifecycle event to need this, since it can be raised by either an admin (`pricing.manage`) or a staff session (the sold-out toggle). Every other lifecycle event in the codebase carries only `UserId` because nothing else could ever be staff-triggered until now.

None of these required backing out or redoing anything.

### ADR-0016 status re-check (per this session's explicit instruction not to move it silently)

Re-checked: still `docs/adr/proposed/`, not `accepted/`. Neither `ProductLocationOverride` nor `VenueTaxConfiguration` adds a `Name`-like translatable column, so nothing in Milestone F could depend on ADR-0016's acceptance status. Still not moved.

### Blockers before Milestone G

None. Location-level overrides, venue tax configuration, and the pricing resolver are fully functional; `dotnet build`/`dotnet test` are clean (533/533); migrations verified clean from empty. Milestone G (menu construction and the resolved-menu read endpoint: `Menu`, `MenuSection`, `MenuSectionItem`, `MenuAvailabilityRule`) can start on request.

One heads-up for whoever starts Milestone G: the resolved-menu read endpoint (`GET /api/v1/menus/resolved?locationId={id}`) is the plan's *other* deliberately staff-accessible endpoint, but with a different mechanism than the sold-out toggle — **no permission code at all**, only `.RequireAuthorization()` (Human Decision #1, approved). Don't reach for `RequirePermission(..., rejectStaffPin: false)` here; the plan is explicit that this read needs no permission gate whatsoever, matching `/auth/me`'s existing precedent. The resolved-menu endpoint must exclude products where `ProductLocationOverride.IsAvailable == false || IsSoldOut == true` (now buildable, from this milestone) and resolve prices via this milestone's `PriceResolver` — both dependencies are ready. The menu org-wide/location-specific merge precedence (location wins) and the day/time `MenuAvailabilityRule` shape are both already fixed by the plan (Human Decision #7, approved) — don't re-litigate either.

---

## Milestone G Report (2026-07-05)

**Recovery context:** implementation of this milestone was interrupted mid-session (API connection loss) after the 4 entities, 4 domain events, 4 EF configurations, 4 configuration-endpoint files, and 3 tracked-file edits (`Location.TimeZoneId` + its EF mapping + `DaxaDbContext` `DbSet`s/query filters) had already been written, but before `ResolvedMenuEndpoints.cs`, the migration, `Program.cs` wiring, audit handlers, or any test existed. The recovery session's crash-tail notes claimed `MenuAvailabilityRuleEndpoints.cs` was corrupted (a `sing` truncation in place of `using` at the top of the file) — this was checked byte-for-byte (hex dump of the first 50 bytes) and found **not corrupted**; the file on disk started cleanly with `using System.Text.Json;`, and `dotnet build DaxaPos.sln` succeeded with 0 warnings/errors before any further changes were made. All 8 partial files and the 3 tracked edits were confirmed complete, well-formed, and consistent with the plan's design (permission tables, tenant/organisation isolation walks, JSON-serialized audit snapshots) — nothing was rewritten from scratch; the session continued directly from that state.

Implemented per the plan's endpoint list and exact permission table (re-read before writing `ResolvedMenuEndpoints.cs`, per the standing instruction to check the plan's own table rather than a paraphrase): `menus.manage` + `rejectStaffPin: true` on `MenuEndpoints`/`MenuSectionEndpoints`/`MenuSectionItemEndpoints`/`MenuAvailabilityRuleEndpoints`; **no permission code, `.RequireAuthorization()` only**, on `ResolvedMenuEndpoints`. CRUD-endpoint acceptance-test convention used for the 4 configuration endpoint groups (not TDD-first, matching every prior milestone's CRUD files); the resolved-menu endpoint was also acceptance-tested rather than TDD'd — it composes already-TDD'd `PriceResolver` rather than introducing new financial-calculation logic itself, so CLAUDE.md's mandatory-TDD rule (which named `TaxCalculationEngine`/`PriceResolver` specifically) does not extend to it.

### Files changed

New (written before the crash, confirmed intact, no changes needed):
- `src/DaxaPos.Domain/Entities/Menu.cs`, `MenuSection.cs`, `MenuSectionItem.cs`, `MenuAvailabilityRule.cs`
- `src/DaxaPos.Domain/Enums/DaysOfWeekMask.cs`
- `src/DaxaPos.Domain/Events/MenuLifecycleDomainEvent.cs`, `MenuSectionLifecycleDomainEvent.cs`, `MenuSectionItemChangedDomainEvent.cs`, `MenuAvailabilityRuleChangedDomainEvent.cs`
- `src/DaxaPos.Persistence/Configurations/MenuConfiguration.cs`, `MenuSectionConfiguration.cs`, `MenuSectionItemConfiguration.cs`, `MenuAvailabilityRuleConfiguration.cs`
- `src/DaxaPos.Api/Endpoints/Menus/MenuEndpoints.cs`, `MenuSectionEndpoints.cs`, `MenuSectionItemEndpoints.cs`, `MenuAvailabilityRuleEndpoints.cs`

New (written this session, continuing from the recovered state):
- `src/DaxaPos.Api/Endpoints/Menus/ResolvedMenuEndpoints.cs`
- `src/DaxaPos.Persistence/Migrations/20260705102237_AddMenus.cs` (+ `.Designer.cs`)
- `tests/DaxaPos.Api.Tests/MenuEndpointsTests.cs`, `MenuSectionEndpointsTests.cs`, `MenuSectionItemEndpointsTests.cs`, `MenuAvailabilityRuleEndpointsTests.cs`, `ResolvedMenuEndpointsTests.cs`

Modified (tracked edits present before the crash, confirmed intact):
- `src/DaxaPos.Domain/Entities/Location.cs` — added `TimeZoneId` (`string`, default `"UTC"`), needed for `MenuAvailabilityRule` evaluation in the location's own local time.
- `src/DaxaPos.Persistence/Configurations/LocationConfiguration.cs` — `TimeZoneId` column mapping (required, max length 100, default `"UTC"`).
- `src/DaxaPos.Persistence/DaxaDbContext.cs` — 4 new `DbSet`s, 4 new fail-closed query filters (`Menu`/`MenuSection`/`MenuSectionItem`/`MenuAvailabilityRule`).

Modified (this session):
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` — 4 new handler classes (`MenuLifecycleAuditHandler`, `MenuSectionLifecycleAuditHandler`, `MenuSectionItemChangedAuditHandler`, `MenuAvailabilityRuleChangedAuditHandler`).
- `src/DaxaPos.Api/Program.cs` — 4 new `AddScoped<IDomainEventHandler<...>>` registrations, 5 new `app.Map...Endpoints()` calls (the 4 configuration groups plus `ResolvedMenuEndpoints`, none of which had been wired in before the crash).
- `tests/DaxaPos.Api.Tests/StaffPinLoginTests.cs` — extended `AssertAllSensitiveEndpointsForbiddenAsync` with the 4 `menus.manage` configuration endpoints (never the resolved-menu endpoint, which has its own dedicated staff-**succeeds** test instead, matching the `ProductSoldOutEndpointsTests` precedent for the plan's other staff-accessible surface).
- `docs/modules/menus.md`, `docs/modules/catalog.md`, `docs/modules/pricing.md` (implementation-status sections), `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md`, `docs/CHANGELOG.md`.

No order integration, payments, receipts, UI, sync, inventory, KDS, or advanced pricing/discount rules were added — Milestone G is menu construction and the resolved-menu read endpoint only, exactly as scoped.

### Migration created

`20260705102237_AddMenus` — adds `locations.TimeZoneId` (`character varying(100)`, not null, default `'UTC'`), and creates `menus`, `menu_sections`, `menu_section_items`, `menu_availability_rules` (all four `TenantId`-indexed + FK-restricted; `menus` additionally `OrganisationId`/`LocationId`-indexed). No `HasData` seed rows. Verified to apply cleanly in sequence from an empty database (all 12 migrations, disposable throwaway Postgres database, then dropped — not the shared dev database, which was migrated separately for the working tree).

### Endpoints implemented

- `POST/GET /api/v1/menus`, `GET/PATCH/{id}`, `.../deactivate`, `.../reactivate` (6).
- `POST/GET /api/v1/menu-sections`, `GET/PATCH/{id}` (4 — no separate deactivate/reactivate; `IsActive` is one of the fields the single `PATCH` updates).
- `POST /api/v1/menu-section-items` (assign), `DELETE /{id}` (unassign) (2).
- `POST/GET /api/v1/menu-availability-rules`, `DELETE /{id}` (3 — create/list/delete only, no update; a rule is replaced by delete-then-recreate).
- `GET /api/v1/menus/resolved?locationId={id}` (1).

16 total, matching the plan's exact count.

### ResolvedMenuEndpoints behaviour

`GetResolvedMenuAsync` resolves in this order: (1) a location-bound staff-PIN session's own `AuthContext.LocationId` must match the requested `locationId` (404 otherwise, independent of the organisation check — the same rule `ProductSoldOutEndpoints` uses); (2) the `Location` must exist and belong to the caller's organisation (404); (3) a `VenueTaxConfiguration` must exist for the location (404 — fails closed, matching `VenueTaxConfigurationEndpoints`' own missing-config behaviour, approved Human Decision #5); (4) applicable `Menu`s are loaded (`OrganisationId` match, `IsActive`, and `LocationId == null || LocationId == locationId`); (5) each menu's `MenuAvailabilityRule`s are evaluated against `Location.TimeZoneId`-converted local time — zero active rules means always available, one or more means available only if at least one rule's `DaysOfWeekMask` includes today and `StartTimeLocal <= now < EndTimeLocal`; (6) for products appearing in both an available org-wide and an available location-specific menu, the location-specific occurrence is kept and the org-wide one dropped entirely (not merged section-by-section); (7) each surviving item is excluded if its `Product` is inactive/archived, or its `ProductLocationOverride` (if any) says `IsAvailable == false` or `IsSoldOut == true`; (8) `PriceResolver.Resolve(product, null, [], productLocationOverride, venueTaxConfiguration)` resolves the price — `variant`/`modifiers` are always empty since `MenuSectionItem` carries only a `ProductId`; (9) each item's `TaxCategory.Code`/`TaxTreatment` are included as marker metadata, not a calculated tax amount.

Response shape: `ResolvedMenuResponse(LocationId, Sections)` where each `ResolvedMenuSectionResponse` carries its originating `MenuId`/`MenuSectionId`/`Name`/`DisplayOrder` and its list of `ResolvedMenuItemResponse`s. Sections from different `Menu`s are never merged into one — only individual *products* are deduplicated across menus per the approved merge-precedence rule; two menus are never reconciled section-by-section since nothing in the plan establishes a rule for matching sections by name across separate `Menu` rows.

### Tests added

44 new integration tests across 5 files:
- `MenuEndpointsTests.cs`, `MenuSectionEndpointsTests.cs`, `MenuAvailabilityRuleEndpointsTests.cs` — standard CRUD/authorization matrix (create/read/update/deactivate-reactivate or create/list/delete as applicable, 400 on client-supplied `TenantId`, 403 without `menus.manage`, 404 cross-organisation, audit-row assertions), plus entity-specific validation (`MenuAvailabilityRule` rejects `DaysOfWeekMask.None` and non-strictly-ordered start/end times).
- `MenuSectionItemEndpointsTests.cs` — assign/unassign happy path (`DisplayOrder` persisted, asserted directly via `DbContext.IgnoreQueryFilters()` since no list endpoint exists for this join, matching `ProductModifierGroupEndpointsTests`' precedent), rejection of an inactive/archived product, both cross-organisation reference checks (section, product) independently, missing-permission 403, and an audit-row test for both `"Assigned"`/`"Unassigned"`.
- `ResolvedMenuEndpointsTests.cs` — the full staff-PIN scenario chain (device registration → staff member → `Staff` role assignment → PIN login), proving: **a staff-PIN session succeeds** (the critical assertion, mirroring `ProductSoldOutEndpointsTests`' asymmetry-proof pattern but for a no-permission-code endpoint rather than an `Operational`-permission one); unauthenticated requests get 401; sold-out and unavailable products are excluded; an org-wide and a location-specific menu merge with the location-specific item winning, including its own `DisplayOrder` and section; a menu with an availability rule covering every day except today is hidden, and the same-shaped rule covering every day including today is shown; prices come from `PriceResolver`'s base-price and location-override paths, matching `PriceResolverTests`' own expectations for equivalent inputs; a missing `VenueTaxConfiguration` fails the whole endpoint closed (404); and both cross-organisation and cross-location (staff session's own location) access are blocked.
- `StaffPinLoginTests`'s extended inventory — the 4 `menus.manage` configuration endpoints only (not the resolved-menu endpoint, proven separately as staff-**succeeds** in `ResolvedMenuEndpointsTests`).

### Commands run

```
dotnet build DaxaPos.sln                                           (0 warnings/errors, confirmed before any Milestone G code was added this session)
dotnet ef migrations add AddMenus --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~MenuEndpointsTests|FullyQualifiedName~MenuSectionEndpointsTests|FullyQualifiedName~MenuSectionItemEndpointsTests|FullyQualifiedName~MenuAvailabilityRuleEndpointsTests"
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~ResolvedMenuEndpointsTests"
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~StaffPinLoginTests"
dotnet test DaxaPos.sln
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "CREATE DATABASE daxapos_migration_check;"
dotnet ef database update ... --connection "...daxapos_migration_check..."   (clean-database migration re-verification, all 12)
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "DROP DATABASE daxapos_migration_check;"
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` — **577/577 passed** (104 unit tests + 473 API tests, up from 533 at Milestone F close — 44 new tests, zero regressions), against real Postgres, 0 failed, 0 skipped.
All 12 migrations verified to apply cleanly in sequence from an empty database.

### Deviations from the written plan (flagged, not silently made)

1. **The recovery-session crash-tail's claim of file corruption in `MenuAvailabilityRuleEndpoints.cs` did not match the file's actual on-disk state.** Verified with a hex dump before trusting the claim (per systematic-debugging practice: verify, don't assume) — the file was complete and well-formed. Recorded here so a future reader doesn't wonder why no corruption-repair work appears in this report despite the recovery brief describing one.
2. **The resolved-menu response groups items by section, one section list entry per surviving `MenuSection`** — not specified by the plan's prose beyond the merge-precedence and exclusion rules themselves. A flat, ungrouped list was considered and rejected: a POS sales screen (the endpoint's stated consumer) needs section grouping to render a usable menu, and grouping by the *winning* menu's own sections (rather than attempting to reconcile org-wide and location-specific sections with the same name) avoids inventing an unstated name-matching rule.
3. **`ResolvedMenuItemResponse` exposes `TaxCategory.Code`/`TaxTreatment` as marker metadata, not a `TaxDefinition`-resolved receipt marker letter (`F`/`Z`/`E`).** The plan's Milestone G bullet says "includes the product's tax category marker info (not a calculated tax amount)" — resolving an actual `TaxDefinition`/`ReceiptMarkerCode` would require the `(TaxCategory, Location)` → `TaxDefinition` resolution step the plan's own Architecture Assumptions explicitly defer to PLAN-0005 ("a separate, thin resolution step ... callable independently"). Exposing `TaxCategory`'s own fields satisfies "tax category marker info" literally without building that deferred resolution layer early.
4. **`ResolvedMenuEndpoints` raises no domain event** — it is a pure read, matches the plan's explicit "must not mutate data" requirement, and every other lifecycle/changed event in the codebase exists to audit a write.
5. **Availability-window boundary semantics (`StartTimeLocal <= now < EndTimeLocal`, half-open) were not specified by the plan** beyond "strictly before" for `Start`/`End` themselves — chosen as the least surprising interpretation (a 9am–5pm window is open at exactly 9:00 and closed at exactly 5:00) and proven by dedicated tests rather than left to accidental `TimeOnly` comparison-operator behaviour.

None of these required backing out or redoing anything.

### ADR-0016 status re-check (per this session's explicit instruction not to move it silently)

Re-checked: still `docs/adr/proposed/`, not `accepted/`. `Menu.Name`/`MenuSection.Name` are the newest translatable-in-future columns — mapped as plain invariant/fallback bounded `varchar` per the plan's pre-recorded constraint, matching every prior milestone. Nothing in Milestone G depends on ADR-0016's acceptance status. Still not moved — now touched by every milestone since B; recommend the human action the one-line `git mv` whenever convenient, most naturally alongside Milestone H's documentation closeout.

### Blockers before Milestone H

None. Menu construction and the resolved-menu read endpoint are fully functional; `dotnet build`/`dotnet test` are clean (577/577); migrations verified clean from empty. Milestone H (consolidation, RBAC sweep, and documentation closeout — test-and-documentation-only, per the plan) can start on request.

One heads-up for whoever starts Milestone H: it is scoped as test-and-documentation-only (no entities, no migration, no endpoints) — extend `RbacTests.cs`'s endpoint inventory with every Milestone A–G permission-gated endpoint (including the two deliberate staff-PIN-succeeds rows: the sold-out toggle and the resolved-menu read, each asserted explicitly rather than merely absent from the rejection inventory), confirm zero new `IgnoreQueryFilters()` call sites, re-verify all 12 migrations from empty one more time, and file the three candidate open issues the plan's own Open Issues Required section already names (archive-and-replace concurrency race; menu merge-precedence revisit if real use suggests a different rule; `VenueTaxConfiguration`-absence behaviour, already resolved as 404 but worth a formal OI record). Also action the ADR-0016 `git mv` to `docs/adr/accepted/` while touching documentation anyway — it has been re-checked-but-not-moved every milestone since B.

---

## Milestone H Report (2026-07-05)

Working tree confirmed clean before editing except one pre-existing uncommitted change: a whitespace-only markdown table-separator normalization in `ADR-0016-multi-language-and-localisation-strategy.md` (`| --- | --- |` → `| --- | --- |`, no semantic change), absorbed into this milestone's ADR-0016 edits since that file was already being touched for the acceptance move.

Test-and-documentation-only, exactly as scoped: no new entities, no migration, no new endpoint groups, no order/payment/receipt/UI/sync/inventory/KDS/pricing-rule work.

### RBAC sweep

`tests/DaxaPos.Api.Tests/RbacTests.cs`'s `PermissionGatedEndpoints()` — previously PLAN-0003-only (Organisation/Location/Terminal/Device/StaffMember, 29 routes) — extended with all 73 PLAN-0004 `rejectStaffPin: true` routes: Milestone C tax configuration (17: templates read, tax-definitions incl. from-template, tax-categories, tax-category-definitions), Milestone D product catalogue (12: categories, products), Milestone E variants/modifiers (20: variants, modifier groups, modifiers, product-modifier-group assign/unassign), Milestone F location overrides/venue tax config (9: `product-location-overrides`, `venue-tax-configurations` — **not** the sold-out toggle, see below), Milestone G menus (15: menus, sections, section-items, availability rules — **not** the resolved-menu read, see below). This one addition drives four theories automatically (`Unauthenticated_Request_Returns401`, `GarbageBearerToken_Returns401`, `AuthenticatedSession_WithoutThePermission_Returns403`, `DeviceToken_Returns403`, `StaffPinSession_Returns403`) across all 73 new routes, per the file's existing single-inventory design. `AllProtectedEndpoints()` additionally gained the resolved-menu read (`GET /api/v1/menus/resolved`) for its 401/garbage-token sweep only.

**Deliberately excluded from the sweep, not an oversight:** `catalog.sold-out-toggle` (`rejectStaffPin: false`) and the resolved-menu read (no permission code at all) are PLAN-0004's two staff-accessible exceptions — adding either to `PermissionGatedEndpoints()` would make the shared `StaffPinSession_Returns403_OnEveryRejectStaffPinEndpoint` theory assert something false for that one row. Both already have their own dedicated staff-**succeeds** proof (`ProductSoldOutEndpointsTests.StaffSession_WithCatalogSoldOutToggle_CanSetSoldOut`, `ResolvedMenuEndpointsTests.StaffSession_CanReadResolvedMenu`) — duplicating that proof here would be redundant at best and actively wrong if placed in the wrong theory.

**Permission categories** — verified via the existing `StaffPinLoginTests.PermissionCatalogue_ClassifiesPLAN0004MilestoneAPermissions_ByCategory` test (already covers all 4 codes: `catalog.manage`/`pricing.manage`/`menus.manage` = `AdminSensitive`, `catalog.sold-out-toggle` = `Operational`) — already correct, no changes needed.

**Role grants** — verified by reading `RolePermissionConfiguration.cs` directly: `SystemAdmin`/`OrganisationOwner`/`VenueManager` all hold `catalog.manage`/`pricing.manage`/`menus.manage`/`catalog.sold-out-toggle`; `Staff` holds only `catalog.sold-out-toggle` (its first-ever grant, Milestone A) — matches the plan's permission table exactly, already correct.

**Stale comment fixed in passing:** `RbacScenarioFixture.NoPermissionClient`'s doc comment claimed the seeded `Staff` role "deliberately carries zero permission codes" — true when PLAN-0003 wrote it, false since Milestone A gave `Staff` its first grant (`catalog.sold-out-toggle`). Corrected to say so explicitly; the test behaviour itself was never wrong (that one `Operational` permission doesn't match any code in the inventory), only the comment's claim.

**Deliberately not extended:** `RbacTests.ValidSession_ForAnotherTenant_SeesNothing_AndNeverAnError` (the cross-tenant "hijack" 404 proof) was not extended to PLAN-0004 entities. That test is about tenant-isolation data leakage, not permission gating — a different axis from this sweep's scope. Every PLAN-0004 milestone's own endpoint test file already carries its own cross-tenant/cross-organisation 404 proof for its entities (e.g. `ProductEndpointsTests.Read_Blocked_ForDifferentTenant`, `MenuEndpointsTests.ReadAndUpdate_Blocked_ForDifferentOrganisation_SameTenant`), so the coverage exists, just not consolidated into this one giant test. Flagged as a scoping decision, not a silent gap.

### Staff-PIN sweep

`StaffPinLoginTests.AssertAllSensitiveEndpointsForbiddenAsync`'s shared inventory was already complete as of the Milestone G session (extended incrementally at C/D/E/F/G) — re-verified by listing every `staffClient.*Async` call in the file: one representative GET+POST pair per Milestone C–G endpoint group, matching the file's own documented "shared inventory, not per-entity duplication" convention. No additions needed this milestone.

### Endpoint registration sweep

`src/DaxaPos.Api/Program.cs` re-checked against all 18 required groups (`TaxDefinitionTemplateEndpoints` through `ResolvedMenuEndpoints`) — all 18 already registered, in the same order they were added at each milestone. No fix needed; the Milestone G session had already wired everything.

### IgnoreQueryFilters() sweep

`grep -rn "IgnoreQueryFilters" src/` re-run: the only production call sites are the 5 files already on `IgnoreQueryFiltersUsageTests.ApprovedFiles` (`DeviceTokenAuthenticationHandler.cs`, `SessionAuthenticationHandler.cs`, `BootstrapAdminSeeder.cs`, `AuthEndpoints.cs`, `DeviceRegistrationEndpoints.cs`) — the exact same set as before PLAN-0004 started. Zero new call sites from any PLAN-0004 milestone; every PLAN-0004 endpoint runs under an already-authenticated tenant/organisation context, so none of them ever needed the bootstrap escape hatch. `dotnet test --filter IgnoreQueryFiltersUsageTests` re-run green, confirming the guard test itself still passes with the unchanged allowlist. **Final allowlist: unchanged from PLAN-0003, 5 files, 0 additions.**

### Migration verification

All 12 migrations (unchanged count — no migration added this milestone) re-verified to apply cleanly in sequence from a completely empty database (disposable throwaway Postgres database `daxapos_migration_check_h`, then dropped — not the shared dev database).

### ADR-0016 acceptance

Per explicit human approval scoped to this closeout, and only after the four required confirmations:

1. **Read ADR-0016 in full.** 9 sections (five localisation types, UI localisation, business-data translation records, default language/fallback, receipt/tax label localisation, audit-log codes-not-sentences, MVP scope, non-goals, documentation/follow-up), Consequences, Alternatives Considered, Follow-Up Work — internally consistent, no contradictions found across the document.
2. **Nothing in Milestones A–G contradicts it.** Every translatable-in-future column (`TaxDefinitionTemplate.Name`/`ReceiptMarkerLabel`, `TaxDefinition.Name`/`ReceiptMarkerLabel`, `TaxCategory.Name`, `Product.Name`/`Description`, `ProductCategory.Name`, `ProductVariant.Name`, `ModifierGroup.Name`, `Modifier.Name`, `Menu.Name`, `MenuSection.Name`) is a plain invariant/fallback bounded `varchar`, confirmed at each milestone's own ADR-0016 re-check note above. No business logic anywhere string-matches on any of these `Name`/label fields for behaviour — matching is always on `Id`/`Code` (`TaxDefinitionTemplate.Code`, `TaxCategory.Code`, `Product.Sku`/`Barcode` as plain optional identifiers, never compared for branching). Zero `{Entity}Translation` tables exist. Every `AuditEvent` write uses a stable `EventType` string code (`$"{nameof(Entity)}{Action}"`) plus JSON `BeforeValue`/`AfterValue` snapshots — never a pre-rendered English sentence — satisfying §6 exactly, unchanged since PLAN-0003.
3. **Acceptance requires no schema/code work in this milestone** — confirmed directly from the ADR's own text ("no implementation required by acceptance, only formalizes constraints this plan already honours") and from the fact that nothing above required a single line of production code to change.
4. **Recorded the acceptance** in `docs/adr/index.md` (row moved from Proposed to Accepted table; Proposed table now empty), the ADR file's own Status field (`Proposed` → `Accepted`, Date field annotated with the acceptance date/context), this plan's ADR Gaps section and Human Decision #6, and this report.

`git mv docs/adr/proposed/ADR-0016-multi-language-and-localisation-strategy.md docs/adr/accepted/ADR-0016-multi-language-and-localisation-strategy.md`. Fixed the file's own internal sibling-ADR links (`ADR-0003`/`ADR-0006`/`ADR-0011`/`ADR-0015` are now same-directory neighbours, not `../accepted/`-prefixed). Updated every **living** doc that linked the old `proposed/` path: `docs/README.md`, `docs/03-phase-roadmap.md`, `docs/architecture/overview.md`, `docs/architecture/tax-engine.md`, `docs/modules/tax.md`, `docs/modules/catalog.md`, `docs/modules/receipts.md`, `docs/plans/active/PLAN-0006-terminal-display-pwa-planning.md`, `docs/plans/active/PLAN-localisation-multi-language.md` (the placeholder implementation plan itself — no longer "blocked on acceptance," still no worker assigned). **Deliberately left unchanged:** `docs/plans/active/PLAN-0003-worker-notes.md` and `docs/plans/active/PLAN-0003-identity-tenancy-locations-devices.md`'s own historical statements ("ADR-0016 stays proposed") and this plan's own per-milestone ADR-0016 check notes (A through G) above — these are point-in-time records of what was true when each was written, and rewriting them would falsify history rather than record it.

### Open issues reviewed

Per the plan's Open Issues Required section, three candidates named:

1. **Archive-and-replace concurrency race — filed as `OI-0017`.** Still a live, undecided risk exactly as accepted at planning approval (Human Decision #4) and reserved for Milestone H by Milestone D's own report. Written parallel to `OI-0013`'s precedent (same race shape: check-then-update with no serialisation), recommending the same fix mechanism (optimistic concurrency token) for consistency, flagging the one structural difference (`Product`'s version also *creates* a row, not just increments a counter).
2. **`VenueTaxConfiguration`-absence behaviour — not filed.** This was never actually an open question by the time Milestone H arrived: Human Decision #5 (approved 2026-07-03, before Milestone C even started) definitively resolved it as "404/explicit-error, never silent default," and Milestones F/G both implemented and tested that exact behaviour (`VenueTaxConfigurationEndpointsTests.Get_MissingConfiguration_ReturnsNotFound_InsteadOfSilentlyDefaulting`, `ResolvedMenuEndpointsTests.ResolvedMenu_FailsClosed_WhenVenueTaxConfigurationIsMissing`). Filing an open issue for an already-decided, already-implemented, already-tested behaviour would misrepresent it as unresolved.
3. **Menu merge-precedence revisit — not filed.** The plan's own wording was conditional: "if the human wants a different rule than 'location wins' once seen in practice." No real usage exists yet — `Order`/PLAN-0005 hasn't been built, so there is no "practice" to have surfaced a problem with the location-wins rule. Filing a speculative issue with no concrete trigger would be inventing a question rather than tracking a real one; if PLAN-0005 (or real tenant usage after launch) surfaces an actual problem with the precedence rule, that is the point to file it, with a concrete scenario attached.

`docs/issues/index.md` updated: new "Catalog / Data Integrity" area section for OI-0017; the Open Issues intro paragraph extended to record the count and reasoning for the two non-filed candidates.

### Docs updated

`docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md` (Status line, Milestone H status marker, ADR Gaps, Human Decision #6, Milestone H ADR-0016 acceptance note), this file (Milestone H Report), `docs/CHANGELOG.md` (new entry), `docs/adr/index.md`, `docs/adr/accepted/ADR-0016-...md`, `docs/issues/index.md`, `docs/issues/open/OI-0017-...md` (new), `docs/issues/open/OI-0016-...md` (updated — PLAN-0004 now a third finished-but-not-relocated plan), `docs/README.md`, `docs/03-phase-roadmap.md`, `docs/architecture/overview.md`, `docs/architecture/tax-engine.md`, `docs/modules/tax.md`, `docs/modules/catalog.md`, `docs/modules/receipts.md`, `docs/plans/active/PLAN-0006-terminal-display-pwa-planning.md`, `docs/plans/active/PLAN-localisation-multi-language.md`.

### Commands run

```
dotnet build DaxaPos.sln
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~RbacTests"
dotnet test tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj --filter "FullyQualifiedName~IgnoreQueryFiltersUsageTests"
dotnet test DaxaPos.sln
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "CREATE DATABASE daxapos_migration_check_h;"
dotnet ef database update ... --connection "...daxapos_migration_check_h..."   (clean-database migration re-verification, all 12)
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "DROP DATABASE daxapos_migration_check_h;"
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` — **944/944 passed** (104 unit tests + 840 API tests, up from 577 at Milestone G close — 367 new test executions from the RBAC theory expansion, zero regressions), against real Postgres, 0 failed, 0 skipped.
All 12 migrations verified to apply cleanly in sequence from an empty database.

### Deviations from the written plan (flagged, not silently made)

1. **Only 1 of the 3 candidate open issues named by the plan was actually filed** — see "Open issues reviewed" above. Filing all three regardless of whether they were still live questions would have misrepresented two already-decided/already-implemented behaviours as open.
2. **The cross-tenant "hijack" 404 test (`RbacTests.ValidSession_ForAnotherTenant_SeesNothing_AndNeverAnError`) was not extended to PLAN-0004 entities** — a deliberate scoping decision (see RBAC sweep section above), not an oversight; coverage exists per-entity in each milestone's own test file already.
3. **A stale doc comment was corrected in passing** (`RbacScenarioFixture.NoPermissionClient`'s "zero permission codes" claim, now inaccurate since Milestone A) — a one-line factual correction while already editing the file for the sweep, not scope creep.

None of these required backing out or redoing anything.

### PLAN-0004 completion

**PLAN-0004 is complete as of Milestone H (2026-07-05), in place under `docs/plans/active/`** — not moved to `docs/plans/completed/`, matching PLAN-0003's own precedent, since `OI-0016` (define completed-plan archival convention) remains open and unresolved. `OI-0016` updated to record that three plans (PLAN-0002, PLAN-0003, PLAN-0004) are now in this same finished-but-not-relocated state.

### Recommended next plan

**PLAN-0005 (Payments, Receipts, Printing)** is next in the plan sequence per `docs/README.md`'s Active Plans ordering and this plan's own Handoff Notes (written at the original planning pass): PLAN-0005's Order module is the first consumer of this plan's `TaxCalculationEngine`/`PriceResolver`/resolved-menu output, and is where ADR-0006's per-order 20-tax-component limit (deferred throughout PLAN-0004) finally gets enforced.

One heads-up for whoever starts PLAN-0005: read `TaxCalculationEngine`/`PriceResolver`/`ResolvedMenuEndpoints` as already-built dependencies to call, not to re-derive — the tax/pricing resolution logic this plan built is specifically designed to be reused, not reimplemented, by the Order module. `OI-0017` (archive-and-replace concurrency) is worth a look if PLAN-0005's order-entry traffic will read `Product` under real concurrent load for the first time.
