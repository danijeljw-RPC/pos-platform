# Module: Order Service

The order service manages the full lifecycle of a POS order.

See also: `docs/modules/02-order-types.md`.

---

## Responsibilities

- Creating orders.
- Adding and removing order lines.
- Modifiers per line.
- Order notes and item notes.
- Void/cancel order lines.
- Hold and resume orders.
- Table and tab assignment (Phase 2).
- Split bill (Phase 2).
- Merge orders (Phase 2).
- Order state machine.
- Order state reconstructable from server/database.

## Order State Machine

```text
Open
→ Paid
→ Voided
→ Refunded (partial or full)
```

## Order Entities

```text
Order
OrderLine
OrderLineModifier
OrderLineTax         (tax snapshot at sale time)
OrderSurcharge
OrderDiscount
```

## Implementation Status (PLAN-0005 Milestone A, 2026-07-05)

Order service foundation is implemented with endpoints under `src/DaxaPos.Api/Endpoints/Orders/`. No payments, refunds, receipts, or printing yet — every later milestone depends on `Order` existing.

- `Order`: `TenantId`, `OrganisationId`, `LocationId`, `TerminalId`, `OrderNumber` (`long`, location-scoped, allocated via an atomic `INSERT ... ON CONFLICT DO UPDATE ... RETURNING` against a new `OrderNumberCounter` row-per-location — never a computed `MAX + 1`, which would race under concurrent order-open calls the same way OI-0013/OI-0017 do), `Status` (`Open`/`Held`/`Completed`/`Voided`/`Cancelled` — only the first four are reachable until Milestone B wires payment settlement), `OpenedByUserId?`/`OpenedByStaffMemberId?` (dual-identity pattern, matching `ProductLocationOverrideChangedDomainEvent`), `IsTaxInclusivePricing` (snapshotted from `VenueTaxConfiguration` at open time, fail-closed 404 if missing — reusing PLAN-0004's approved Human Decision #5 precedent), `SubtotalAmount`/`TotalTaxAmount`/`GrandTotalAmount` (server-computed, never client-supplied).
- `OrderLine`/`OrderLineModifier`/`OrderLineTax` snapshot `Product.Name`/`Modifier.Name`/tax fields by value at add-line time, per ADR-0010/ADR-0011 — never a live join at read/receipt time. `OrderLineTax` stores one row per `TaxLineResult` from `TaxCalculationEngine.CalculateLine`, verbatim.
- Order-line add reuses PLAN-0004's `PriceResolver`/`TaxCalculationEngine` directly (no re-derivation). Tax category resolution precedence: a location-specific set of `TaxCategoryDefinition` rows wins entirely over the organisation-wide set for the same `TaxCategory`, matching the resolved-menu endpoint's approved merge rule (Human Decision #7) — never a partial per-component merge.
- **ADR-0006's 20-distinct-tax-component-per-order limit is enforced here** (`DaxaPos.Application.Orders.OrderTaxAggregation`, TDD'd first) — the limit PLAN-0004 deliberately left unenforced since `Order` didn't exist yet. Counts distinct `TaxDefinitionId`s across all active lines, not `OrderLineTax` row count, so a 21-line all-GST order does not spuriously fail.
- Lines/products/modifiers are validated at add-line time: product must be `IsActive`, not `IsArchived`, and not sold-out/unavailable via `ProductLocationOverride`; a supplied `ProductVariantId` must belong to the product; supplied `ModifierIds` must be active and linked to the product via `ProductModifierGroup`.
- Order state machine: `Open ⇄ Held` (park/resume); any line may be voided (reversal, never deleted) while `Open`/`Held`; the whole order may become `Voided`/`Cancelled` from `Open`/`Held` (no restriction on line count — a fully-built order can still be cancelled). `Completed` is defined but unreachable until Milestone B.
- New permission `orders.manage` — `Operational` category, staff-PIN-eligible, granted to `SystemAdmin`/`OrganisationOwner`/`VenueManager`/`Staff` — the plan's first staff-accessible write surface from day one (unlike PLAN-0004's catalogue writes, admin-only until Milestone F's sold-out toggle).
- See `docs/plans/active/PLAN-0005-worker-notes.md`'s "Milestone A Report" for full detail and deviations.

## Related Modules

- Tax (tax snapshot on each order line)
- Payments (payment records linked to order)
- Receipts (receipt generated from completed order)
- Audit (all order events audited)

## Related Plans

- [PLAN-0005 — Payments, Receipts, Printing](../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
