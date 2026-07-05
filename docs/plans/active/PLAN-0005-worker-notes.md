# PLAN-0005 Worker Notes — Planning Pass (2026-07-05)

## Session Purpose

Turn the architecture-level PLAN-0005 draft into an implementation-ready, milestone-by-milestone plan, following the exact process PLAN-0004's own planning pass used against PLAN-0003. No product code, migrations, or `src/` changes were made in this session — planning/documentation only.

## What Was Read

Full pass over: `ADR-0005` (payment provider adapter architecture), `ADR-0006` (tax-line based tax engine), `ADR-0010` (financial records ledger and audit), `ADR-0011` (receipt tax marker strategy), `ADR-0014` (inter-module communication), `ADR-0016` (multi-language and localisation strategy) — all accepted; `docs/modules/payments.md`, `refunds.md`, `receipts.md`, `printing.md`, `orders.md`, `audit.md`; `docs/architecture/payment-adapters.md`; `docs/issues/open/OI-0017-product-archive-and-replace-concurrency.md`; `PLAN-0004-catalog-menu-tax-pricing-planning.md` and `PLAN-0004-worker-notes.md` in full (all 8 milestone reports — the source of `TaxCalculationEngine`, `PriceResolver`, `VenueTaxConfiguration`, and the resolved-menu endpoint this plan builds on); `PLAN-0009-first-payment-adapter-stripe-terminal.md`; current source — `TaxCalculationEngine.cs`, `TaxCalculationModels.cs`, `PriceResolver.cs`, `PriceResolutionModels.cs`, `VenueTaxConfiguration.cs`, `Product.cs`, `ProductVariant.cs`, `Modifier.cs`, `ResolvedMenuEndpoints.cs`, `Permissions.cs`, `DaxaDbContext.cs`.

## What Was Produced

1. `docs/plans/active/PLAN-0005-payments-receipts-printing-planning.md` — rewritten from the original architecture-level draft into 6 named milestones (A–F) with concrete entity field lists, per-milestone migrations, endpoint lists, 3 new permission codes (with a `Permission.Category` classification), and a full commit sequence.
2. This file.

Not yet touched: `docs/adr/index.md`, `docs/issues/index.md` — no ADR or issue status changed (OI-0017 is discussed as a risk, not filed or closed by this pass).

## Design Decisions Worth Flagging to a Future Reader

- **No new `Modules.*`/`PaymentProviders.*` project.** The plan's original draft named `src/DaxaPos.Modules.Orders/`, `src/DaxaPos.Modules.Payments/`, `src/DaxaPos.PaymentProviders.*/`, none of which exist — `DaxaPos.sln` still has exactly five projects under `src/` (`Api`, `Application`, `Domain`, `Infrastructure`, `Persistence`). This follows PLAN-0004's own already-established precedent for catalog/tax/pricing/menu code rather than re-litigating it; the Files Likely To Change section was rewritten to point at the real five-project layout.
- **`DaxaPos.Workers` does not exist yet either**, despite being named in CLAUDE.md's suggested solution structure. Milestone E (printing) is the first milestone that actually needs it, because it is the first consumer of ADR-0014's Handler I/O Rule (see next point).
- **ADR-0006's 20-tax-component-per-order limit is finally enforced in this plan's Milestone A.** PLAN-0004 deliberately left it unenforced — it's an order-level aggregate across lines, and `Order` didn't exist until now. Flagged in Architecture Assumptions, Milestone A, and Tests To Run Later so it can't quietly fall through the gap between the two plans a second time.
- **Order lines snapshot `Product.Name`, `TaxCategory.Code`, and the resolved tax marker code/label at add-line time**, not a live join at receipt/read time — required by ADR-0010 (immutable financial source records) and ADR-0011 (historical receipts preserve the marker meaning in effect at time of sale), and is also what stops a later `Product` edit (including a future ADR-0016 translation) from retroactively changing a historical order/receipt.
- **Receipt/tax-summary label strings must be read from configuration, never hard-coded**, per ADR-0011 + ADR-0016 — MVP still only ships `en-AU` defaults (ADR-0016 §7), but the rendering code itself must not bake in string literals, so a later locale doesn't require touching `DaxaPos.Application.Receipts` code.
- **The printer service (Milestone E) is this codebase's first real consumer of ADR-0014's Handler I/O Rule.** ADR-0014 names printing explicitly as work that must go through an outbox → `DaxaPos.Workers` path rather than running inline in a request handler — no domain event handler in this plan may call a printer, a payment provider, or any other slow/external I/O directly. This plan is therefore also where the generic outbox/work-item mechanism (referenced but not yet built by ADR-0014's own Follow-Up Work) gets built for the first time, not invented as a printing-specific one-off.
- **`IPaymentTerminalProvider`'s interface is claimed by both this plan's Scope and PLAN-0009's Scope.** Resolved as a recommendation, not silently assumed: this plan's Milestone B defines the interface and DI resolution wiring; PLAN-0009 implements the first concrete adapter (Stripe Terminal) against it. PLAN-0009's own Handoff Notes already treat this plan as upstream ("this plan's non-adapter work") — see Human Decisions Needed #1.
- **Integrated payment amounts are structurally prevented from manual staff entry**, not just prohibited by convention — Milestone B's request DTO for the integrated-payment path has no staff-supplied-amount field once PLAN-0009 lands, mirroring how `ProductSoldOutEndpoints`' `SetSoldOutRequest` has no `PriceOverride` field.
- **OI-0017 (Product archive-and-replace concurrency race) is flagged, not fixed, in this plan.** OI-0017's own recommendation names PLAN-0005 explicitly as "the next plan to read `Product` under real concurrent load from order-entry traffic," so it had to be addressed rather than silently ignored. The working recommendation is to continue accepting it for Milestone A (order-line add reads and snapshots `Product` by id; it doesn't itself race the way two concurrent `PATCH`es do) but to flag this plan as the trigger for scheduling the OI-0013/OI-0017 hardening pass — see Human Decisions Needed #3.
- **Refund permission is proposed as `AdminSensitive`, not `Operational`** (unlike order entry and cash/EFTPOS payment recording, which are proposed `Operational`/staff-PIN-eligible) — refunds are exactly the kind of override CLAUDE.md's pricing/discounts section says must be "permissioned and audited," matching OI-0007's precedent of reserving consequential overrides for manager-level or higher.

## Open Items Requiring the User's Explicit Sign-Off

See "Human Decisions Needed" in the plan itself — summarized: (1) confirm `IPaymentTerminalProvider`'s interface lands in this plan's Milestone B, not PLAN-0009; (2) confirm `Order.OrderNumber` uses a Postgres sequence per location rather than a computed max; (3) confirm OI-0017 stays an accepted risk for Milestone A specifically, with this plan flagged as the trigger for a future hardening pass; (4) confirm a single `payments.refund` code is sufficient for MVP rather than a threshold-based escalation; (5) confirm the three new permission codes and their categories (`orders.manage`/`payments.record` as `Operational`, `payments.refund` as `AdminSensitive`).

## Recommended Next Session (superseded — see Milestone A Report below)

1. ~~Human reviews and (dis)approves the plan's 5 Human Decisions Needed items~~ — done 2026-07-05, all 5 approved (three refined beyond the plan's own recommendation: firmer manager/admin-only refund floor, explicit receipt/printing permission codes added to the catalogue, explicit no-hardware-coupling scope boundary on the interface split).
2. ~~On approval, implement Milestone A first~~ — done, see report below.
3. Update this plan's Status section with milestone checkboxes as work proceeds — no more than 3 commits without a refresh, per CLAUDE.md's plan-refresh rule, exactly as PLAN-0003/PLAN-0004 did throughout their own milestones.

---

## Milestone A Report (2026-07-05)

Implemented per the plan using strict TDD for the one genuinely financial-logic unit (ADR-0006's 20-distinct-tax-component-per-order limit): wrote `OrderTaxAggregationTests.cs` first, confirmed RED via `dotnet build` (compile error — `DaxaPos.Application.Orders` namespace not found, the expected reason since neither existed yet), implemented the pure `OrderTaxAggregation` class, confirmed GREEN. Everything else (entities, EF configs, endpoints, integration tests) followed the established CRUD-endpoint convention from PLAN-0004 Milestones C–G (not TDD-first), matching how those milestones treated schema/endpoint code versus the pure calculation engines.

### Files changed

New:
- `src/DaxaPos.Domain/Enums/OrderStatus.cs`, `OrderLineStatus.cs`
- `src/DaxaPos.Domain/Entities/Order.cs`, `OrderLine.cs`, `OrderLineModifier.cs`, `OrderLineTax.cs`, `OrderNumberCounter.cs`
- `src/DaxaPos.Domain/Events/OrderLifecycleDomainEvent.cs`, `OrderLineChangedDomainEvent.cs`
- `src/DaxaPos.Application/Orders/OrderTaxAggregation.cs`
- `src/DaxaPos.Persistence/Configurations/OrderConfiguration.cs`, `OrderLineConfiguration.cs`, `OrderLineModifierConfiguration.cs`, `OrderLineTaxConfiguration.cs`, `OrderNumberCounterConfiguration.cs`
- `src/DaxaPos.Persistence/Migrations/20260705122929_AddOrderFoundation.cs` (+ `.Designer.cs`)
- `src/DaxaPos.Api/Endpoints/Orders/OrderEndpoints.cs`
- `tests/DaxaPos.UnitTests/Orders/OrderTaxAggregationTests.cs`
- `tests/DaxaPos.Api.Tests/OrderEndpointsTests.cs`

Modified:
- `src/DaxaPos.Persistence/DaxaDbContext.cs` — 5 new `DbSet`s, 5 new fail-closed query filters (`Order`, `OrderLine`, `OrderLineModifier`, `OrderLineTax`, `OrderNumberCounter` — all denormalized `TenantId`, none derive it via a join, matching every prior entity).
- `src/DaxaPos.Application/Identity/Permissions.cs` — added `OrdersManage` constant.
- `src/DaxaPos.Persistence/Seed/RbacSeedIds.cs`, `PermissionConfiguration.cs`, `RolePermissionConfiguration.cs` — new `orders.manage` permission (`Operational`), granted to `SystemAdmin`/`OrganisationOwner`/`VenueManager`/`Staff` (first plan whose write endpoints are staff-accessible from day one).
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` — 2 new handler classes (`OrderLifecycleAuditHandler`, `OrderLineChangedAuditHandler`), same `$"{EntityType}{Action}"` convention, both carrying dual `UserId`/`StaffMemberId`.
- `src/DaxaPos.Api/Program.cs` — 2 new `AddScoped<IDomainEventHandler<...>>` registrations, 1 new `app.MapOrderEndpoints()` call.
- `docs/modules/orders.md`, `docs/plans/active/PLAN-0005-payments-receipts-printing-planning.md` (Status line + Milestone A status marker), this file.

No payments, refunds, receipts, or printing were added — Milestone A is order/order-line/tax-snapshot foundation only, exactly as scoped.

### Migration created

`20260705122929_AddOrderFoundation` — creates `orders`, `order_lines`, `order_line_modifiers`, `order_line_taxes` (all four `TenantId`-indexed + fail-closed query filters, no bootstrap `IgnoreQueryFilters()` caller needed — every endpoint runs under an already-authenticated tenant/org context), and `order_number_counters` (`LocationId` primary key, `TenantId`-indexed). A unique index on `(LocationId, OrderNumber)` enforces the location-scoped display sequence at the database level as a second line of defense behind the atomic counter allocation. Verified to apply cleanly in sequence from an empty database (all 13 migrations, disposable throwaway Postgres database, then dropped — not the shared dev database, which was migrated separately for the working tree).

### Deviations from the written plan (flagged, not silently made)

1. **`TenantId` added to `OrderLine`, `OrderLineModifier`, and `OrderLineTax`**, though the plan's per-entity field lists for these three don't list it — only `Order` does. The plan's own Milestone A migration bullet ("all four tables, `TenantId`-indexed + fail-closed query filters on all four") requires it, and every other tenant-owned, no-`OrganisationId`, derived-through-parent entity in the codebase (`ProductVariant`, `Modifier`, `MenuSection`, `MenuSectionItem`) carries its own denormalized `TenantId` rather than deriving it via a join — this follows that established, load-bearing precedent (`DaxaDbContext`'s query filters are always a single indexed comparison, never a join) rather than leaving a contradiction between the plan's entity-field prose and its own migration bullet unresolved.
2. **`Order.IsTaxInclusivePricing` is fail-closed-checked and snapshotted at *open* time, not deferred to first line-add.** The plan's Architecture Assumptions describe the snapshot happening "at open time" but its explicit fail-closed `VenueTaxConfiguration`-missing bullet appears only under the line-add-flow description. Resolved by doing both: `VenueTaxConfiguration` is looked up and required at order-open (matching the entity field's own "at open time" language — there is nothing else for `IsTaxInclusivePricing` to be snapshotted from), and looked up again at every line-add (matching the line-add flow's own explicit bullet, and defensive against a future deletion path that doesn't exist yet, but keeps the endpoint correct if one is ever added without needing a second look at this code).
3. **`OrderTaxAggregation.CountDistinctTaxComponents` counts distinct `TaxDefinitionId`s across an order's active lines, not `OrderLineTax` row count** — not spelled out by the plan's Milestone A bullet, but required by ADR-0006's own framing of the limit as "components" (i.e. how many different tax rates/rules the order has to reconcile), proven by a dedicated test (`CountDistinctComponents_SameTaxDefinitionAcrossMultipleLines_CountsOnce`) so a future reader doesn't assume it's a naive row count.
4. **Tax category resolution precedence (location-specific wins entirely over organisation-wide for the same `TaxCategory`) is new logic this milestone had to write** — the plan's Architecture Assumptions note `TaxCategoryDefinition` supports this via its nullable `LocationId`, but no resolution helper existed before this milestone (PLAN-0004 built the config CRUD, not a runtime resolver). Implemented as an "all region-specific rows or all org-wide rows, never mixed" precedence, explicitly modelled on the resolved-menu endpoint's approved Human Decision #7 merge rule rather than inventing a new merge shape.
5. **Line-level charged-total math (`LineTotalAmount`/`LineSubtotalAmount`) required deriving a formula the plan doesn't spell out**: inclusive-component tax is already embedded in the pre-tax line amount (nothing extra to charge); exclusive-component tax adds on top. Implemented as `LineTotalAmount = lineAmountBeforeTax + Σ(TaxAmount for exclusive components)`, `LineSubtotalAmount = LineTotalAmount - Σ(all TaxAmount)` — verified against the CLAUDE.md/ADR-0006 AU worked example byte-for-byte ($5.50 + $8.80 + $6.00 → $20.30 total, $1.30 GST) in `AddLine_AuMixedBasket_MatchesClaudeMdWorkedExample`.
6. **Order void/cancel accept an optional `reason` via query string, not a JSON body.** The plan doesn't specify the transport; a nullable-body-on-POST risks an empty-request-body deserialization failure that has no precedent elsewhere in the codebase (every existing deactivate/reactivate-style action takes no body at all), so `reason` was bound the same way `VoidLineAsync`'s reason is — query string — rather than introducing a new, untested body-binding pattern.
7. **No dedicated modifier/variant integration test** in `OrderEndpointsTests.cs`, though the add-line code path validates both. The plan's Tests bullet lists tax aggregation, the 20-component boundary, the CRUD/state-machine matrix, RBAC, cross-tenant/org 404s, and the missing-`VenueTaxConfiguration` fail-closed case explicitly — it does not call for modifier/variant coverage, and CLAUDE.md's "don't test what wasn't asked" balance was applied here rather than expanding scope. Flagged so a future milestone (or a follow-up hardening pass) knows this is an intentional gap, not an oversight.

None of these required backing out or redoing anything — all were caught while writing the code (1, 4, 5) or while writing the tests (2, 3, 6).

### Tests added

5 new unit tests (`OrderTaxAggregationTests.cs`): distinct-component counting across lines with and without overlap, the exact-20/21-boundary pair (mirroring `TaxCalculationEngineTests`' exact-10/11 pattern for its sibling limit), and a determinism proof.

18 new integration tests (`OrderEndpointsTests.cs`): order-open happy path with snapshotted `IsTaxInclusivePricing` and sequential per-location `OrderNumber` allocation; a staff-PIN-succeeds proof (`Open_Succeeds_ForStaffPinSession` — the load-bearing proof that `orders.manage`'s `Operational` category actually admits a staff session, mirroring PLAN-0004's `catalog.sold-out-toggle` precedent); client-supplied-`TenantId` rejection; cross-organisation terminal rejection; missing-`VenueTaxConfiguration` fail-closed rejection; the full AU mixed-basket worked example end-to-end (byte-for-byte against CLAUDE.md's example); sold-out-product rejection; add-line-on-a-closed-order conflict; void-line total recomputation; hold/resume round-trip and hold-when-not-open conflict; void/cancel transitions; the 20-component-per-order boundary (20 lines succeed, the 21st distinct component is rejected); cross-tenant and cross-organisation read blocking; missing-permission 403 (via the `SupportAccess` seeded role, which does not carry `orders.manage`); and an audit-row test covering both `Order`- and `OrderLine`-entity events.

### Commands run

```
dotnet build tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj                      (RED: 1 compile error, expected symbol)
dotnet test tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj --filter "FullyQualifiedName~OrderTaxAggregationTests"   (GREEN: 5/5)
dotnet build DaxaPos.sln                                                            (0 warnings/errors)
dotnet ef migrations add AddOrderFoundation --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~OrderEndpointsTests"
dotnet test DaxaPos.sln
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "CREATE DATABASE daxapos_migration_check;"
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api --connection "...daxapos_migration_check..."   (clean-database migration re-verification, all 13)
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "DROP DATABASE daxapos_migration_check;"
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` — **967/967 passed** (109 unit tests + 858 API tests, up from 944 before this session — 23 new tests, zero regressions), against real Postgres, 0 failed, 0 skipped.
All 13 migrations verified to apply cleanly in sequence from an empty database.

### Scope boundary re-check (approved Human Decision #1)

No hardware coupling was introduced — `OrderEndpoints.cs`/`Order`/`OrderLine`/`OrderLineTax` contain no printer, payment-terminal, or provider-specific code of any kind. This milestone's only "adapter-shaped" surface is `OrderTaxAggregation`/`PriceResolver`/`TaxCalculationEngine` reuse, all pure domain logic. Nothing here anticipates or blocks PLAN-0009's hardware-adapter work.

### Blockers before Milestone B

None. `Order`/`OrderLine`/`OrderLineModifier`/`OrderLineTax` exist, are fully CRUD/state-machine-manageable via the API, tax-aggregated and limit-enforced; `dotnet build`/`dotnet test` are clean (967/967); migrations verified clean from empty. Milestone B (payment foundation: `Payment`, `PaymentLedgerEntry`, cash/manual-EFTPOS recording, `IPaymentTerminalProvider` interface + DI wiring) can start on request.

One heads-up for whoever starts Milestone B: it's the first milestone to reach back into this milestone's `Order` state machine (a fully-settled payment transitions `Order.Status` to `Completed` and sets `ClosedAtUtc`) — reuse `OrderEndpoints`' `LoadAuthorizedOrderAsync`/`RecomputeOrderTotalsAsync`-style helpers rather than re-deriving the context-provenance or totals logic, and note the established "save the mutation, then recompute via a fresh query, then save again" ordering this milestone had to fix once (`VoidLineAsync`'s original bug — recomputing before saving the triggering change reads stale data, since `RecomputeOrderTotalsAsync` is a real DB query, not a local in-memory filter).

---

## Milestone B Report (2026-07-05)

Implemented per the plan using strict TDD for the one genuinely financial-logic unit (the payment/order settlement rules): wrote `PaymentSettlementTests.cs` first, confirmed RED via `dotnet build` (compile error — `DaxaPos.Application.Payments` namespace not found, the expected reason since neither existed yet), implemented the pure `PaymentSettlement` class, confirmed GREEN. Everything else (entities, EF configs, endpoints, integration tests) followed the established CRUD-endpoint convention from Milestone A (not TDD-first).

### Files changed

New:
- `src/DaxaPos.Domain/Enums/PaymentMethod.cs`, `PaymentStatus.cs`
- `src/DaxaPos.Domain/Entities/Payment.cs`, `PaymentLedgerEntry.cs`
- `src/DaxaPos.Domain/Events/PaymentLifecycleDomainEvent.cs`
- `src/DaxaPos.Application/Payments/PaymentSettlement.cs`, `IPaymentTerminalProvider.cs`
- `src/DaxaPos.Persistence/Configurations/PaymentConfiguration.cs`, `PaymentLedgerEntryConfiguration.cs`
- `src/DaxaPos.Persistence/Migrations/20260705130818_AddPaymentFoundation.cs` (+ `.Designer.cs`)
- `src/DaxaPos.Api/Endpoints/Payments/PaymentEndpoints.cs`
- `tests/DaxaPos.UnitTests/Payments/PaymentSettlementTests.cs`
- `tests/DaxaPos.Api.Tests/PaymentEndpointsTests.cs`

Modified:
- `src/DaxaPos.Persistence/DaxaDbContext.cs` — 2 new `DbSet`s, 2 new fail-closed query filters (`Payment`, `PaymentLedgerEntry`).
- `src/DaxaPos.Application/Identity/Permissions.cs` — added `PaymentsRecord` constant.
- `src/DaxaPos.Persistence/Seed/RbacSeedIds.cs`, `PermissionConfiguration.cs`, `RolePermissionConfiguration.cs` — new `payments.record` permission (`Operational`), granted to the same four roles as `orders.manage` (`SystemAdmin`/`OrganisationOwner`/`VenueManager`/`Staff`).
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` — 1 new handler class (`PaymentLifecycleAuditHandler`), same `$"{EntityType}{Action}"` convention, dual `UserId`/`StaffMemberId`. The order-completion side effect of a fully-settling payment reuses the existing `OrderLifecycleAuditHandler` (a `"Completed"`-action `OrderLifecycleDomainEvent`) — no second handler needed for that part.
- `src/DaxaPos.Api/Program.cs` — 1 new `AddScoped<IDomainEventHandler<...>>` registration, 1 new `app.MapPaymentEndpoints()` call.
- `docs/modules/payments.md`, `docs/modules/orders.md` (the `Completed`-is-now-reachable line), `docs/plans/active/PLAN-0005-payments-receipts-printing-planning.md` (Status line + Milestone B status marker), this file.

No refunds, receipts, or printing were added — Milestone B is payment foundation only, exactly as scoped. No hardware/provider code: `IPaymentTerminalProvider` is interface + placeholder DTOs, never called by any endpoint.

### Migration created

`20260705130818_AddPaymentFoundation` — creates `payments` (`TenantId`/`OrderId`/`LocationId`-indexed, `IdempotencyKey` globally unique) and `payment_ledger_entries` (`TenantId`/`PaymentId`-indexed, `Metadata` as `jsonb`). Verified to apply cleanly in sequence from an empty database (all 14 migrations, disposable throwaway Postgres database, then dropped — not the shared dev database, which was migrated separately for the working tree).

### Deviations from the written plan (flagged, not silently made)

1. **`TenantId` added to `PaymentLedgerEntry`**, though the plan's field list for it doesn't include one — only `Payment` does. Same reasoning as Milestone A's identical deviation for `OrderLine`/`OrderLineModifier`/`OrderLineTax`: every tenant-owned, derived-through-parent entity in this codebase carries its own denormalized `TenantId` for `DaxaDbContext`'s fail-closed query filter, never a join.
2. **`PaymentLedgerEntry.Amount`, not `AmountAmount`.** The plan's literal field list reads "`AmountAmount`" — read as a typo (a doubled word), not a deliberate two-part name; no other entity in the codebase uses a repeated-word field name. Corrected rather than reproduced literally.
3. **`IPaymentTerminalProvider` has no DI registration**, though the plan's Milestone B bullet says "interface only, adapter resolution wiring (DI registration keyed by configured provider)." With zero concrete adapters to register, a "keyed by configured provider" DI registration would have nothing to key — there is no provider configuration concept yet, and building one now would be guessing PLAN-0009's requirements before that plan actually states them (CLAUDE.md: don't design for hypothetical future requirements). The interface and placeholder DTOs exist and compile; wiring is deferred to whichever plan adds the first concrete adapter.
4. **`Payment.Status` only ever reaches `Recorded` in this milestone** (`Created`/`Approved`/`Declined`/`Cancelled`/`TimedOut` are defined per the plan's literal enum list but never assigned by any code path) — cash and manual EFTPOS have no external system to produce an intermediate state from. This matches the plan's own description ("recorded immediately as `Recorded`") rather than being a gap; flagged so a future reader doesn't go looking for code that sets the other five values.
5. **Settlement equality (`AmountApproved == Order.GrandTotalAmount`, literal in the plan) is made safe by rejecting any payment that would push the running recorded total past the order's grand total** (400 Bad Request), rather than accepting an overpayment and reconciling change elsewhere. The plan doesn't say what happens above the total; not rejecting it would make the plan's own `==` check unreachable in the overpayment case (the sum would jump past the total without ever equalling it) and would leave the order permanently un-completable by this logic. Change-giving/tender-amount UI concerns are out of this milestone's scope (no terminal UI exists yet — PLAN-0006) and are not addressed here.
6. **Idempotency-key retry is checked before the order-open/held-state check**, not spelled out by the plan. A retry of the exact payment that already fully settled and closed the order must still return the existing payment (200 OK), not a spurious 409 — checking idempotency first means a retry is never affected by a state transition its own first attempt caused.
7. **No `GET` single-payment-by-id endpoint** — the plan's own endpoint list is exactly 2 (`POST`/`GET` list), and `Results.Created`'s `Location` header still points at a plausible (if unroutable) `/api/v1/orders/{orderId}/payments/{paymentId}` path, matching `ProductModifierGroupEndpoints`' identical precedent (an assign/list-only entity with no single-item `GET`).

None of these required backing out or redoing anything — all were caught while writing the code (1, 3, 4, 7) or while writing the tests (2, 5, 6).

### Tests added

7 new unit tests (`PaymentSettlementTests.cs`): exact-settlement boundary (does not exceed), over-the-total rejection, partial/split-payment-in-progress (does not exceed), a zero-grand-total edge case (any positive payment exceeds), the fully-settled/not-fully-settled pair, and a determinism proof — mirroring `OrderTaxAggregationTests`' style precisely.

11 new integration tests (`PaymentEndpointsTests.cs`): cash payment for the full amount settling and completing the order; a staff-PIN-succeeds proof (`RecordPayment_Succeeds_ForStaffPinSession` — the load-bearing proof that `payments.record`'s `Operational` category actually admits a staff session, mirroring Milestone A's `orders.manage` precedent, exercised end-to-end: staff opens the order, adds the line, and records the payment); a split payment across two calls that only completes the order on the second, fully-settling call; idempotency-key retry returning the same payment with no duplicate row; overpayment rejection; `Integrated`-method rejection (no adapter exists); payment-on-a-cancelled-order conflict; client-supplied-`TenantId` rejection; missing-permission 403 (via `SupportAccess`); cross-organisation list-blocking; and an audit-row test covering both the `Payment`-entity event and the `Order`-entity `"Completed"` event.

### Commands run

```
dotnet build tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj                      (RED: 1 compile error, expected symbol)
dotnet test tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj --filter "FullyQualifiedName~PaymentSettlementTests"   (GREEN: 7/7)
dotnet build DaxaPos.sln                                                            (0 warnings/errors)
dotnet ef migrations add AddPaymentFoundation --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~PaymentEndpointsTests"
dotnet test DaxaPos.sln
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "CREATE DATABASE daxapos_migration_check;"
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api --connection "...daxapos_migration_check..."   (clean-database migration re-verification, all 14)
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "DROP DATABASE daxapos_migration_check;"
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` — **985/985 passed** (116 unit tests + 869 API tests, up from 967 before this session — 18 new tests, zero regressions), against real Postgres, 0 failed, 0 skipped.
All 14 migrations verified to apply cleanly in sequence from an empty database. No new `IgnoreQueryFilters()` call sites.

### Scope boundary re-check (approved Human Decision #1)

No hardware coupling was introduced — `PaymentEndpoints.cs`/`Payment`/`PaymentLedgerEntry` contain no printer or provider-specific code. `IPaymentTerminalProvider` and its DTOs are pure abstractions with no HTTP client, no vendor SDK reference, and are never invoked by any endpoint in this milestone. Nothing here anticipates or blocks PLAN-0009's hardware-adapter work.

### OI-0017 status re-check

Not touched. Milestone B never reads `Product`/`ProductVariant` — its only entity interactions are `Order` (read/update) and its own new `Payment`/`PaymentLedgerEntry` tables. OI-0017 remains open, unaffected.

### Blockers before Milestone C

None. `Payment`/`PaymentLedgerEntry` exist, cash/manual EFTPOS recording works end-to-end including split payments and order settlement; `dotnet build`/`dotnet test` are clean (985/985); migrations verified clean from empty. Milestone C (refund service: `Refund` entity, full/partial refunds, `payments.refund` permission) can start on request.

One heads-up for whoever starts Milestone C: `payments.refund` was approved as manager/admin-only (`AdminSensitive`, `rejectStaffPin: true`) — a different category and staff-PIN posture than every permission this plan has added so far (`orders.manage`/`payments.record`, both `Operational`). Reuse the `Payment`/`Order` context-provenance helpers' shape but do not copy their `rejectStaffPin` default; refunds must reject staff-PIN sessions explicitly, matching PLAN-0004's `catalog.manage`/`pricing.manage`/`menus.manage` precedent, not this plan's own `orders.manage`/`payments.record` precedent.

---

## Milestone C Report (2026-07-06)

Implemented per the plan using strict TDD for the one genuinely financial-logic unit (the refund/payment settlement rule): wrote `RefundSettlementTests.cs` first, confirmed RED via `dotnet build` (compile error — `RefundSettlement` not found, the expected reason since it didn't exist yet), implemented the pure `RefundSettlement` class, confirmed GREEN. Everything else (entity, EF config, endpoints, integration tests) followed the established CRUD-endpoint convention from Milestones A/B (not TDD-first).

### Files changed

New:
- `src/DaxaPos.Domain/Enums/RefundStatus.cs`
- `src/DaxaPos.Domain/Entities/Refund.cs`
- `src/DaxaPos.Domain/Events/RefundLifecycleDomainEvent.cs`
- `src/DaxaPos.Application/Payments/RefundSettlement.cs`
- `src/DaxaPos.Persistence/Configurations/RefundConfiguration.cs`
- `src/DaxaPos.Persistence/Migrations/20260705135628_AddRefundFoundation.cs` (+ `.Designer.cs`)
- `src/DaxaPos.Api/Endpoints/Refunds/RefundEndpoints.cs`
- `tests/DaxaPos.UnitTests/Payments/RefundSettlementTests.cs`
- `tests/DaxaPos.Api.Tests/RefundEndpointsTests.cs`

Modified:
- `src/DaxaPos.Persistence/DaxaDbContext.cs` — 1 new `DbSet`, 1 new fail-closed query filter (`Refund`).
- `src/DaxaPos.Application/Identity/Permissions.cs` — added `PaymentsRefund` constant.
- `src/DaxaPos.Persistence/Seed/RbacSeedIds.cs`, `PermissionConfiguration.cs`, `RolePermissionConfiguration.cs` — new `payments.refund` permission (`AdminSensitive`), granted to `SystemAdmin`/`OrganisationOwner`/`VenueManager` only — deliberately absent from `Staff`'s and `SupportAccess`'s grant lists (approved Human Decision #4: manager/admin-only by default).
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` — 1 new handler class (`RefundLifecycleAuditHandler`), same `$"{EntityType}{Action}"` convention as every prior handler.
- `src/DaxaPos.Api/Program.cs` — 1 new `AddScoped<IDomainEventHandler<...>>` registration, 1 new `app.MapRefundEndpoints()` call.
- `docs/modules/refunds.md`, `docs/modules/payments.md` (short refund cross-reference addendum — no change to `docs/modules/orders.md`, since refunds do not touch `Order` at all), `docs/plans/active/PLAN-0005-payments-receipts-printing-planning.md` (Status line + Milestone C status marker), this file, `docs/CHANGELOG.md`.

No receipts, printing, or UI were added — Milestone C is refund-service foundation only, exactly as scoped. No hardware/provider code: `Refund.Status`'s `ProviderPending`/`ProviderConfirmed` values are defined but never assigned by any code path (PLAN-0009's scope).

### Migration created

`20260705135628_AddRefundFoundation` — creates `refunds` (`TenantId`/`PaymentId`/`OrderId`-indexed, no `IdempotencyKey` — the plan's Milestone C entity field list does not include one for `Refund`, unlike `Payment`). Verified to apply cleanly in sequence from an empty database (all 15 migrations, disposable throwaway Postgres database, then dropped — not the shared dev database, which was migrated separately for the working tree).

### Deviations from the written plan (flagged, not silently made)

1. **No new ledger table for refunds**, though ADR-0010 states "payment, refund, gift card, and store credit activity is ledgered — every movement has a signed record" and Milestone B added `PaymentLedgerEntry` for the analogous payment case. The plan's own Milestone C "Entities/tables" bullet lists only `Refund` (new) — no ledger table — and its Tests bullet asks for "refund audit-row assertions," not "refund ledger entry assertions." Resolved by treating the `Refund` row itself as the append-only record (its `Status` only ever reaches `Recorded` in this milestone, exactly like `Payment` in Milestone B) plus `RefundLifecycleAuditHandler`'s audit row (carrying who/when/reason/linked order+payment ids in `AfterValue` JSON) as jointly satisfying ADR-0010's ledger/audit requirement for now. A `RefundLedgerEntry` table can be added alongside PLAN-0009's adapter refund path if/when `Refund.Status` actually starts transitioning through `ProviderPending`/`ProviderConfirmed`, mirroring why `PaymentLedgerEntry` exists ahead of `Payment.Status` actually needing it.
2. **`ReasonCode` is a free-form `string`, not a closed enum.** The plan names the field but does not enumerate a fixed set of reason values (unlike, say, `RefundStatus`, which the plan's prose does enumerate). Modelled on `TaxCategory.Code`'s existing precedent (a caller-supplied string code) rather than guessing an incomplete enum a future milestone would need to rework.
3. **`payments.refund`'s staff-PIN rejection could not be proven the way the worker notes originally suggested** (assign a role that carries `payments.refund` to a real staff member and show the refund call still 403s). The staff-PIN login endpoint (`AuthEndpoints.StaffPinLoginAsync`) already has its own defense-in-depth check that rejects login itself (401, reason `RoleGrantsSensitivePermissions`) for any role granting an `AdminSensitive` permission — so a role carrying `payments.refund` can never complete staff-PIN login in the first place, discovered while writing `RefundEndpointsTests.RecordRefund_Rejects_ForRealStaffPinSession` (first attempt 401'd on login, not 403 on refund). Resolved by assigning the plain `Staff` role instead (which carries none of the `AdminSensitive` codes) and proving the realistic case: a legitimate staff-PIN session is still rejected 403 by `RequirePermissionFilter`'s `rejectStaffPin` gate when it attempts a refund. This is arguably a *stronger* proof of the security posture (two independent layers reject an admin-sensitive action from a staff-PIN context) — flagged here so a future reader doesn't try the role-with-the-permission approach again and hit the same dead end.
4. **A payment must be in `PaymentStatus.Recorded` to be refunded** (409 otherwise) — not spelled out by the plan's Milestone C bullet, but a sensible safety net once PLAN-0009 introduces other `Payment.Status` values (e.g. `Declined`/`Cancelled`), mirroring the same defensive-filtering pattern `PaymentSettlement`'s sum query already applies (`.Where(p => p.Status == PaymentStatus.Recorded)`).

None of these required backing out or redoing anything — 1 and 2 were caught while writing the code, 3 was caught while writing the tests, 4 was a small addition made alongside the endpoint's other validation.

### Tests added

5 new unit tests (`RefundSettlementTests.cs`): exact full-refund-completes boundary, over-the-approved-amount rejection, a partial-refund-in-progress case, a fully-refunded-already edge case (any further positive refund exceeds), and a determinism proof — mirroring `PaymentSettlementTests`' style precisely.

11 new integration tests (`RefundEndpointsTests.cs`): full refund succeeds and does not mutate the original payment; two partial refunds summing exactly to the payment total both succeed; over-refund rejection; a second partial refund that would push the running total past the approved amount is rejected; client-supplied-`TenantId` rejection; missing-`ReasonCode` rejection; missing-permission 403 (via the `Staff` role, which does not carry `payments.refund`); the real staff-PIN-session rejection proof described in Deviation 3 above; refund against a non-existent payment 404; cross-organisation list-blocking; and an audit-row test asserting `EventType == "RefundRecorded"` and that `AfterValue` contains the linked `PaymentId`/`OrderId`/`ReasonCode`.

### Commands run

```
dotnet build tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj                      (RED: 6 compile errors, expected symbol)
dotnet test tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj --filter "FullyQualifiedName~RefundSettlementTests"   (GREEN: 5/5)
dotnet build DaxaPos.sln                                                            (0 warnings/errors)
dotnet ef migrations add AddRefundFoundation --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~RefundEndpointsTests"   (11/11, after fixing Deviation 3's first-attempt failure)
dotnet test DaxaPos.sln
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "CREATE DATABASE daxapos_migration_check;"
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api --connection "...daxapos_migration_check..."   (clean-database migration re-verification, all 15)
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "DROP DATABASE daxapos_migration_check;"
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` — **1001/1001 passed** (121 unit tests + 880 API tests, up from 985 before this session — 16 new tests, zero regressions), against real Postgres, 0 failed, 0 skipped.
All 15 migrations verified to apply cleanly in sequence from an empty database. No new `IgnoreQueryFilters()` call sites outside the established test-only audit-row-assertion pattern (matching `PaymentEndpointsTests`' identical usage).

### Scope boundary re-check (approved Human Decision #1)

No hardware coupling was introduced — `RefundEndpoints.cs`/`Refund` contain no printer, payment-terminal, or provider-specific code of any kind. `RefundStatus.ProviderPending`/`ProviderConfirmed` are defined per the plan's own field description but never assigned by any code path. Nothing here anticipates or blocks PLAN-0009's hardware-adapter work.

### OI-0017 status re-check

Not touched. Milestone C never reads `Product`/`ProductVariant` — its only entity interactions are `Payment` (read), `Order` (read, for organisation/location scoping only), and its own new `Refund` table. OI-0017 remains open, unaffected.

### Blockers before Milestone D

None. `Refund` exists, full/partial refunds work end-to-end with server-side over-refund rejection and manager/admin-only RBAC; `dotnet build`/`dotnet test` are clean (1001/1001); migrations verified clean from empty. Milestone D (receipt generation: pure `ReceiptDocument` rendering model, GST-free marker, tax summary, refund-receipt linking) can start on request.

One heads-up for whoever starts Milestone D: receipt rendering reads `Order`/`OrderLine`/`OrderLineTax` snapshots (Milestone A) and `Payment`/`Refund` rows (Milestones B/C) but must not recompute tax or price from either — it is a pure projection over already-immutable source data, per ADR-0010's "PDF Generation Strategy" pattern. `Refund.ReasonCode`/`ReasonNote` and `Payment.Method`/`ProviderReference` are the fields a refund receipt will need to surface; no new query/join mechanism is required beyond what Milestones A–C already store.

---

## Milestone D Report (2026-07-06)

Implemented per the plan using strict TDD for the milestone's one genuinely pure-logic unit (the receipt rendering projection): wrote `ReceiptRendererTests.cs` first (9 tests covering the CLAUDE.md/ADR-0006 AU mixed-basket worked example, tax-summary aggregation, voided-line exclusion, marker-legend dedup/absence, payment/refund summary, label-set configurability, and determinism), confirmed RED via `dotnet build` (2 compile errors — `DaxaPos.Application.Receipts` namespace and `ReceiptOrderInput`/`ReceiptLineInput` types not found, the expected reason since neither existed yet), implemented `ReceiptModels.cs`/`ReceiptRenderer.cs`, confirmed GREEN (9/9). Endpoint code (`ReceiptEndpoints.cs`, permission/RBAC wiring, integration tests) followed the established CRUD-endpoint convention from Milestones A–C (not TDD-first).

### Files changed

New:
- `src/DaxaPos.Application/Receipts/ReceiptModels.cs` (`ReceiptLineTaxInput`, `ReceiptLineInput`, `ReceiptPaymentInput`, `ReceiptRefundInput`, `ReceiptOrderInput`, `ReceiptLabelSet`, `ReceiptLineItemLine`, `ReceiptTaxSummaryEntry`, `ReceiptPaymentSummaryLine`, `ReceiptRefundSummaryLine`, `ReceiptDocument`), `ReceiptRenderer.cs`.
- `src/DaxaPos.Domain/Events/ReceiptReprintedDomainEvent.cs`.
- `src/DaxaPos.Api/Endpoints/Receipts/ReceiptEndpoints.cs`.
- `src/DaxaPos.Persistence/Migrations/20260705234451_AddReceiptsReprintPermission.cs` (+ `.Designer.cs`) — a pure permission-catalogue seed migration, no new table (mirrors PLAN-0004's OI-0015-era permission-only migrations).
- `tests/DaxaPos.UnitTests/Receipts/ReceiptRendererTests.cs` (9 tests, TDD'd first).
- `tests/DaxaPos.Api.Tests/ReceiptEndpointsTests.cs` (7 tests).

Modified:
- `src/DaxaPos.Application/Identity/Permissions.cs` — added `ReceiptsReprint` constant.
- `src/DaxaPos.Persistence/Seed/RbacSeedIds.cs`, `Configurations/PermissionConfiguration.cs`, `RolePermissionConfiguration.cs` — new `receipts.reprint` permission (`Operational`), granted to `SystemAdmin`/`OrganisationOwner`/`VenueManager`/`Staff` (same grant set as `orders.manage`/`payments.record` — reprinting is routine counter work, not manager-only like `payments.refund`).
- `src/DaxaPos.Api/Audit/DomainEventAuditHandlers.cs` — 1 new handler class (`ReceiptReprintedAuditHandler`), `EventType = "ReceiptReprinted"`, `EntityType = "Receipt"` (no dedicated table — the string names the domain event, not a real entity), `EntityId = OrderId`.
- `src/DaxaPos.Api/Program.cs` — 1 new `AddScoped<IDomainEventHandler<...>>` registration, 1 new `app.MapReceiptEndpoints()` call, 1 new `using` for the `Receipts` endpoint namespace.
- `docs/modules/receipts.md`, `docs/plans/active/PLAN-0005-payments-receipts-printing-planning.md` (Status line + Milestone D status marker), this file, `docs/CHANGELOG.md`.

No printing, PDF generation, or UI were added — Milestone D is a pure rendering-model and viewing/reprint-endpoint foundation only, exactly as scoped. No hardware/provider code: `ReceiptDocument` carries no ESC/POS, printer, or transport-specific type of any kind (approved Human Decision #1's scope boundary re-checked below).

### Migration created

`20260705234451_AddReceiptsReprintPermission` — inserts the `receipts.reprint` permission row and 4 `role_permissions` rows (`SystemAdmin`/`OrganisationOwner`/`VenueManager`/`Staff`). No new table — receipts are rendered from `Order`/`OrderLine`/`OrderLineTax`/`Payment`/`Refund` at request time, never persisted as their own row, per the plan's own "Entities/tables: none new" scoping and ADR-0010's "PDF Generation Strategy" pattern. Verified to apply cleanly in sequence from an empty database (all 16 migrations, disposable throwaway Postgres database, then dropped — not the shared dev database, which was migrated separately for the working tree).

### Deviations from the written plan (flagged, not silently made)

1. **No separate "refund receipt" shape.** The plan's Milestone D bullet says "Refund receipts link to the original order/payment (`Refund.PaymentId`/`OrderId`), per ADR-0010" without specifying whether that means a distinct rendering path. Resolved by giving `ReceiptDocument` a single `Payments`/`Refunds` section pair covering the whole order — a `Refund`'s `PaymentId` already places it next to the payment it reverses in the same document, satisfying the linking requirement without a second, near-duplicate `ReceiptDocument`-for-refunds type.
2. **`ReceiptLabelSet` is a plain in-memory record with one hard-coded `Default` instance, not a location-settings-backed lookup.** The plan's Architecture Assumptions require labels to be "read from configuration, not hard-coded... MVP still ships only en-AU defaults" — no location-level receipt-label settings table exists yet (`VenueTaxConfiguration` doesn't carry one). Resolved by making `ReceiptLabelSet` a constructor parameter to `ReceiptRenderer.Render` rather than an internal constant, so the *rendering code* never bakes in a string literal — `ReceiptEndpoints.cs` is the only place `ReceiptLabelSet.Default` is referenced, and swapping in a real per-location lookup later is a one-line change there, not a `ReceiptRenderer` rewrite.
3. **`receipts.reprint` was finalized as `Operational` (not left open), matching the plan's own "(proposed)" table entry rather than reopening the category question.** The plan's Human Decision #5 approval record already proposed `Operational` for `receipts.reprint` "to be confirmed at Milestone D start" — confirmed as originally proposed since reprinting a receipt is the same class of routine counter work as `orders.manage`/`payments.record`, not a manager-only override like `payments.refund`.
4. **Live-sale receipt viewing (`GET .../receipt`) reuses `orders.manage` rather than introducing a third receipt-viewing permission code.** The plan's own Milestone D bullet says "No new permission code — receipt viewing/printing during a live sale is part of `orders.manage`/`payments.record`'s existing surface" — implemented exactly as stated; only the standalone reprint action got `receipts.reprint`.
5. **`GetAsync`/`ReprintAsync` share a private `BuildReceiptDocumentAsync` helper that issues 4 queries (lines, tax rows grouped by line, payments, refunds keyed by payment id) rather than a single joined query.** Not specified by the plan; chosen to keep each query shape simple and mirror `OrderEndpoints`' own multi-query line/modifier/tax loading pattern rather than a novel single mega-join specific to receipts.

None of these required backing out or redoing anything — all were caught while writing the code (2, 5) or while approving the permission category against the plan's own pre-recorded proposal (3, 4).

### Tests added

9 new unit tests (`ReceiptRendererTests.cs`): the AU mixed-basket worked example byte-for-byte ($5.50 + $8.80 + $6.00 → $20.30 total, $1.30 GST, `F = GST-free` legend), tax-summary aggregation across lines sharing a tax name, voided-line exclusion, marker-legend absence when no marker is configured, marker-legend dedup when multiple lines share one marker, payment-summary inclusion, refund-summary linking to its original payment, custom-`ReceiptLabelSet` proof (labels are not hard-coded), and a determinism proof (comparing scalars/sequences explicitly, since `ReceiptDocument`'s list-typed properties use reference equality under record-synthesized `Equals`).

7 new integration tests (`ReceiptEndpointsTests.cs`): the same AU mixed-basket worked example end-to-end through `GET .../receipt`; payment-and-linked-refund summary inclusion; 404 for a non-existent order; cross-organisation 404; a staff-PIN-succeeds proof for reprint (`Reprint_Succeeds_ForStaffPinSession` — the load-bearing proof that `receipts.reprint`'s `Operational` category actually admits a staff session, mirroring `orders.manage`/`payments.record`'s precedent); a missing-permission 403 proof (via `SupportAccess`, which carries neither `orders.manage` nor `receipts.reprint`); and a reprint audit-row test asserting `EventType == "ReceiptReprinted"`/`EntityType == "Receipt"`/`EntityId == order.Id`.

### Commands run

```
dotnet build tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj                        (RED: 2 compile errors, expected symbols)
dotnet test tests/DaxaPos.UnitTests/DaxaPos.UnitTests.csproj --filter "FullyQualifiedName~ReceiptRendererTests"   (GREEN: 9/9)
dotnet build DaxaPos.sln                                                              (0 warnings/errors)
dotnet ef migrations add AddReceiptsReprintPermission --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api
dotnet test tests/DaxaPos.Api.Tests/DaxaPos.Api.Tests.csproj --filter "FullyQualifiedName~ReceiptEndpointsTests"   (7/7, after fixing a first-attempt staff-PIN role-assignment gap)
dotnet test DaxaPos.sln
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "CREATE DATABASE daxapos_migration_check;"
dotnet ef database update --project src/DaxaPos.Persistence --startup-project src/DaxaPos.Api --connection "...daxapos_migration_check..."   (clean-database migration re-verification, all 16)
docker exec deploy-db-1 psql -U daxapos -d daxapos -c "DROP DATABASE daxapos_migration_check;"
```

### Build/test result

`dotnet build DaxaPos.sln` — 0 warnings, 0 errors.
`dotnet test DaxaPos.sln` — **1017/1017 passed** (130 unit tests + 887 API tests, up from 1001 before this session — 16 new tests, zero regressions), against real Postgres, 0 failed, 0 skipped.
All 16 migrations verified to apply cleanly in sequence from an empty database. No new `IgnoreQueryFilters()` call sites outside the established test-only audit-row-assertion pattern (`ReceiptEndpointsTests.cs`'s reprint audit-row assertion matches `RefundEndpointsTests`/`PaymentEndpointsTests`' identical usage).

### Scope boundary re-check (approved Human Decision #1)

No hardware coupling was introduced — `ReceiptEndpoints.cs`/`ReceiptDocument`/`ReceiptRenderer` contain no printer, ESC/POS, PDF, or provider-specific code of any kind. The renderer's output is a plain, print-transport-agnostic structured model, exactly the shape Milestone E's ESC/POS generation is expected to consume as input without `ReceiptRenderer` itself changing.

### OI-0017 status re-check

Not touched. Milestone D never reads `Product`/`ProductVariant` — its only entity interactions are `Order`/`OrderLine`/`OrderLineTax` (read), `Payment`/`Refund` (read). OI-0017 remains open, unaffected.

### Blockers before Milestone E

None. `ReceiptRenderer`/`ReceiptDocument` exist and render correctly from immutable order/payment/refund snapshots; `GET`/`POST reprint` endpoints work end-to-end with correct RBAC and reprint auditing; `dotnet build`/`dotnet test` are clean (1017/1017); migrations verified clean from empty. Milestone E (ESC/POS printing and the outbox mechanism) can start on request — **do not start it in this session**, per this session's explicit scope boundary.

One heads-up for whoever starts Milestone E: `ReceiptDocument` (this milestone's output) is the input Milestone E's ESC/POS command generation should consume — it is already print-transport-agnostic (no printer/ESC/POS type anywhere in `DaxaPos.Application.Receipts`). The generic outbox/work-item table and `DaxaPos.Workers` project do not exist yet; per ADR-0014's Handler I/O Rule and this plan's own Architecture Assumptions, sending a receipt to a printer must go through that outbox → worker path, never inline from a request handler — building it is Milestone E's first task, not a prerequisite this milestone already satisfied.
