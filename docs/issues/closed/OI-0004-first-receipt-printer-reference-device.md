# OI-0004 — First Receipt Printer Reference Device

## Status

Closed

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

---

## Decision Addendum

OI-0004 is resolved.

The first receipt printer reference device for Daxa POS will be the **Epson TM-T88 series**, with the **Epson TM-T88VI** used as the initial reference model where available.

The integration must remain ESC/POS-compatible and not be locked to a single printer vendor.

## Decision

Daxa POS will use an ESC/POS printer abstraction.

The first tested commercial reference printer is:

```text
Reference printer family: Epson TM-T88 series
Initial reference model: Epson TM-T88VI
Paper width: 80mm
Primary connection: Network / Ethernet
Secondary connection: USB where required
Protocol: ESC/POS
```

The Epson TM-T88VI is selected as the first practical test target because it is a common commercial receipt printer and provides a good baseline for ESC/POS receipt formatting, printer commands, and cash drawer kick testing.

## Vendor-Agnostic Rule

The printer implementation must not be written as an Epson-only integration.

The system should model receipt printing as:

```text
Daxa Receipt Renderer
    ↓
Printer Driver / Transport
    ↓
ESC/POS Command Output
    ↓
Network, USB, or future transport
```

The Epson TM-T88VI is the first reference device, not the only supported device.

Future supported printers may include:

- Epson TM-T88 variants.
- Epson TM-T20 variants.
- Star Micronics printers.
- Citizen printers.
- Generic ESC/POS-compatible 80mm printers.
- Other certified thermal receipt printers.

## MVP Connection Method

Network/Ethernet printing should be the primary MVP target.

Network printing is preferred because:

- It works naturally with local server deployments.
- Multiple terminals can share the same printer.
- Kitchen/bar printer routing is easier.
- It avoids direct USB driver installation on every POS terminal.
- It fits local/hybrid venue deployments where printers are attached to the venue network.

USB printing may be supported as a secondary path where a printer is directly attached to a Windows POS terminal or local hardware device.

## Cash Drawer

Cash drawer kick should be supported through the receipt printer where the printer provides a cash drawer port.

Cash drawer support is useful for MVP because cash is an MVP payment mode.

However, cash drawer support should be treated as a printer capability, not a hard requirement for all receipt printers.

The system should allow configuration such as:

```text
PrinterSupportsCashDrawerKick: true
CashDrawerKickEnabled: true
CashDrawerKickCommandProfile: StandardEscPos
```

## Receipt Width

The first receipt templates should target 80mm paper.

The receipt renderer should not assume every printer is 80mm forever.

Printer profile settings should allow future support for:

- 80mm receipts.
- 58mm receipts.
- Wider or custom formats where needed.
- Digital receipt rendering.
- PDF receipt rendering.

## Configuration

Receipt printer configuration should be location/device specific.

A location may have multiple printers, such as:

- Front counter receipt printer.
- Bar printer.
- Kitchen printer.
- Coffee printer.
- Label printer later.

A printer profile should include:

```text
PrinterName
PrinterRole
PrinterModel
ConnectionType
NetworkAddress
Port
PaperWidth
CommandProfile
CashDrawerKickEnabled
LocationId
DeviceId, if device-specific
```

## Testing Expectations

The reference printer should be used to validate:

- Basic receipt printing.
- Line wrapping.
- 80mm formatting.
- Tax marker display.
- Totals and payment lines.
- Refund/void receipt variants.
- Logo/image support if required later.
- QR code support if implemented later.
- Cash drawer kick.
- Printer offline/error behaviour.

## Consequences

This gives Daxa POS a real tested printer target without making the system vendor-locked.

The first production integration can be tested against a known commercial printer while the architecture remains open to other ESC/POS-compatible printers.

## Status Update

This open issue is resolved by selecting the Epson TM-T88 series, initially the Epson TM-T88VI, as the first receipt printer reference device while keeping the implementation ESC/POS and vendor agnostic.

Status: **Closed**
