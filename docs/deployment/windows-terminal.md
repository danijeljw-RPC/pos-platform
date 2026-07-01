# Deployment: Windows POS Terminal (Daxa Terminal)

Daxa Terminal is the .NET MAUI Windows POS application.

---

## Overview

Daxa Terminal runs on Windows POS devices at the counter. It connects to a Daxa Local server or Daxa Cloud API.

A second MAUI window (Daxa Display) is shown on the customer-facing display.

---

## Requirements

- Windows 10 or Windows 11 (64-bit).
- .NET MAUI runtime installed (or bundled with the MSIX package).
- Network access to Daxa Local Server or Daxa Cloud API.
- EFTPOS terminal connected (via USB, Bluetooth, or network — depends on provider).
- Receipt printer connected (USB or network).
- Cash drawer (connected via receipt printer port).
- Optional: barcode scanner (keyboard-wedge input).

---

## Installation

App delivery method is pending decision. See [OI-0009 — MAUI App Update Delivery](../issues/closed/OI-0009-maui-app-update-delivery.md).

Recommended: MSIX package + Daxa-hosted AppInstaller feed.

---

## First-Time Setup

1. Install Daxa Terminal via MSIX package.
2. Launch app — device registration screen appears.
3. Enter venue code or scan registration QR from Daxa Back Office.
4. Device registers and downloads initial configuration.
5. Staff PIN login screen appears.

---

## Kiosk / Assigned Access (Optional)

For venues that want the POS to lock down to Daxa Terminal only:
- Use Windows Assigned Access (single-app kiosk mode) via Group Policy or Intune.
- This is an OS-level configuration, not a Daxa application feature.

---

## Customer Display Setup

- Connect a secondary monitor to the Windows POS machine.
- Daxa Terminal automatically opens Daxa Display on the secondary monitor on launch.
- If no secondary monitor is detected, the display window is not shown.

---

## Related Documents

- [Architecture: Device Strategy](../architecture/device-strategy.md)
- [ADR-0004 — Windows MAUI and PWA Device Strategy](../adr/accepted/ADR-0004-windows-maui-and-pwa-device-strategy.md)
- [OI-0009 — MAUI App Update Delivery](../issues/closed/OI-0009-maui-app-update-delivery.md)
- [PLAN-0006 — Terminal, Display, PWA](../plans/active/PLAN-0006-terminal-display-pwa-planning.md)
