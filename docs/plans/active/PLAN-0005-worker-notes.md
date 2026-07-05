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

## Recommended Next Session

1. Human reviews and (dis)approves the plan's 5 Human Decisions Needed items.
2. On approval, implement Milestone A first (Order service foundation — `Order`, `OrderLine`, `OrderLineModifier`, `OrderLineTax`, the 9 endpoints, and the 20-tax-component-per-order enforcement).
3. Update this plan's Status section with milestone checkboxes as work proceeds — no more than 3 commits without a refresh, per CLAUDE.md's plan-refresh rule, exactly as PLAN-0003/PLAN-0004 did throughout their own milestones.
