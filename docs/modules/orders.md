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

## Related Modules

- Tax (tax snapshot on each order line)
- Payments (payment records linked to order)
- Receipts (receipt generated from completed order)
- Audit (all order events audited)

## Related Plans

- [PLAN-0005 — Payments, Receipts, Printing](../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
