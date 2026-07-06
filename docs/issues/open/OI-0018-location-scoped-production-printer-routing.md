# OI-0018 — Location-Scoped Production Printer Routing

## Status

Open

## Area

Printing / Devices

## Summary

PLAN-0005 Milestone E shipped exactly one configured network printer per deployment
(`NetworkPrinterTransport`, a single host/port) — every completed order's receipt prints to that
one printer, with no concept of per-location or per-station (drinks/kitchen/bar/dessert/etc.)
routing. Production use requires each `Location` to independently map its own order-line
"production routes" to its own physical printers.

## Context

Flagged as a known follow-up in PLAN-0005 Milestone E's own report ("Printer transport is
network-only; a single configured host/port, not a per-location/per-terminal printer routing
table") and explicitly reserved for Milestone F to close, defer, or document (not implement) —
see `docs/plans/active/PLAN-0005-worker-notes.md`'s Milestone E Report, "Blockers before
Milestone F". Milestone F's own human decision record confirms this stays deferred: required for
production, but out of scope for PLAN-0005.

## Requirements (recorded for whichever plan implements this)

- Printer routing must be scoped by `Location` — each location configures its own routing
  independently of every other location in the same tenant/organisation.
- Each order line/component must resolve to a production route, e.g.:
  - `drinks`
  - `kitchen`
  - `kitchen-deep-fried`
  - `dessert`
  - `coffee`
  - `bar`
- Each location maps those production routes to its own printers — the mapping is data, not code.
  Example: Location A routes `drinks` → its bar printer, `kitchen` → its kitchen printer,
  `kitchen-deep-fried` → its fryer printer; Location B routes both `kitchen` and
  `kitchen-deep-fried` to one shared kitchen printer, and `drinks` to its own drinks printer.
- Production dockets must contain only the order components relevant to that printer/station — not
  the whole order (this is a materially different document from the whole-order customer receipt
  already built by PLAN-0005 Milestone D/E; the two must stay independent renderers/outputs, not
  the same document filtered client-side).
- Customer-facing receipts remain whole-order documents (`ReceiptDocument`/`EscPosReceiptFormatter`,
  unchanged) — production docket routing is additive, not a replacement.
- Missing/disabled route handling (an order line whose product has no configured production route
  at that location, or a configured route pointing at a disabled/unreachable printer) is explicitly
  undecided — must be resolved by whichever plan implements this, not assumed here.
- `PrinterDevice`/routing configuration continues to build on PLAN-0003's existing `Device` entity
  (`DeviceType.Printer`) per Milestone E's own precedent — no evidence yet that a second
  printer-specific table is needed, but the implementing plan should re-check, not assume.

## Explicitly Out of Scope Here (belongs to PLAN-0009 or a later hardware/device plan)

- Printer discovery.
- USB transport (a Windows POS terminal/MAUI device capability per CLAUDE.md's device strategy,
  not this backend service's).
- MAUI application changes.
- Daemon/installer/runtime deployment mechanics.
- Provider-specific hardware adapters.
- Admin UI for configuring printer routes (likely PLAN-0006 back-office work, or a dedicated
  follow-up plan, once this OI's data model exists for it to configure).

## Impact

- Every venue with more than one production station (the common case for cafes, restaurants, pubs)
  currently gets a single printer for the whole order — usable for a single-counter MVP demo, not
  for real kitchen/bar operations.
- No data loss or correctness risk: this is a missing feature, not a bug in existing behaviour.
  `NetworkPrinterTransport`'s single-printer behaviour is accurate to what Milestone E documented
  and shipped.

## Options

1. Add a `ProductionRoute` (or similarly named) enum/lookup and a `Location`-scoped
   `ProductionRoutePrinterMapping` table (`LocationId`, `ProductionRoute`, `DeviceId`), resolved at
   outbox-processing time by `DaxaPos.Workers` alongside the existing whole-order receipt print.
2. Model routes as free-form location-configured strings (mirroring `TaxCategory.Code`'s existing
   precedent) rather than a closed enum, since venues' station names vary (a food truck's "kitchen"
   isn't a pub's "kitchen").
3. Decide docket-vs-receipt document generation: a new `DaxaPos.Application.Printing` rendering
   type (e.g. `ProductionDocketDocument`) parallel to `ReceiptDocument`, or a filtered view over the
   same order-line data with its own ESC/POS formatter.

## Recommendation

Not made here — this OI exists to track and scope the requirement, per Milestone F's explicit
instruction not to implement production printer routing tables in this milestone. The implementing
plan should evaluate Options 1–3 against real venue configuration needs at that time.

## Decision Needed

- Which plan implements this (a PLAN-0005 follow-up, part of PLAN-0006's back-office/terminal work,
  or its own dedicated plan) and when.
- Closed-enum vs free-form string route naming (Option 1 vs 2 above).
- Missing/disabled route handling behaviour.

## Related Documents

- [PLAN-0005 — Payments, Receipts, Printing](../../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
- [PLAN-0005 worker notes — Milestone E report, deferred printer routing](../../plans/active/PLAN-0005-worker-notes.md)
- [Module: Printer Service](../../modules/printing.md)
- [ADR-0014 — Inter-Module Communication Pattern](../../adr/accepted/ADR-0014-inter-module-communication.md)
