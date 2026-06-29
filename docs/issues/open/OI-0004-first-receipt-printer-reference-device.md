# OI-0004 — First Receipt Printer Reference Device

## Status

Open

## Area

Devices / Hardware

## Summary

Which receipt printer model should be the first supported and tested reference device for Daxa POS?

## Context

Daxa POS requires thermal receipt printing via ESC/POS. Most hospitality and retail receipt printers support ESC/POS over network (Ethernet or Wi-Fi) or USB. A reference device is needed to guide driver development, integration testing, and customer deployment guidance.

## Impact

- Determines the first ESC/POS printer integration to test.
- Affects receipt template width (typically 80mm or 58mm paper width).
- Affects cash drawer kick implementation (signal via printer port).
- Informs deployment documentation.

## Options

1. **Epson TM-T88VI** — Industry standard, 80mm, network + USB, excellent ESC/POS compatibility.
2. **Star Micronics TSP143** — Popular, 80mm, network + USB + Bluetooth.
3. **Epson TM-T20III** — Budget 80mm, network + USB, good for simpler deployments.
4. **Citizen CT-S310II** — 80mm, reliable, common in hospitality.
5. **Generic 80mm thermal printer** — Very low cost, basic ESC/POS support only.

## Recommendation

**Epson TM-T88VI** as the reference device. It is the most widely used commercial receipt printer in AU/NZ hospitality and retail, has comprehensive ESC/POS support, and is well-documented.

## Decision Needed

- Which printer model is the first reference device.
- Whether USB or network is the primary connection method for MVP.
- Whether a cash drawer is required for MVP.

## Related ADRs

None directly. Printing implementation covers ADR-0011 (receipt tax markers).

## Related Documents

- [Module: Printing](../../modules/printing.md)
- [Integration: ESC/POS](../../integrations/printers/escpos.md)
- [PLAN-0005 — Payments, Receipts, Printing](../../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
