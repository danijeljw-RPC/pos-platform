# Module: Receipt Service

The receipt service generates thermal and digital receipts.

See also: `docs/modules/11-customer-display-receipts-printing.md`.

---

## Responsibilities

- Thermal receipt generation (ESC/POS-ready).
- Tax invoice generation.
- Refund receipt generation (linked to original order/payment).
- Gift receipt (later).
- Digital receipt (later — email/QR).
- Receipt templates.
- GST-free item marker (`F = GST-free`).
- Tax summary section.
- Reprint audit.
- Provider payment details on receipt.

## Receipt Structure

```text
Header: Venue name, address, ABN
Order lines: product name, quantity, price, tax marker
Surcharges / discounts
Total
Tax summary: Includes GST $X.XX
Payment: method, amount
Footer: Thank you, receipt options
```

## Related Plans

- [PLAN-0005 — Payments, Receipts, Printing](../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
- [ADR-0011 — Receipt Tax Marker Strategy](../adr/accepted/ADR-0011-receipt-tax-marker-strategy.md)
- [ADR-0016 — Multi-Language and Localisation Strategy](../adr/accepted/ADR-0016-multi-language-and-localisation-strategy.md) (accepted 2026-07-05) — receipt text localisation is planned but deferred; receipt labels must remain configurable/localisable rather than hard-coded, per that ADR.

## Implementation Status (PLAN-0005 Milestone D, 2026-07-06)

Receipt generation foundation is implemented as a pure, DB-independent rendering model —
`DaxaPos.Application.Receipts.ReceiptRenderer` — plus 2 endpoints under
`src/DaxaPos.Api/Endpoints/Receipts/ReceiptEndpoints.cs`. No printing, PDF generation, or UI yet.

- `ReceiptRenderer.Render(ReceiptOrderInput, ReceiptLabelSet)` (TDD'd first, mirrors
  `OrderTaxAggregation`/`RefundSettlement`'s dependency-free shape) takes already-loaded snapshots —
  never queries the database, never knows about `Order`/`OrderLine`/`Payment`/`Refund` entities
  directly — and returns a structured `ReceiptDocument` (line items, tax summary, marker legend,
  payment summary, refund summary). It never recomputes tax or price; every amount is read verbatim
  from the caller-supplied `OrderLineTax`/`Payment`/`Refund` snapshots (ADR-0006, ADR-0010's "PDF
  Generation Strategy" pattern).
- Tax markers (ADR-0011): a rendered line's marker code is the first non-null
  `ReceiptMarkerCodeSnapshot` among its tax components — read from the snapshot Milestone A already
  stored at add-line time, not re-resolved. The footer marker legend (`"{Code} = {Label}"`) lists
  each distinct marker once, sorted, only when at least one active line uses it. Proven byte-for-byte
  against the CLAUDE.md/ADR-0006 AU mixed-basket worked example (`F = GST-free`, $20.30 total, $1.30
  GST).
- Voided lines are excluded from the rendered receipt (a void is a reversal, never a charged line —
  ADR-0010); `Order.SubtotalAmount`/`GrandTotalAmount` already exclude them.
- Receipt labels (`ReceiptLabelSet`, e.g. `TotalLabel`/`TaxInclusiveSummaryLabel`) are a parameter to
  the renderer, not hard-coded strings — `ReceiptLabelSet.Default` is the only concrete instance
  shipped this milestone (en-AU: `"Total"`/`"Includes GST"`, per ADR-0016 §7's MVP scope); a later
  location-level settings lookup can supply a different instance with no renderer change.
- Refund-receipt linking (ADR-0010): a `Refund`'s `PaymentId`/`OrderId` place it in the same
  `ReceiptDocument` as the order's own payments — no separate "refund receipt" shape was needed for
  this foundation.
- `GET /api/v1/orders/{id}/receipt` — live-sale receipt viewing, gated `orders.manage` (unaudited,
  same surface as order entry/payment recording).
- `POST /api/v1/orders/{id}/receipt/reprint` — the standalone after-the-fact reprint action, gated
  the new `receipts.reprint` permission (`Operational`, staff-PIN-eligible like `orders.manage`/
  `payments.record`, per approved Human Decision #5) and audited via
  `ReceiptReprintedDomainEvent`/`ReceiptReprintedAuditHandler` (`EventType = "ReceiptReprinted"`,
  `EntityType = "Receipt"`, `EntityId = OrderId` — no dedicated `Receipt` table exists; receipts are
  regenerated from immutable source data every call, never persisted as their own row).
- See `docs/plans/active/PLAN-0005-worker-notes.md`'s "Milestone D Report" for full detail.
