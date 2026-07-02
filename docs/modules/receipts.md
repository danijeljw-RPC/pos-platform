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
- [ADR-0016 — Multi-Language and Localisation Strategy](../adr/proposed/ADR-0016-multi-language-and-localisation-strategy.md) (proposed) — receipt text localisation is planned but deferred; receipt labels must remain configurable/localisable rather than hard-coded, per that ADR.
