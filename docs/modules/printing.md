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

See [OI-0004 — First Receipt Printer Reference Device](../issues/closed/OI-0004-first-receipt-printer-reference-device.md).

## Related Plans

- [PLAN-0005 — Payments, Receipts, Printing](../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
- [ADR-0014 — Inter-Module Communication Pattern](../adr/accepted/ADR-0014-inter-module-communication.md) — the Handler I/O Rule this milestone is the first real implementation of.

## Implementation Status (PLAN-0005 Milestone E, 2026-07-06)

ESC/POS printing and the generic outbox/work-item mechanism are implemented. No printer discovery
UI, USB transport, per-location/per-terminal printer routing, MAUI, or hardware/device
orchestration yet — those remain PLAN-0009/a later hardware-integration plan's scope (approved
Human Decision #1).

- **The outbox/work-item mechanism** (`DaxaPos.Domain.Entities.OutboxWorkItem`,
  `OutboxWorkItemStatus`: `Pending`/`Processing`/`Completed`/`Failed`) is generic, not
  printing-specific — `WorkType` (currently only `"PrintReceipt"`) distinguishes what a row
  represents, per ADR-0014's Follow-Up Work ("the two should likely share one outbox table/worker
  pattern rather than being built twice"). A fully-settling payment's
  `OrderLifecycleDomainEvent("Completed")` enqueues a `"PrintReceipt"` row
  (`OrderCompletedPrintOutboxHandler`, `src/DaxaPos.Api/Printing/`) — registered alongside the
  existing `OrderLifecycleAuditHandler` for the same event, ADR-0014's "one event, several
  independent reactors" pattern, not a replacement for it.
- **`DaxaPos.Workers`** (new host project, referenced in CLAUDE.md's suggested solution structure
  but not built until this milestone) polls the outbox (`OutboxProcessorWorker`, a
  `BackgroundService`) and dispatches each item by `WorkType`. The poll query is cross-tenant
  (`IgnoreQueryFilters()`, a documented exception — see `IgnoreQueryFiltersUsageTests`) since the
  worker has no HTTP-derived tenant context of its own; `AmbientTenantContext`/
  `AmbientCurrentTenantProvider` (`DaxaPos.Infrastructure.Identity`) scope each claimed item's
  processing to its own tenant, mirroring what `HttpContextAuthContextAccessor` does for a real
  request.
- **`PrintReceiptOutboxProcessor`** (`src/DaxaPos.Workers/Processing/`) loads the order's
  already-immutable lines/tax snapshots/payments/refunds (the same query shape as
  `ReceiptEndpoints.BuildReceiptDocumentAsync`, deliberately duplicated rather than shared across
  the Api/Workers project boundary — see that class's remarks), renders them via Milestone D's
  `ReceiptRenderer` unchanged, formats ESC/POS bytes, and sends them through `IPrinterTransport`.
  On failure it defers to `OutboxRetryPolicy` (pure, TDD'd first, exponential backoff capped at
  5 minutes) to decide retry-with-backoff vs. permanent `Failed`.
- **ESC/POS byte generation** (`DaxaPos.Application.Printing.EscPosReceiptFormatter`) is pure and
  DB-independent, mirroring `ReceiptRenderer`'s shape — TDD'd first. Proven byte-for-byte: the
  GST-free marker and marker legend survive generation, tax summary/payment/refund lines print
  from the `ReceiptDocument`'s own data, and a cash-drawer kick command
  (`ESC p 0 25 250`) is appended only when the order's payments include a `Cash` payment.
- **The printer transport boundary** (`DaxaPos.Infrastructure.Printing.IPrinterTransport`) has one
  concrete implementation this milestone, `NetworkPrinterTransport` (raw TCP to a single configured
  host/port, conventionally port 9100) — no per-location/per-terminal printer routing table yet;
  USB printing is a Windows POS terminal (MAUI) device capability, not this backend service's
  concern, per CLAUDE.md's device strategy.
- No new endpoints, no `printing.manage` permission code. Printing is fully automatic
  (order completion → outbox → `DaxaPos.Workers`); the plan's own Milestone E scope names a manual
  retry/administration endpoint as optional, not required. `printing.manage` stays reserved for
  whichever future milestone adds one.
- See `docs/plans/active/PLAN-0005-worker-notes.md`'s "Milestone E Report" for full detail and
  deviations.

## Deferred: Location-Scoped Production Printer Routing (required for production, not yet built)

Tracked as [OI-0018](../issues/open/OI-0018-location-scoped-production-printer-routing.md), opened
at PLAN-0005 Milestone F closeout (2026-07-06). Today's single-network-printer behaviour (above) is
correct for a single-counter MVP demo, but production venues need each `Location` to route order
components to its own printers independently:

- Printer routing must be scoped by `Location` — each location's mapping is independent of every
  other location, even within the same tenant/organisation.
- Each order line/component resolves to a production route (e.g. `drinks`, `kitchen`,
  `kitchen-deep-fried`, `dessert`, `coffee`, `bar`).
- Each location maps those routes to its own physical printers as data, not code — e.g. Location A
  might route `drinks` to a bar printer and `kitchen-deep-fried` to a dedicated fryer printer, while
  Location B routes both `kitchen` and `kitchen-deep-fried` to one shared kitchen printer.
- A production docket must contain only the components relevant to that printer/station — a
  materially different document from the whole-order customer receipt (`ReceiptDocument`), which
  remains unchanged and is not filtered client-side to produce dockets.
- Missing/disabled route handling is explicitly undecided — left for whichever plan implements this.

This was deliberately not built in PLAN-0005 (per approved Human Decision #1's hardware/device
scope boundary and Milestone F's own explicit instruction not to add printer routing tables in a
consolidation milestone) — see OI-0018 for the full requirement, options, and open decisions. Admin
UI for configuring routes, printer discovery, USB transport, MAUI, and hardware/provider adapters
remain PLAN-0006/PLAN-0009/a later hardware-integration plan's scope, not this OI's.
