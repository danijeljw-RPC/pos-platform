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
tests/DaxaPos.UnitTests/Tax/
```

The original architecture-level sketch above named a dedicated `DaxaPos.Tax.Tests` project; the actual repository (PLAN-0002 Platform Skeleton onward) has only two test projects, `DaxaPos.UnitTests` and `DaxaPos.Api.Tests`. Pure-logic tests with no DB/HTTP dependency (like the tax engine) live under `DaxaPos.UnitTests`, matching PLAN-0003's `Pbkdf2PinHasherTests`/`LoginLockoutPolicyTests` precedent.

## Implementation Status (PLAN-0004 Milestone B, 2026-07-04)

`tests/DaxaPos.UnitTests/Tax/TaxCalculationEngineTests.cs` — 10 tests, covering: AU GST-inclusive single item, the AU mixed-basket worked example above (byte-for-byte), a GST-free line producing a populated zero-tax result (not an absent line), NZ GST-inclusive at 15%, tax-exclusive calculation, rounding at a genuine 2-decimal midpoint (proving `NearestCent` is implemented as away-from-zero, not the CLR's banker's-rounding default), missing-configuration fail-closed behaviour, the exact-10/over-10 component boundary (ADR-0006's design limit), and determinism (no DB/HTTP/session dependency exists to inject). Tax Snapshot, Tax Summary (order-level), Receipt Marker, and Modifier Tax test categories below are not yet implemented — they depend on `Order`/`Product`/`Receipt` entities that don't exist until PLAN-0005/Milestone D+.

---

## Related Documents

- [ADR-0006 — Tax-Line Based Tax Engine](../adr/accepted/ADR-0006-tax-line-based-tax-engine.md)
- [ADR-0011 — Receipt Tax Marker Strategy](../adr/accepted/ADR-0011-receipt-tax-marker-strategy.md)
- [Architecture: Tax Engine](../architecture/tax-engine.md)
- [Module: Tax](../modules/tax.md)
