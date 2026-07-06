# PLAN-0005 - Payments, Receipts, and Printing

## Status

**Complete.** PLAN-0005 finished on 2026-07-06.

Closeout state:

- Milestones A-F complete.
- 1052/1052 tests passing at closeout.
- 17 migrations apply cleanly from an empty database.
- OI-0017 remains open.
- OI-0018 was created during closeout for location-scoped production printer routing.
- `printing.manage` remains reserved but unimplemented because no print administration endpoint
  exists yet.
- Plan remains in `docs/plans/active/` until the repository defines and applies a completed-plan
  archival convention.

## Goal

Implement the commercial transaction foundation:

- Orders.
- Cash/manual EFTPOS payments.
- Payment ledger.
- Refunds.
- Receipt generation.
- ESC/POS receipt printing.
- Outbox/worker foundation for external I/O.

## Completed Milestones

| Milestone | Status | Summary |
|-----------|--------|---------|
| A | Done | Order service foundation, order lines, tax snapshots, order number counter. |
| B | Done | Cash/manual EFTPOS payment foundation, payment ledger, payment adapter interface. |
| C | Done | Refund service foundation, full/partial refunds, over-refund rejection. |
| D | Done | Receipt document generation, tax markers, receipt/reprint endpoints. |
| E | Done | Generic outbox/work-item mechanism, worker host, ESC/POS receipt printing. |
| F | Done | RBAC/Staff PIN sweep, migration verification, docs closeout, OI-0018 created. |

## Implemented Scope

- `orders.manage` order lifecycle endpoints.
- Location-scoped order number counter.
- Order-line tax snapshots.
- ADR-0006 20-distinct-tax-component-per-order enforcement.
- `payments.record` cash/manual EFTPOS payment endpoints.
- Payment ledger entries.
- `IPaymentTerminalProvider` interface and placeholder DTOs.
- `payments.refund` refund endpoints.
- Refund over-refund rejection.
- Receipt rendering from immutable order/payment/refund/tax data.
- `receipts.reprint` audited reprint endpoint.
- `OutboxWorkItem` and `OutboxWorkItemStatus`.
- `DaxaPos.Workers`.
- `OutboxProcessorWorker`.
- `PrintReceiptOutboxProcessor`.
- `OutboxRetryPolicy`.
- `EscPosReceiptFormatter`.
- `IPrinterTransport` and `NetworkPrinterTransport`.

## Non-Goals / Deferred Work

- Integrated payment provider implementations. First integration remains PLAN-0009.
- Gift cards and store credit.
- Split bills beyond the payment foundation's ability to record multiple payments.
- Production kitchen/bar/dessert docket routing. Tracked by OI-0018.
- Printer discovery.
- USB/local Windows printer support. Deferred to a future MAUI terminal/hardware plan.
- Print-job administration/retry endpoint.
- `printing.manage` implementation.
- Full UI. PLAN-0006 is PWA-first.

## Permission Catalogue Additions

| Code | Category | Implemented | Staff PIN posture |
|------|----------|-------------|-------------------|
| `orders.manage` | Operational | Yes | Staff-PIN-eligible |
| `payments.record` | Operational | Yes | Staff-PIN-eligible |
| `payments.refund` | AdminSensitive | Yes | Staff PIN rejected |
| `receipts.reprint` | Operational | Yes | Staff-PIN-eligible |
| `printing.manage` | AdminSensitive | Reserved only | Not implemented |

`printing.manage` should be added only when print-job retry/administration endpoints exist.

## Key Decisions

- PLAN-0005 owns the domain/application abstractions needed for payment, receipt, print-job, and
  order-transaction flow.
- PLAN-0009 or a later hardware/device plan owns provider-specific hardware adapters, printer
  discovery, device protocols, installer/runtime mechanics, USB transport, and terminal-local
  hardware concerns.
- Order numbers are server-side, database-backed, location-scoped, monotonic, and safe under
  concurrent order creation.
- OI-0017 stays open as a tracked risk. PLAN-0005 did not fix product archive-and-replace
  concurrency.
- Refund permission is separate from normal payment/order permissions and is manager/admin-only by
  default.
- Receipt reprint and print administration are separate permission surfaces.

## Historical Milestone Notes

The implementation reports are retained in
[PLAN-0005 worker notes](PLAN-0005-worker-notes.md).

## Historical Planning Sections

Earlier versions of this plan contained active planning sections named:

- Tests To Run Later.
- Documentation To Update.
- ADRs Required.
- Open Issues Required.
- Commit Sequence.
- Human Decisions Needed.

Those sections are now historical. The plan is complete, and future sessions should not treat that
old wording as open implementation instruction. The authoritative current state is the status and
completed-scope summary above plus the detailed worker notes.

## Handoff

Next active product track:

- [PLAN-0006 - Terminal, Display, and PWA](PLAN-0006-terminal-display-pwa-planning.md)

Important open issues:

- [OI-0017 - Product archive-and-replace concurrency](../../issues/open/OI-0017-product-archive-and-replace-concurrency.md)
- [OI-0018 - Location-scoped production printer routing](../../issues/open/OI-0018-location-scoped-production-printer-routing.md)
