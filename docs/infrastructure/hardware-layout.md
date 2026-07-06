# Hardware Layout — Daxa POS

## Purpose

This document describes typical hardware layouts for Daxa POS deployments.

Daxa POS supports:

- Daxa Cloud
- Daxa Local
- Daxa Hybrid
- Windows MAUI staff terminals
- Windows MAUI customer displays
- PWA KDS devices
- PWA admin devices
- Payment terminals
- Printers
- Cash drawers
- Barcode/QR scanners

The system should treat client devices as replaceable. If a device fails, another device should be registered and recover state from the server.

---

## Hardware Principles

### 1. Clients are disposable

A POS terminal, KDS screen, tablet, or customer display should not be the source of truth.

If a device fails:

```text
Replace device
↓
Register/login device
↓
Recover state from server
```

### 2. Server state is authoritative

For Daxa Cloud, server state is in the cloud.

For Daxa Local, server state is on the local server.

For Daxa Hybrid, the local server is authoritative during local trading and syncs to cloud.

### 3. Device identity and user identity are separate

A physical terminal has a device registration.

A staff member has a user/staff login.

Do not combine these concepts.

### 4. Hardware should be configurable

Printers, payment terminals, cash drawers, customer displays, and KDS stations must be configured server-side.

---

## Typical Small Venue — Cloud Only

Suitable for:

- Small cafe
- Small bakery
- Small retail store
- Cake counter
- Simple service business

### Layout

```text
Internet
↓
Daxa Cloud
↓
Venue network
├─ Windows POS terminal running Daxa Terminal
├─ Optional second screen running Daxa Display
├─ Receipt printer
├─ Payment terminal
├─ Barcode scanner optional
└─ Back Office browser/PWA device
```

### Notes

- No local server required.
- Works best with reliable internet.
- Manual fallback procedure should exist if internet fails.
- Payment provider offline capability depends on provider.

---

## Typical Small Venue — Local or Hybrid

Suitable for:

- Cafe
- Bakery
- Pub
- Small restaurant
- Food truck with local mini PC
- Remote venue with unreliable internet

### Layout

```text
Venue network
├─ Daxa Local Server / mini PC
├─ Router/firewall
├─ Network switch
├─ Wi-Fi access point(s)
├─ Windows POS terminal running Daxa Terminal
├─ Customer-facing second screen running Daxa Display
├─ Receipt printer
├─ Kitchen/bar printer optional
├─ Payment terminal
├─ Barcode/QR scanner optional
└─ Back Office browser/PWA device

Optional:
Daxa Cloud sync/backup/reporting
```

### Notes

- Local server should be wired.
- POS LAN should be separate from guest Wi-Fi.
- Printers should not be on guest network.
- Local server should use UPS where possible.
- Hybrid mode syncs to cloud when available.

---

## Typical Hospitality Venue

Suitable for:

- Pub
- Restaurant
- Bar
- Cafe with kitchen
- Bakery with prep area

### Hardware kit

- 1 local POS server / mini PC for local or hybrid deployments
- 1 router/firewall
- 1 network switch
- Wi-Fi access point(s)
- 1–5 POS terminals
- 1–5 KDS screens
- 1–5 network thermal printers
- 1+ cash drawers
- 1+ payment terminals
- Optional barcode/QR scanners
- Optional customer displays

### Layout

```text
Daxa Local Server / Cloud API
↓
Venue network
├─ POS Terminal 1
│  ├─ Daxa Terminal
│  ├─ Daxa Display
│  ├─ Receipt Printer
│  ├─ Cash Drawer
│  └─ Payment Terminal
│
├─ POS Terminal 2
│  ├─ Daxa Terminal
│  ├─ Receipt Printer
│  └─ Payment Terminal
│
├─ KDS Kitchen
├─ KDS Bar
├─ KDS Coffee
├─ Kitchen Printer
├─ Bar Printer
└─ Back Office Device
```

---

## Typical Retail Store

Suitable for:

- Clothing store
- Electronics store
- General retail
- Gift shop
- Computer store

### Hardware kit

- 1–3 POS terminals
- Barcode scanners
- Receipt printers
- Cash drawers
- Payment terminals
- Optional label printer
- Optional customer display
- Optional local server for hybrid/local mode

### Layout

```text
Daxa Cloud or Daxa Local Server
↓
Store network
├─ Front Counter POS
│  ├─ Daxa Terminal
│  ├─ Daxa Display optional
│  ├─ Barcode scanner
│  ├─ Receipt printer
│  ├─ Cash drawer
│  └─ Payment terminal
│
├─ Back Counter POS optional
├─ Label printer optional
└─ Back Office browser/PWA
```

---

## Typical Food Truck

Suitable for:

- Food truck
- Market stall
- Festival stall
- Event pop-up

### Hardware kit

- Windows POS tablet/terminal or tablet PWA
- Optional local mini PC/server
- Portable router/hotspot
- Payment terminal
- Portable receipt printer
- Optional kitchen/prep screen
- Optional battery/UPS

### Layout — Cloud only

```text
Mobile internet/hotspot
↓
Daxa Cloud
↓
POS tablet/terminal
├─ Payment terminal
└─ Portable printer
```

### Layout — Local/hybrid

```text
Food truck local network
├─ Daxa Local Server / mini PC
├─ POS terminal/tablet
├─ Payment terminal
├─ Portable printer
└─ Optional prep display

Sync to Daxa Cloud when internet available
```

### Notes

- Offline/local resilience is important.
- Event/location tagging should be supported.
- Stock countdown should be supported.
- Payment offline capability depends on provider.

---

## Device Recommendations

### POS Terminals

Recommended options:

- Commercial Windows touchscreen POS terminal.
- Windows all-in-one touchscreen.
- Windows tablet with dock.
- Desktop browser for admin/back office.
- PWA tablet only where Windows MAUI is not required.

For Windows POS:

```text
Daxa Terminal = .NET MAUI
Daxa Display = second MAUI window on customer-facing monitor
```

### Customer Display

Options:

- Second HDMI/DisplayPort/USB-C monitor attached to Windows POS.
- Integrated customer display on commercial POS terminal.
- Small touchscreen or display facing customer.

Daxa Display should show:

- Order items.
- Total.
- Discounts.
- Surcharges.
- Payment prompt.
- Payment result.
- Receipt QR later.
- Loyalty prompt later.
- Idle branding/promos.

### KDS

Options:

- iPad in enclosure.
- Commercial Android touch display.
- Linux touch panel.
- Touchscreen monitor plus mini PC.
- Windows display if required.

iPads are acceptable for small-to-medium KDS deployments, especially drinks, coffee, expo/pass, and low-to-medium heat environments.

Industrial/commercial displays are preferred for hot, greasy, steam-heavy, high-abuse kitchen areas.

### Printers

Prefer Ethernet ESC/POS printers.

Printer types:

- Receipt printer.
- Kitchen printer.
- Bar printer.
- Label printer later.
- Portable printer for food trucks.

Avoid exposing printers to guest Wi-Fi.

### Cash Drawers

Prefer drawers connected to receipt printers.

Flow:

```text
Daxa Terminal
↓
Daxa API / Local Server
↓
Print service
↓
Receipt printer
↓
Cash drawer kick
```

Cash drawer openings must be audited.

### Payment Terminals

Supported providers should be configurable.

Initial AU/NZ provider targets:

- Tyro
- Zeller
- Square Terminal
- Stripe Terminal
- Windcave

Later:

- Adyen
- Worldline
- Global Payments

Payment terminals should be mapped to Daxa terminals.

Example:

```text
Venue: Main Street Bakery
Daxa Terminal: Front Counter 1
Payment Provider: Zeller
Payment Terminal: Zeller terminal FRONT-01
```

### Barcode/QR Scanners

First implementation should support keyboard-wedge scanners.

Use cases:

- Retail SKU scanning.
- Gift card QR scanning later.
- Voucher scanning later.
- Customer loyalty QR later.
- Repair/device labels later.

---

## Network Recommendations

### POS LAN

Recommended:

- Separate POS LAN from guest Wi-Fi.
- Local server wired.
- Payment terminals on approved secure network path.
- Printers wired where possible.
- Wi-Fi only for devices that require it.
- Router/firewall controlled.
- UPS for server/network switch where possible.

### Guest Wi-Fi

Guest Wi-Fi must not access:

- Local server.
- Printers.
- POS terminals.
- Payment terminals unless explicitly required by provider.
- KDS devices.

---

## Device Registration

Every device should be registered in Daxa.

Device configuration should include:

```text
Device name
Device type
Tenant
Organisation
Location
Terminal assignment
Printer assignment
Payment terminal assignment
Customer display assignment
KDS station assignment if applicable
Status
Last seen
App version
```

Device types:

- Daxa Terminal
- Daxa Display
- Daxa KDS
- Daxa Back Office
- Daxa Kiosk
- Daxa Local Server
- Worker/service

---

## Cloud, Local, and Hybrid Hardware

### Cloud only

Requires:

- Internet.
- POS terminal/tablet.
- Payment terminal.
- Printer.
- Optional customer display.

Does not require:

- Local server.

### Local

Requires:

- Local server/mini PC.
- Local database.
- Local network.
- POS terminals.
- Printers.
- Payment terminals.

Optional:

- Cloud backup.
- Cloud reporting.
- Remote support.

### Hybrid

Requires:

- Local server/mini PC.
- Cloud account.
- Sync service.
- Local network.
- POS terminals.
- Printers.
- Payment terminals.

Provides:

- Local trading resilience.
- Cloud reporting.
- Central config.
- Backup/export.
- Multi-location visibility.

---

## Replacement and Recovery Rule

Clients are disposable.

The recovery design should be:

```text
Device fails
↓
Disable old device if needed
↓
Register replacement device
↓
Assign same terminal role
↓
Recover state from Daxa Cloud or Daxa Local Server
```

Do not rely on unrecoverable client-local state.

---

## Hardware Open Questions

Create open issues for unresolved questions such as:

- Which Windows POS terminal hardware is the MVP reference device?
- Which receipt printer model is the MVP reference device?
- Which cash drawer model is the MVP reference device?
- Which payment terminal is the first integrated provider test device?
- Should local server run Linux only or Windows also?
- Should food truck local mode require a mini PC or support terminal-only offline cache?
- Which barcode scanner model should be used for testing?
