# Deployment: Linux Kiosk (PWA)

Daxa POS uses PWA in Chromium kiosk mode for Linux-based kiosk and counter devices.

---

## Overview

Linux mini PCs, kiosk devices, and embedded computers can run Daxa POS (as a non-MAUI fallback) using a Chromium-based browser in kiosk mode pointing to the Daxa PWA.

This is suitable for:

- Non-Windows POS fallback.
- Self-ordering kiosks.
- KDS screens on Linux devices.
- Admin/back-office on Linux.

---

## Requirements

- Linux (Debian/Ubuntu recommended).
- Chromium or Google Chrome installed.
- Network access to Daxa Local Server or Daxa Cloud.
- For kiosk lockdown: OS-level kiosk configuration (not Daxa application feature).

---

## Kiosk Launch Command

```bash
chromium-browser \
  --kiosk \
  --no-first-run \
  --disable-infobars \
  --disable-session-crashed-bubble \
  --app=https://your-daxa-pos-url.com
```

For kiosk lockdown (prevent user from exiting):

- Configure auto-login with a restricted user account.
- Use `openbox` or similar WM with kiosk profile.
- Disable keyboard shortcuts via Chromium policy.

This is an OS and browser deployment concern, not a Daxa application feature.

---

## Printers and Hardware

- Network receipt printers are accessible from Linux via IP address.
- USB printers require a Linux-compatible driver or CUPS.
- Payment terminal integration depends on the provider's Linux SDK or HTTP API.
- Barcode scanners: keyboard-wedge input works in any browser.

---

## Related Documents

- [Architecture: Device Strategy](../architecture/device-strategy.md)
- [ADR-0004 — Windows MAUI and PWA Device Strategy](../adr/accepted/ADR-0004-windows-maui-and-pwa-device-strategy.md)
