# Module: Printer Service

The printer service routes print jobs to receipt, kitchen, bar, and label printers.

---

## Responsibilities

- Receipt printer routing.
- Kitchen/bar docket routing (Phase 2).
- Label printer routing (later).
- ESC/POS command generation.
- Cash drawer kick via printer port.
- Printer health monitoring.
- Print queue with retry on failure.
- Reprint flow.

## Printer Types

| Type | Connection | MVP |
|------|-----------|-----|
| Receipt printer | Network (Ethernet/Wi-Fi) | Yes |
| Receipt printer | USB (Windows only) | Yes |
| Kitchen/bar printer | Network | Phase 2 |
| Label printer | USB / network | Later |

## Reference Device

See [OI-0004 — First Receipt Printer Reference Device](../issues/open/OI-0004-first-receipt-printer-reference-device.md).

## Related Plans

- [PLAN-0005 — Payments, Receipts, Printing](../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
