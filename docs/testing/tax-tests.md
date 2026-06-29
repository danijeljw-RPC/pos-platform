# Testing: Tax Engine Tests

Tax engine tests are mandatory. Financial and tax logic must not be changed without test coverage.

---

## Required Test Categories

### AU GST Mixed Basket

- Basket containing AU_GST_10 and AU_GST_FREE items.
- Verify correct GST per line (5.50/11 = 0.50, etc.).
- Verify total GST is sum of line GSTs.
- Verify GST-free lines show $0.00 tax.
- Verify total inc GST is correct.

### NZ GST

- NZ_GST_15 basket.
- Verify NZ GST at 15%.
- Verify zero-rated items show $0.00 tax.

### Tax Snapshot

- Snapshot is stored on order line at sale time.
- Snapshot is not changed if tax rate changes after order.
- Historical order shows original tax snapshot.

### Tax Summary

- Order tax summary matches sum of line-level tax snapshots.
- Multiple tax categories in one order.

### Receipt Marker

- GST-free items show `F` marker on receipt.
- Receipt footer includes `F = GST-free` legend.
- Non-GST-free items do not show marker.

### Modifier Tax

- Modifier with additional charge uses product's tax category.
- Modifier tax is included in line tax snapshot.

### Rounding

- Per-line rounding test.
- Per-order total rounding test.
- Cash rounding (AU rounds to $0.05 — later).

---

## Test Project

```
tests/DaxaPos.Tax.Tests/
```

---

## Related Documents

- [ADR-0006 — Tax-Line Based Tax Engine](../adr/proposed/ADR-0006-tax-line-based-tax-engine.md)
- [ADR-0011 — Receipt Tax Marker Strategy](../adr/proposed/ADR-0011-receipt-tax-marker-strategy.md)
- [Architecture: Tax Engine](../architecture/tax-engine.md)
- [Module: Tax](../modules/tax.md)
