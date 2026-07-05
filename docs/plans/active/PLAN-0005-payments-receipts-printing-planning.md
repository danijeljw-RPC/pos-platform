# PLAN-0005 — Payments, Receipts, and Printing

## Status

Milestones A, B, and C done (2026-07-05, 2026-07-05, 2026-07-06) — see each milestone's status note below. Milestones D–F not yet started.

## Goal

Implement the payment service (cash, manual EFTPOS, integrated payment adapter foundation), refund service, receipt generation, and receipt printing. This is the core commercial transaction layer.

## Scope

- Order service (create order, add/remove lines, modifiers, notes).
- Cash payment.
- Manual external EFTPOS payment.
- Payment adapter interface (`IPaymentTerminalProvider`).
- Payment ledger.
- Refund service (full and partial).
- Receipt generation (thermal, tax invoice).
- ESC/POS receipt printing.
- Receipt tax marker (`F = GST-free`).
- Audit logging for all payment and refund activity.

## Non-goals

- Full integrated payment provider implementations — the first integration (Stripe Terminal) is [PLAN-0009](PLAN-0009-first-payment-adapter-stripe-terminal.md).
- Kitchen/bar docket routing (PLAN-0006 onwards).
- Gift cards and store credit.
- Split bills.

## Context Read

- `docs/adr/accepted/ADR-0005-payment-provider-adapter-architecture.md`
- `docs/adr/accepted/ADR-0006-tax-line-based-tax-engine.md`
- `docs/adr/accepted/ADR-0010-financial-records-ledger-and-audit.md`
- `docs/adr/accepted/ADR-0011-receipt-tax-marker-strategy.md`
- `docs/adr/accepted/ADR-0014-inter-module-communication.md`
- `docs/adr/accepted/ADR-0016-multi-language-and-localisation-strategy.md`
- `docs/modules/payments.md`, `refunds.md`, `receipts.md`, `printing.md`, `orders.md`, `audit.md`
- `docs/architecture/payment-adapters.md`
- `docs/issues/open/OI-0017-product-archive-and-replace-concurrency.md`
- `docs/plans/active/PLAN-0004-catalog-menu-tax-pricing-planning.md` and `PLAN-0004-worker-notes.md` (all 8 milestone reports — this is where `TaxCalculationEngine`, `PriceResolver`, `VenueTaxConfiguration`, and the resolved-menu endpoint are defined; this plan reuses them, it does not re-derive tax/pricing rules)
- `docs/plans/active/PLAN-0009-first-payment-adapter-stripe-terminal.md`
- Current source: `src/DaxaPos.Application/Tax/TaxCalculationEngine.cs`, `TaxCalculationModels.cs`; `src/DaxaPos.Application/Pricing/PriceResolver.cs`, `PriceResolutionModels.cs`; `src/DaxaPos.Domain/Entities/VenueTaxConfiguration.cs`, `Product.cs`, `ProductVariant.cs`, `Modifier.cs`; `src/DaxaPos.Api/Endpoints/Menus/ResolvedMenuEndpoints.cs`; `src/DaxaPos.Application/Identity/Permissions.cs`; `src/DaxaPos.Persistence/DaxaDbContext.cs`.

## Files Likely To Change

**Correction (2026-07-05 planning pass):** the paths below replace the plan's original draft, which named `src/DaxaPos.Modules.*`/`src/DaxaPos.PaymentProviders.*` projects. Those projects do not exist. PLAN-0004's own planning pass already made and recorded this call for catalog/tax/pricing/menu code ("No new `Modules.*` project... code lives directly in the five existing layered projects") — this plan follows the same, now-established precedent rather than re-litigating it. `DaxaPos.sln` currently has exactly five projects under `src/`: `Api`, `Application`, `Domain`, `Infrastructure`, `Persistence`.

```
src/DaxaPos.Domain/Entities/            (Order, OrderLine, OrderLineModifier, OrderLineTax, Payment, PaymentLedgerEntry, Refund, RefundLine, ...)
src/DaxaPos.Domain/Enums/               (OrderStatus, OrderLineStatus, PaymentMethod, PaymentStatus, RefundReasonCode, ...)
src/DaxaPos.Domain/Events/              (lifecycle/changed domain events, matching PLAN-0004's per-entity convention)
src/DaxaPos.Application/Orders/         (order state machine, order line/tax aggregation — new folder, mirrors Application/Tax, Application/Pricing)
src/DaxaPos.Application/Payments/       (IPaymentTerminalProvider interface, payment ledger service)
src/DaxaPos.Application/Receipts/       (receipt rendering model — pure, no DB dependency, mirrors TaxCalculationEngine's shape)
src/DaxaPos.Infrastructure/Printing/    (ESC/POS command generation, printer transport)
src/DaxaPos.Infrastructure/Outbox/      (new — durable outbox/work-item table + DaxaPos.Workers consumer, per ADR-0014's Handler I/O Rule; does not exist yet in the codebase)
src/DaxaPos.Api/Endpoints/Orders/
src/DaxaPos.Api/Endpoints/Payments/
src/DaxaPos.Api/Endpoints/Refunds/
src/DaxaPos.Persistence/Configurations/ (EF configs for every new entity)
src/DaxaPos.Persistence/Migrations/     (one migration per milestone, following PLAN-0004's convention)
tests/DaxaPos.UnitTests/Orders/, Payments/, Receipts/
tests/DaxaPos.Api.Tests/               (OrderEndpointsTests.cs, PaymentEndpointsTests.cs, RefundEndpointsTests.cs, ...)
```

`DaxaPos.Workers` is referenced in CLAUDE.md's suggested solution structure but does not exist yet either (`ls src/` confirms only the five projects above) — Milestone E (printing) is the first plan to actually need it, per the Architecture Assumptions below.

## Architecture Assumptions

- Orders reuse PLAN-0004's `TaxCalculationEngine` (`DaxaPos.Application.Tax`) and `PriceResolver` (`DaxaPos.Application.Pricing`) directly — this plan does not re-derive tax or pricing calculation logic, only order-level aggregation on top of them (per PLAN-0004's own Handoff Notes: "PLAN-0005's Order module will call this plan's tax-resolution and pricing-resolution logic directly rather than re-deriving it").
- **This plan is where ADR-0006's 20-tax-component-per-order design limit is finally enforced** — PLAN-0004 deliberately left this unenforced (it's an order-level aggregate across lines, and `Order` didn't exist yet). Order line/tax aggregation must check this explicitly, the same way `TaxCategoryDefinition` enforces the 10-per-line limit at the config layer.
- Order lines snapshot `Product.Name`, `TaxCategory.Code`, and the resolved tax marker code/label at the moment a line is added — not a live join to `Product`/`TaxCategory` at receipt/read time. This is required by ADR-0010 (immutable financial source records) and ADR-0011 (historical receipts preserve the marker meaning in effect at time of sale), and is also what makes a later `Product` edit (including ADR-0016 translations, once built) unable to retroactively change a historical order/receipt.
- Payments are ledgered (append-only, no silent edits) and idempotent (ADR-0010).
- Refunds create reversal records linked to original orders/payments; original records are never mutated.
- Receipt generation is a pure rendering step over already-immutable order/payment/refund source data (ADR-0010's "PDF Generation Strategy" pattern, applied to the receipt's structured model before any actual PDF/print-format rendering) — it does not recompute tax or price, it reads the snapshots already stored on the order.
- Receipt/tax-summary labels ("Total", "Includes GST", the tax-free marker legend) must be configurable/localisable strings per ADR-0011 + ADR-0016, never string literals baked into receipt-rendering code — MVP still ships only `en-AU` defaults (ADR-0016 §7), but the rendering code must read the label from configuration, not hard-code it.
- Receipt printer uses ESC/POS commands over network or USB (Windows terminals only for USB).
- **The printer service is this codebase's first real consumer of ADR-0014's Handler I/O Rule.** Sending a receipt to a printer is explicitly named in ADR-0014 as work that "must go through the outbox → `DaxaPos.Workers` path, not run inline" — no domain event handler in this plan may call a printer, a payment provider, or any other slow/external I/O directly in the request path. This plan is therefore also where the generic outbox/work-item table (referenced but not yet built by ADR-0014's Follow-Up Work) gets built for the first time, not a printing-specific mechanism invented separately.
- Module communication follows ADR-0014: direct calls for synchronous needs (`Order` calling `ITaxEngine`/`IPriceResolver`, later `IPaymentTerminalProvider.StartPaymentAsync`), in-process domain events for fast/local fan-out (audit logging), outbox + `DaxaPos.Workers` for anything slow/external (printing; a future payment-provider webhook path in PLAN-0009).
- `IPaymentTerminalProvider`'s interface definition is claimed by both this plan's Scope ("Payment adapter interface") and PLAN-0009's Scope ("`IPaymentTerminalProvider` adapter interface, implemented in code per ADR-0005's conceptual interface") — see Human Decisions Needed #1. This plan's Milestone B is where it is proposed to actually land, since PLAN-0009 already treats this plan as upstream ("this plan's non-adapter work").

## Domain Assumptions

- Cash and manual EFTPOS require no integrated terminal API call.
- Integrated payment always sends amount from POS to terminal (never manual entry) — enforced structurally in Milestone B by there being no request-DTO field for a staff-supplied amount on the integrated-payment path once PLAN-0009 lands, mirroring how `ProductSoldOutEndpoints`' `SetSoldOutRequest` has no `PriceOverride` field.
- Refunds may only be performed by authorised staff (RBAC) — module docs (`docs/modules/refunds.md`) call this "elevated staff role (configurable)"; this plan must decide a concrete permission code, not leave it as prose (see Human Decisions Needed #4).
- An order belongs to exactly one `Location`/`Terminal` and is opened by either a `User` or a `StaffMember` (never both), matching the dual `UserId`/`StaffMemberId` pattern PLAN-0004 Milestone F introduced for `ProductLocationOverrideChangedDomainEvent`.

## Risks

- ESC/POS compatibility varies by printer model (Milestone E only).
- Payment idempotency keys must be generated and tracked correctly (Milestone B).
- Refund permissions must prevent unauthorised refunds (Milestone C).
- **OI-0017 (Product archive-and-replace concurrency race)** — OI-0017's own Recommendation names this plan explicitly: "Schedule alongside PLAN-0005 (Orders/Payments), which is the next plan to read `Product` under real concurrent load from order-entry traffic." Milestone A (order line creation) is that read path. This is not a blocker — order lines read `Product` by id and snapshot its fields at add-line time regardless of whether the row is later archived-and-replaced, which is exactly the immutability behaviour Milestone A already needs for its own reasons (Architecture Assumptions above) — but it is the first place a fix (if the human wants one now rather than deferred further) would actually be exercised by real traffic. See Human Decisions Needed #3.
- The generic outbox/work-item mechanism (Milestone E) is new infrastructure with no existing precedent in this codebase to copy — first-of-its-kind risk, isolated to Milestone E, does not block Milestones A–D.

## Milestones

### Milestone A — Order service foundation

**Status: Done (2026-07-05).** Implemented as planned below, with the atomic `OrderNumberCounter` upsert mechanism per approved Human Decision #2. 967/967 tests passing (23 new: 5 unit `OrderTaxAggregationTests` + 18 API `OrderEndpointsTests`), 13 migrations verified clean from empty. See `PLAN-0005-worker-notes.md`'s "Milestone A Report" for full detail and deviations.

No payments, refunds, receipts, or printing. Pure order/order-line/tax-snapshot foundation — every later milestone depends on `Order` existing.

- `Order`: `Id`, `TenantId`, `OrganisationId`, `LocationId`, `TerminalId`, `OrderNumber` (`long`, location-scoped monotonic sequence per approved Human Decision #2 — implemented as an `OrderNumberCounter` row per `LocationId` incremented via a single atomic `INSERT ... ON CONFLICT DO UPDATE ... RETURNING` statement, not a computed `MAX(OrderNumber) + 1`, so concurrent order-open calls at the same location never race; not a native Postgres `SEQUENCE` object, since those can't be created dynamically per location without per-tenant DDL), `Status` (`Open`, `Held`, `Completed`, `Voided`, `Cancelled` — only `Open`/`Held`/`Voided`/`Cancelled` are reachable in this milestone; `Completed` is wired when Milestone B's payment recording closes an order), `OpenedAtUtc`, `ClosedAtUtc?`, `OpenedByUserId?`, `OpenedByStaffMemberId?`, `Notes?`, `IsTaxInclusivePricing` (snapshotted from `VenueTaxConfiguration` at open time — the venue config can change later without altering an already-open order's basis), `SubtotalAmount`, `TotalTaxAmount`, `GrandTotalAmount` (all server-computed, never client-supplied).
- `OrderLine`: `Id`, `OrderId`, `ProductId`, `ProductVariantId?`, `Quantity`, `ProductNameSnapshot`, `UnitPriceSnapshot`, `LineSubtotalAmount`, `LineTotalAmount`, `TaxCategoryCodeSnapshot`, `Notes?`, `Status` (`Active`/`Voided`), `VoidedAtUtc?`, `VoidedByUserId?`, `VoidedByStaffMemberId?`, `VoidReason?`.
- `OrderLineModifier`: `Id`, `OrderLineId`, `ModifierId`, `NameSnapshot`, `PriceDeltaSnapshot`.
- `OrderLineTax`: `Id`, `OrderLineId`, `TaxDefinitionId`, `TaxNameSnapshot`, `RatePercentSnapshot`, `TaxableAmount`, `TaxAmount`, `JurisdictionNameSnapshot`, `JurisdictionTypeSnapshot`, `ReceiptMarkerCodeSnapshot?`, `ReceiptMarkerLabelSnapshot?` — one row per `TaxLineResult` returned by `TaxCalculationEngine.CalculateLine`, stored verbatim (not recalculated later).
- Order-line add flow: resolve `Product`/`ProductVariant`/`Modifier`s and `ProductLocationOverride`/`VenueTaxConfiguration` for the order's location via `PriceResolver.Resolve` (reused as-is), reject if the product is sold-out/unavailable/inactive/archived at add-line time, reject if `VenueTaxConfiguration` is missing (404/fail-closed, matching PLAN-0004's Human Decision #5 precedent — not silently defaulted), resolve applicable `TaxCategoryDefinition`s for `(Product.TaxCategoryId, Location)` and call `TaxCalculationEngine.CalculateLine`, persist the line + its `OrderLineTax` rows in the same transaction, recompute the order's running totals.
- Enforce ADR-0006's 20-tax-components-per-order limit here (summed distinct `TaxDefinitionId`s across all active lines) — this is the milestone PLAN-0004 deferred it to.
- Order state machine: `Open` → add/remove lines, modifiers, notes; `Open ⇄ Held` (park/resume, e.g. a tab); any line may be voided while `Open`/`Held` (reversal, not delete, per ADR-0010); the whole order may be `Voided`/`Cancelled` while `Open`/`Held` and before any payment exists. Transition to `Completed` is out of scope for this milestone (needs Milestone B's payment recording) but the enum value is defined now so Milestone B doesn't need a schema change to add it.
- New permission code(s) — see Human Decisions Needed #5 for the exact code/category; this plan's working assumption is `orders.manage`, `PermissionCategory.Operational`, granted to `SystemAdmin`/`OrganisationOwner`/`VenueManager`/`Staff` (order entry is the core POS workflow a staff-PIN session must be able to do, the same reasoning PLAN-0004 used for `catalog.sold-out-toggle` and the resolved-menu read).

**Entities/tables:** `Order`, `OrderLine`, `OrderLineModifier`, `OrderLineTax` (all new).
**Migration:** one new migration, all four tables, `TenantId`-indexed + fail-closed query filters on all four (following the `Product`/`Menu` precedent — no bootstrap `IgnoreQueryFilters()` caller needed).
**Endpoints:** `POST /api/v1/orders` (open), `GET /api/v1/orders/{id}`, `GET /api/v1/orders` (list, filtered by location/status), `POST /api/v1/orders/{id}/lines` (add), `DELETE /api/v1/orders/{id}/lines/{lineId}` (void, not hard delete), `POST /api/v1/orders/{id}/hold`, `POST /api/v1/orders/{id}/resume`, `POST /api/v1/orders/{id}/void`, `POST /api/v1/orders/{id}/cancel` — 9 endpoints, `orders.manage` + `rejectStaffPin: false` (pending Human Decisions Needed #5).
**Tests:** unit tests for order-line tax aggregation against `TaxCalculationEngineTests`' own AU/NZ mixed-basket fixtures (reusing the fixtures, not re-deriving new ones), the 20-component-per-order boundary/rejection, integration tests for the full CRUD/state-machine matrix plus RBAC (staff-PIN-succeeds proof, matching `ProductSoldOutEndpointsTests`/`ResolvedMenuEndpointsTests`' pattern), cross-tenant/cross-organisation/cross-location 404s, and a fail-closed test for missing `VenueTaxConfiguration` at add-line time.
**Docs:** `docs/modules/orders.md` implementation-status section.

### Milestone B — Payment foundation (cash, manual EFTPOS, ledger, adapter interface)

**Status: Done (2026-07-05).** Implemented as planned below. `PaymentLedgerEntry` gained a denormalized `TenantId` (deviation, same reasoning as Milestone A's `OrderLine`/`OrderLineModifier`/`OrderLineTax`). `IPaymentTerminalProvider` is interface + placeholder DTOs only, with no DI registration (nothing to register with zero adapters). 985/985 tests passing (18 new: 7 unit `PaymentSettlementTests` + 11 API `PaymentEndpointsTests`), 14 migrations verified clean from empty. See `PLAN-0005-worker-notes.md`'s "Milestone B Report" for full detail and deviations.

- `Payment`: `Id`, `OrderId`, `TenantId`, `LocationId`, `Method` (`Cash`, `ManualEftpos`, `Integrated` — `GiftCard`/`StoreCredit` deferred), `Status` (`Created`, `Approved`, `Declined`, `Cancelled`, `TimedOut`, `Recorded` — matching `docs/modules/payments.md`'s documented lifecycle exactly), `AmountRequested`, `AmountApproved?`, `IdempotencyKey` (unique index), `TakenByUserId?`, `TakenByStaffMemberId?`, `RecordedAtUtc`, `ProviderReference?` (null for cash/manual).
- `PaymentLedgerEntry`: append-only row per state transition — `Id`, `PaymentId`, `Status`, `AmountAmount`, `OccurredAtUtc`, `Metadata` (jsonb) — proves the ledger is genuinely append-only rather than `Payment.Status` being overwritten with no trail.
- Cash payment: recorded immediately as `Recorded`, no idempotency-key collision path meaningfully retried (cash has no external system to retry against, but the key is still required and unique for consistency with the integrated path).
- Manual EFTPOS: staff confirms an amount was taken on an external terminal; Daxa POS never calls out anywhere — same immediate-`Recorded` shape as cash, distinguished only by `Method`.
- `IPaymentTerminalProvider` interface (in `DaxaPos.Application.Payments`, per ADR-0005's conceptual interface, verbatim): `StartPaymentAsync`, `RefundAsync`, `GetTerminalStatusAsync`, `CancelPaymentAsync` — interface only, adapter resolution wiring (DI registration keyed by configured provider), **no concrete adapter** — Stripe Terminal is PLAN-0009's scope, not this milestone's.
- A payment fully settles (`AmountApproved == Order.GrandTotalAmount`, summed across all `Recorded` payments on the order, split payments included) transitions `Order.Status` to `Completed` and sets `Order.ClosedAtUtc` — the one place this milestone reaches back into Milestone A's state machine.
- New permission code: `payments.record` (or fold into `orders.manage` — see Human Decisions Needed #5), `Operational`, staff-PIN-eligible (taking a cash/EFTPOS payment is routine counter work, same reasoning as order entry itself).

**Entities/tables:** `Payment`, `PaymentLedgerEntry` (new).
**Migration:** one new migration.
**Endpoints:** `POST /api/v1/orders/{id}/payments` (record cash/manual EFTPOS payment — `Method` in the body distinguishes; the integrated-payment route is added by PLAN-0009, not here), `GET /api/v1/orders/{id}/payments` (list) — 2 endpoints.
**Tests:** idempotency-key retry does not create a duplicate `Payment` row, split-payment total-settlement closes the order, ledger entries are append-only (no `UPDATE` on a prior ledger row), RBAC staff-PIN-succeeds proof.
**Docs:** `docs/modules/payments.md` implementation-status section.

### Milestone C — Refund service

**Status: Done (2026-07-06).** Implemented as planned below. No new ledger table for refunds (deviation, see worker notes) — a `Refund` row is append-only by itself in this milestone, and ADR-0010's refund-audit requirement is satisfied by the row plus `RefundLifecycleAuditHandler`. 1001/1001 tests passing (5 new unit `RefundSettlementTests` + 11 new API `RefundEndpointsTests`), 15 migrations verified clean from empty. See `PLAN-0005-worker-notes.md`'s "Milestone C Report" for full detail and deviations.

- `Refund`: `Id`, `PaymentId`, `OrderId`, `TenantId`, `Amount`, `ReasonCode`, `ReasonNote?`, `RequestedByUserId?`, `RequestedByStaffMemberId?`, `Status` (`Recorded`, later `ProviderPending`/`ProviderConfirmed` once PLAN-0009's adapter refund path exists), `RecordedAtUtc`, `ProviderReference?`.
- Full and partial refunds — `Amount <= Payment.AmountApproved - Sum(existing refunds against that payment)`, enforced server-side (never client-trusted).
- Refunds create a reversal record; `Payment`/`Order` are never mutated — a refunded order's `GrandTotalAmount`/line data stay exactly as they were at sale time (ADR-0010).
- Concrete permission code decision required here — module docs say "elevated staff role (configurable)" but no code exists yet; recommend `payments.refund`, `AdminSensitive` (not `Operational` — unlike order entry/cash payment, refunds are exactly the kind of override CLAUDE.md's Pricing/discounts section says must be "permissioned and audited," and OI-0007's precedent for tax-category changes reserved similarly consequential actions to manager-level or higher).

**Entities/tables:** `Refund` (new).
**Migration:** one new migration.
**Endpoints:** `POST /api/v1/payments/{id}/refunds`, `GET /api/v1/payments/{id}/refunds` — `payments.refund`, `rejectStaffPin: true`.
**Tests:** full/partial refund happy path, over-refund rejection, refund audit-row assertions (who/when/reason/linked order+payment ids per ADR-0010), staff-PIN rejection proof (this is squarely an `AdminSensitive` surface, not staff-accessible).
**Docs:** `docs/modules/refunds.md` implementation-status section.

### Milestone D — Receipt generation

- Pure receipt-rendering model (`DaxaPos.Application.Receipts`, no DB dependency, mirrors `TaxCalculationEngine`'s pure/typed shape): takes an already-loaded `Order` + its lines/tax rows/payments/refunds and a `ReceiptLabelSet` (venue-configured label strings per ADR-0011/ADR-0016), returns a structured `ReceiptDocument` (header, line items with marker codes, tax summary, payment summary, footer) — not a print-ready byte stream yet (that's Milestone E's job).
- Tax marker resolution reuses the precedence ADR-0011 already defines (item override → tax category marker → tax definition marker → location default) — this plan reads that precedence at receipt-render time from the snapshots Milestone A already stored on `OrderLineTax`, it does not add a second resolution mechanism.
- Refund receipts link to the original order/payment (`Refund.PaymentId`/`OrderId`), per ADR-0010.
- Reprints are audited (`ReceiptReprintedDomainEvent` or equivalent) — CLAUDE.md's explicit requirement.
- No new permission code — receipt viewing/printing during a live sale is part of `orders.manage`/`payments.record`'s existing surface; a standalone reprint-after-the-fact action may need its own code (flag as a Milestone D-time decision, not resolved here).

**Entities/tables:** none new — receipts are rendered from `Order`/`OrderLine`/`OrderLineTax`/`Payment`/`Refund`, not persisted as their own row (ADR-0010's "regenerate from immutable source data" pattern), except a minimal `ReceiptReprintAuditEntry` if reprint-audit can't be satisfied by the existing generic `AuditEvent` table alone.
**Migration:** none, or a minimal one for `ReceiptReprintAuditEntry` if needed.
**Endpoints:** `GET /api/v1/orders/{id}/receipt` (renders the structured document), `POST /api/v1/orders/{id}/receipt/reprint` (audited).
**Tests:** GST-free marker rendering (the CLAUDE.md worked example, byte-for-byte), tax summary accuracy against `OrderLineTax` snapshots, refund receipt links to original order/payment, reprint audit row written.
**Docs:** `docs/modules/receipts.md` implementation-status section.

### Milestone E — ESC/POS printing and the outbox mechanism

- The generic outbox/work-item table + `DaxaPos.Workers` consumer (ADR-0014's Handler I/O Rule, never built before this milestone) — a durable row written in the same transaction as the triggering domain event, processed asynchronously outside the request path.
- ESC/POS command generation from a `ReceiptDocument` (Milestone D's output), printer transport (network first, USB for Windows terminals per CLAUDE.md), cash drawer kick command.
- Print queue with retry on failure; printer health/reachability status.
- A payment/order-completion domain event enqueues a print-receipt outbox row rather than calling the printer inline from the request handler — the concrete proof of ADR-0014's rule, not just documentation of it.

**Entities/tables:** `OutboxWorkItem` (or similarly named — generic, not printing-specific), `PrinterDevice` (if not already covered by PLAN-0003's device model — check before adding a new table).
**Migration:** one new migration.
**Endpoints:** none required for MVP printing itself (the print is triggered by the outbox consumer, not a direct API call) — a manual `POST /api/v1/orders/{id}/print` may be added for staff-triggered reprints, reusing Milestone D's reprint audit path.
**Tests:** outbox row written transactionally with the triggering event, `DaxaPos.Workers` consumer processes and marks it done, retry-on-failure, ESC/POS byte-output tests against a fixed expected command sequence (no physical printer required for unit tests).
**Docs:** `docs/modules/printing.md` implementation-status section; `docs/architecture/` gains a new outbox-mechanism doc or an addition to `docs/architecture/overview.md` (ADR-0014 itself doesn't document the mechanism's concrete shape, only the rule that it must exist).

### Milestone F — Consolidation, RBAC sweep, and documentation closeout

Test-and-documentation-only, mirroring PLAN-0004 Milestone H exactly: extend `RbacTests.cs`'s endpoint inventory with every Milestone A–E `rejectStaffPin: true` route, extend `StaffPinLoginTests`'s shared inventory, re-verify all migrations from an empty database, confirm zero new `IgnoreQueryFilters()` call sites, review whether OI-0017 (see Human Decisions Needed #3) needs filing as touched-by-this-plan or remains tracked separately, update `docs/CHANGELOG.md`/`docs/issues/index.md`/`docs/adr/index.md` as needed.

## Permission Catalogue Additions (Summary)

Approved per Human Decisions Needed #5 (Approval Record above) — see that section's table for the full 5-code list including the two receipt/printing codes added by approval (`receipts.reprint`, `printing.manage`). Milestone A implements only `orders.manage`.

## Tests To Run Later

- Order creation and tax calculation (using tax engine from PLAN-0004).
- ADR-0006's 20-tax-component-per-order limit (new — PLAN-0004 deferred this to this plan).
- Cash payment recording and ledger entry.
- Manual EFTPOS payment recording.
- Refund with linked original payment.
- Receipt rendering with GST-free marker.
- Receipt tax summary accuracy.
- Payment idempotency (retry does not create duplicate payment).
- Outbox durability (row survives a simulated worker crash/restart) and retry.
- Audit log entries for order, payment, refund, and reprint activity.

## Documentation To Update

- `docs/modules/orders.md`
- `docs/modules/payments.md`
- `docs/modules/refunds.md`
- `docs/modules/receipts.md`
- `docs/modules/printing.md`
- `docs/modules/audit.md`
- `docs/architecture/payment-adapters.md`
- `docs/architecture/overview.md` (new outbox mechanism, Milestone E)

## ADRs Required

- ADR-0005, ADR-0010, ADR-0011, ADR-0014, ADR-0016 (all already accepted) — no new ADR required for Milestones A–D. **Milestone E may warrant a short ADR addendum or a new ADR documenting the concrete outbox/work-item schema**, since ADR-0014 states the rule but not the mechanism's shape — flag as a candidate at Milestone E start, not decided now.

## Open Issues Required

- None filed by this planning pass. OI-0017 is discussed under Risks/Human Decisions Needed #3 but is not filed as blocking — see there.

## Commit Sequence

```
feat(orders): add order service and state machine
test(orders): cover order lifecycle, tax aggregation, and authorization
feat(payments): add cash and manual EFTPOS payment, ledger, and adapter interface
test(payments): cover payment ledger, idempotency, and authorization
feat(refunds): add refund service with original payment link
test(refunds): cover refund authorization and over-refund rejection
feat(receipts): add receipt generation with tax markers
test(receipts): cover marker rendering and tax summary accuracy
feat(printing): add outbox mechanism and ESC/POS printer service
test(printing): cover outbox durability and retry
docs: close PLAN-0005 Milestone F
```

## Human Decisions Needed

**Approval record (2026-07-05):** all 5 items below were approved, unblocking Milestone A. Approval refined three of the five beyond the plan's own recommendation, recorded here so a later milestone doesn't re-derive or contradict them:

- **Item 1** (interface ownership split) approved as recommended, with an explicit scope boundary added: this plan owns the payment, receipt, print-job, and order-transaction *domain* abstractions (`IPaymentTerminalProvider`'s interface in Milestone B; the outbox/work-item and `ReceiptDocument` shapes in Milestones D/E) — hardware/provider-specific adapters, daemons, printer discovery, device protocols, and installer/runtime deployment mechanics belong to PLAN-0009 or a later hardware-integration plan. **Milestone E's printer transport code must stay behind an interface Milestone A–D's abstractions don't leak past** — no ESC/POS byte-generation or transport-specific type may appear in `Order`/`Payment`/`Refund`/`ReceiptDocument` shapes.
- **Item 2** (`Order.OrderNumber`) approved as recommended: a server-side, database-backed sequence/counter per venue/location, monotonic and safe under concurrent order creation, never client-generated.
- **Item 3** (OI-0017) approved as recommended: tracked risk, not a Milestone A blocker. Milestone A may read `Product`/`ProductVariant`/`Menu` data as needed but must not attempt to fix the archive-and-replace race unless a direct implementation blocker appears. OI-0017 stays open.
- **Item 4** (refund permission model) approved with a firmer floor than the plan's own recommendation: explicit refund permission code(s), kept separate from `orders.manage`/`payments.record`, and **manager/admin-only by default** (not merely "AdminSensitive category" — the plan's `payments.refund` proposal already satisfies this; approval confirms it rather than leaving the threshold-escalation question open for Milestone C to re-litigate).
- **Item 5** (permission codes/categories) approved with one addition beyond the plan's original three-code table: **explicit receipt and printing permission codes are also required**, resolving Milestone D's previously-open "may need its own code, not resolved here" question. Concretely: `receipts.reprint` (reprinting a receipt after the fact) and `printing.manage` (print-job retry/administration) must be permission-gated separately from `orders.manage`'s live-sale surface — working proposal below, to be finalised when Milestones D/E are actually implemented, not decided further now.

**Permission Catalogue Additions, updated per approval:**

| Code | Category | Milestone | Note |
|------|----------|-----------|------|
| `orders.manage` | Operational | A | Staff-PIN-eligible — order entry is core counter work. |
| `payments.record` | Operational | B | Staff-PIN-eligible — cash/EFTPOS recording is core counter work. |
| `payments.refund` | AdminSensitive | C | Manager/admin-only by default, per approved Item 4. |
| `receipts.reprint` | Operational (proposed) | D | New per approved Item 5 — live-sale receipt viewing/printing stays under `orders.manage`; only the standalone after-the-fact reprint action gets its own code. Category to be confirmed at Milestone D start. |
| `printing.manage` | AdminSensitive (proposed) | E | New per approved Item 5 — print-job retry and print-job administration, never staff-PIN-eligible. |

Recorded here, mirroring PLAN-0004's own planning-pass convention, so implementation can start on approval without re-litigating mid-milestone:

1. **`IPaymentTerminalProvider`'s interface definition is listed in both this plan's Scope and PLAN-0009's Scope.** Recommendation: this plan (Milestone B) defines the interface and DI resolution wiring; PLAN-0009 implements the first concrete adapter (`DaxaPos.PaymentProviders.StripeTerminal`, or the equivalent path in the actual flat project layout) against it. PLAN-0009's own Handoff Notes already assume this plan goes first ("this plan's non-adapter work"), so this just makes that assumption explicit rather than leaving two plans each believing they own the same file.
2. **`Order.OrderNumber` generation** — a location-scoped, human-readable sequential number (e.g. daily or all-time sequence per location) is the obvious staff-facing expectation, but a naive `MAX(OrderNumber) + 1` read-then-write has the same race shape as OI-0013/OI-0017. Recommendation: a Postgres sequence per location (or a single global sequence formatted with a location prefix) rather than a computed max, decided at Milestone A start, not deferred.
3. **OI-0017 (Product archive-and-replace concurrency race)** — fix now (alongside Milestone A, since it's the first plan reading `Product` under real order-entry traffic per OI-0017's own recommendation) or continue to accept it as a documented risk and track only as a hardening pass later? Recommendation: continue to accept for Milestone A specifically (order-line add reads and snapshots `Product` by id; it doesn't itself race with a concurrent `PATCH` the way two concurrent `PATCH`es race with each other) — but flag this plan as the trigger to schedule the OI-0013/OI-0017 hardening pass, since "next plan to read `Product` under concurrent load" has now arrived.
4. **Refund permission code** — is `payments.refund` sufficiently "elevated" on its own (granted to `VenueManager`+), or should refunds above a configurable threshold require a second, higher-tier approval (e.g. `SystemAdmin`/`OrganisationOwner` only for refunds over $X)? Recommendation: single `payments.refund` code for MVP (matching the module doc's "configurable" language as a future refinement, not an MVP requirement) — a threshold-based escalation is Phase 2 discount/override-engine territory, not this plan's scope.
5. **New permission codes and categories (`orders.manage`, `payments.record`, `payments.refund`) are this plan's own proposal, not yet confirmed against a documented decision the way PLAN-0004's codes were confirmed against OI-0007.** Recommendation: approve as proposed — `orders.manage`/`payments.record` as `Operational` (staff-PIN-eligible, since order entry and taking a cash/EFTPOS payment are the core counter workflow a staff-PIN session must be able to do, exactly PLAN-0004's `catalog.sold-out-toggle`/resolved-menu reasoning) and `payments.refund` as `AdminSensitive` (staff-PIN rejected, matching `catalog.manage`/`pricing.manage`/`menus.manage`).

## Handoff Notes

Depends on PLAN-0004 (Catalog/Tax/Pricing/Menu — closed, `dotnet test` 944/944, all 12 migrations verified clean). Payment tests require the tax engine to be working; both are already true. The first integrated payment provider (Stripe Terminal) is implemented in [PLAN-0009](PLAN-0009-first-payment-adapter-stripe-terminal.md), which should proceed alongside or immediately after Milestone B (the milestone that defines `IPaymentTerminalProvider`, pending Human Decisions Needed #1). Next parallel track once this plan is under way: PLAN-0006 (Terminal, Display, PWA). See `PLAN-0005-worker-notes.md` for this planning pass's full session record.
