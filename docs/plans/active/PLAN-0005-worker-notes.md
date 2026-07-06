# PLAN-0005 Worker Notes

## Status

PLAN-0005 is complete.

Closeout state:

- Completed on 2026-07-06.
- Final closeout commits: Milestone F test/docs closeout.
- 1052/1052 tests passing.
- 17 migrations verified from an empty database.
- No code changes in Milestone F.
- OI-0017 remains open.
- OI-0018 created and remains open.

This file is now historical implementation trace. It should not be treated as an active prompt or
open work queue.

## Completed Milestone Reports

### Milestone A - Order Service Foundation

Implemented:

- `Order`.
- `OrderLine`.
- `OrderLineModifier`.
- `OrderLineTax`.
- `OrderNumberCounter`.
- Order lifecycle endpoints.
- `orders.manage`.
- Order tax aggregation.
- Location-scoped monotonic order numbers.

Verification:

- 967/967 tests passing after Milestone A.
- 13 migrations verified clean from empty.

Important notes:

- Order lines snapshot product/tax/receipt-marker details at sale time.
- `OrderNumberCounter` uses a database-backed atomic counter, not `MAX(OrderNumber) + 1`.
- ADR-0006 20-distinct-tax-component-per-order limit is enforced here.
- OI-0017 remained open.

### Milestone B - Payment Foundation

Implemented:

- `Payment`.
- `PaymentLedgerEntry`.
- `PaymentSettlement`.
- `IPaymentTerminalProvider`.
- Cash payment recording.
- Manual EFTPOS payment recording.
- `payments.record`.

Verification:

- 985/985 tests passing after Milestone B.
- 14 migrations verified clean from empty.

Important notes:

- Payments record against persisted order totals.
- Overpayment is rejected.
- Fully settled orders transition to `Completed`.
- `IPaymentTerminalProvider` has no concrete adapter yet. PLAN-0009 owns the first integration.

### Milestone C - Refund Service

Implemented:

- `Refund`.
- `RefundSettlement`.
- Refund endpoints.
- `payments.refund`.
- Refund audit handler.
- Server-side over-refund rejection.

Verification:

- 1001/1001 tests passing after Milestone C.
- 15 migrations verified clean from empty.

Important notes:

- `payments.refund` is `AdminSensitive`.
- Staff PIN sessions are rejected for refund endpoints.
- Refunds link back to original payment/order.
- No provider-specific refund logic was added.

### Milestone D - Receipt Generation

Implemented:

- `ReceiptDocument`.
- Receipt input/model types.
- `ReceiptRenderer`.
- Receipt view endpoint.
- Receipt reprint endpoint.
- `receipts.reprint`.
- Reprint audit event.

Verification:

- 1017/1017 tests passing after Milestone D.
- 16 migrations verified clean from empty.

Important notes:

- Receipt generation uses immutable order/payment/refund/tax snapshots.
- Receipt generation does not recalculate tax or pricing.
- Customer receipts are whole-order documents.
- Receipt generation is separate from printing.

### Milestone E - Printing Foundation

Implemented:

- `OutboxWorkItem`.
- `OutboxWorkItemStatus`.
- `OutboxRetryPolicy`.
- `EscPosReceiptFormatter`.
- `PrintReceiptWorkPayload`.
- `IPrinterTransport`.
- `NetworkPrinterTransport`.
- `AmbientTenantContext`.
- `AmbientCurrentTenantProvider`.
- `OrderCompletedPrintOutboxHandler`.
- `DaxaPos.Workers`.
- `OutboxProcessorWorker`.
- `PrintReceiptOutboxProcessor`.

Verification:

- 1035/1035 tests passing after Milestone E.
- 17 migrations verified clean from empty.

Important notes:

- Milestone E is the first concrete implementation of ADR-0014's Handler I/O Rule.
- Printing is automatic: completed order -> outbox -> worker -> transport.
- Network printing is server-side.
- USB/local Windows printing was not implemented.
- No endpoints were added.
- `printing.manage` was not added because no print administration endpoint exists.
- One documented `IgnoreQueryFilters()` exception was added for the worker polling loop.

### Milestone F - Consolidation, RBAC Sweep, Documentation Closeout

Implemented:

- Extended RBAC inventory coverage for PLAN-0005 endpoints.
- Extended Staff PIN rejection coverage for refund endpoints.
- Added no-permission/device-token sweeps for uncovered order/payment/receipt endpoints.
- Added permission-category classification proof for PLAN-0005 permissions.
- Re-verified all migrations.
- Re-ran `IgnoreQueryFilters()` guard.
- Created OI-0018 for production printer routing.
- Updated printing docs.

Verification:

- 1052/1052 tests passing.
- 17 migrations verified clean from empty.
- No new `IgnoreQueryFilters()` call sites.
- No `src/` changes.

Milestone E follow-ups:

- `deploy/docker-compose.yml` worker service entry deferred.
- Location-scoped production printer routing filed as OI-0018.

## Final Known Open Items

### OI-0017

Product archive-and-replace concurrency remains open.

PLAN-0005 did not fix it because no milestone exposed a direct blocker.

### OI-0018

Location-scoped production printer routing remains open.

Required future work:

- Product production-route field.
- Combo/component production expansion.
- Location-scoped route-to-printer mapping.
- Production docket document model.
- Production print outbox/work items.
- Back Office configuration/warnings.

### `printing.manage`

Reserved but unimplemented.

Only add this permission when print-job administration or retry endpoints exist.

## Handoff To PLAN-0006

PLAN-0006 must be PWA-first.

Current human decisions after PLAN-0005 closeout:

- Blazor/PWA first.
- MAUI deferred to a later dedicated Windows terminal plan.
- PWA can act as a terminal on supported devices.
- Blazor only for PWA surfaces.
- OI-0018 remains deferred and must not be implemented accidentally inside the first PLAN-0006
  milestone.
