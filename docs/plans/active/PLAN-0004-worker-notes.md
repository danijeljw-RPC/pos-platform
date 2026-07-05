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
