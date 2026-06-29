# OI-0009 — MAUI App Update Delivery

## Status

Open

## Area

Devices / Deployment

## Summary

How will Daxa Terminal (the .NET MAUI Windows POS app) receive and apply updates in production venues?

## Context

Daxa Terminal runs on Windows POS devices in venues. Unlike PWA (which auto-updates via the browser), a MAUI app is a native Windows application that requires explicit update delivery.

Venues may have limited IT support. POS terminal updates must be reliable, testable, and not interrupt trading hours. Some venues may want to control when updates are applied.

## Impact

- Determines the MAUI app distribution and update mechanism.
- Affects deployment documentation and venue IT guidance.
- Affects CI/CD pipeline (build artefacts).
- Affects testing workflow (signed builds required).

## Options

1. **MSIX package + Windows Package Manager (winget)** — Modern Windows distribution. Supports auto-update via MSIX. Requires code signing certificate.
2. **MSIX package + Daxa-hosted AppInstaller feed** — Daxa hosts an `.appinstaller` XML file. Windows can auto-check for updates. No store required.
3. **Windows Store (MSIX)** — Managed distribution. Subject to Store review process. May not be suitable for B2B POS.
4. **Custom updater (self-check on startup)** — App checks Daxa API for new version and downloads installer. Full control. More engineering effort.
5. **IT-managed deployment (Intune / SCCM / PDQ)** — Enterprise customers deploy via MDM. Requires MSIX or MSI packages. No auto-update for unmanaged venues.

## Recommendation

**MSIX + Daxa-hosted AppInstaller feed** for most venues. Enterprise customers can use MDM (Option 5). Requires a code signing certificate from the start.

## Decision Needed

- Update delivery mechanism for production MAUI app.
- Code signing certificate strategy.
- Whether updates are mandatory or deferred.

## Related ADRs

- [ADR-0004 — Windows MAUI and PWA Device Strategy](../../adr/proposed/ADR-0004-windows-maui-and-pwa-device-strategy.md)

## Related Documents

- [Deployment: Windows Terminal](../../deployment/windows-terminal.md)
- [PLAN-0006 — Terminal, Display, PWA](../../plans/active/PLAN-0006-terminal-display-pwa-planning.md)
