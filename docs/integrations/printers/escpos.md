# Integration: ESC/POS Printer Protocol

## Overview

ESC/POS is the de facto standard command protocol for thermal receipt printers. It is used by Epson, Star Micronics, Citizen, and most other commercial receipt printer brands.

Daxa POS uses ESC/POS for receipt printing and cash drawer kick commands.

---

## Connection Methods

| Method | Notes |
|--------|-------|
| Network (Ethernet/Wi-Fi) | Primary for Daxa Local and Daxa Hybrid. Printer on same local network. |
| USB | Supported on Windows POS terminals (Daxa Terminal). |
| Bluetooth | Optional — less common for fixed counter printers. |

---

## Common Commands

| Command | Function |
|---------|---------|
| `ESC @` | Initialize printer |
| `ESC a n` | Text alignment (left, centre, right) |
| `ESC ! n` | Text formatting (bold, double height, etc.) |
| `LF` | Line feed |
| `GS V m` | Cut paper (full or partial cut) |
| `ESC p m t1 t2` | Cash drawer kick (via printer port) |

---

## Reference Device

See [OI-0004 — First Receipt Printer Reference Device](../../issues/closed/OI-0004-first-receipt-printer-reference-device.md).

Recommended initial reference: Epson TM-T88VI (80mm, network, full ESC/POS support).

---

## Receipt Width

- 80mm paper: 42–48 characters per line (depending on font and printer).
- 58mm paper: ~32 characters per line.

Daxa receipt templates should target 42 characters per line for 80mm compatibility.

---

## Cash Drawer

Cash drawers connect via the printer's RJ11/RJ12 port. The drawer kick command is sent through the printer, not a separate device driver.

```text
ESC p 0 100 200   → Drawer 0, pulse duration
ESC p 1 100 200   → Drawer 1
```

---

## Libraries

Potential .NET ESC/POS libraries:

- `ESCPOS-ThermalPrinter-NetCore` (open source).
- Custom command builder (straightforward for basic receipt printing).

---

## Related Documents

- [Module: Printing](../../modules/printing.md)
- [Module: Receipts](../../modules/receipts.md)
- [OI-0004 — First Receipt Printer Reference Device](../../issues/closed/OI-0004-first-receipt-printer-reference-device.md)
- [PLAN-0005 — Payments, Receipts, Printing](../../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
