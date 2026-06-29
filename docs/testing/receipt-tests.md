# Testing: Receipt Tests

Receipt tests verify correct rendering of tax invoices and refund receipts.

---

## Required Test Categories

### GST Receipt (AU)

- AU mixed basket receipt shows correct product names.
- GST-free items show `F` marker.
- Non-GST items show no marker.
- Receipt footer includes `F = GST-free`.
- Total GST line is correct.
- `Includes GST $X.XX` shown correctly.

### NZ GST Receipt

- NZ basket receipt shows correct product names.
- GST at 15% is shown correctly.
- Zero-rated items are marked appropriately.

### Refund Receipt

- Refund receipt links to original order number.
- Refund receipt shows refund amount.
- Refund receipt shows original payment method.
- Refund receipt shows reason.

### Tax Invoice Format

- ABN is shown on tax invoice.
- Venue name and address are shown.
- Receipt date and time are correct.
- Order number is shown.
- Payment method is shown.

### Reprint Audit

- Reprint creates an audit event.
- Reprint shows correct original order data.

---

## Test Project

```
tests/DaxaPos.Receipt.Tests/
```

---

## Related Documents

- [ADR-0011 — Receipt Tax Marker Strategy](../adr/proposed/ADR-0011-receipt-tax-marker-strategy.md)
- [Module: Receipts](../modules/receipts.md)
- [Module: Tax](../modules/tax.md)
