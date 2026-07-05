# Module: Payment Service

The payment service processes payments and maintains the payment ledger.

See also: `docs/modules/08-payments.md`.

---

## Responsibilities

- Cash payments.
- Manual external EFTPOS payments.
- Integrated EFTPOS payments (via payment adapter).
- Gift card payments (later).
- Store credit payments (later).
- Account sale / charge account (later).
- Deposits.
- Split payments (multiple payments per order).
- Payment provider selection and adapter resolution.
- Payment status tracking.
- Payment ledger (append-only).
- Idempotency keys.

## Payment Lifecycle

```text
Created → SentToTerminal → AwaitingCustomer
→ Approved / Declined / Cancelled / TimedOut
→ Recorded → OrderClosed or PaymentRetry
```

## Payment Entities

```text
Payment
PaymentMethod      (Cash, ManualEftpos, Integrated, GiftCard, StoreCredit)
PaymentStatus
PaymentLedgerEntry
```

## Key Rules

- Staff do not manually type amounts into integrated terminals.
- Payments are append-only (no silent edits).
- Each payment has an idempotency key.
- All payment activity is audited.

## Implementation Status (PLAN-0005 Milestone B, 2026-07-05)

Payment foundation (cash, manual EFTPOS, ledger, adapter interface) is implemented with endpoints under `src/DaxaPos.Api/Endpoints/Payments/`. No refunds, receipts, or printing yet.

- `Payment`: `TenantId`, `OrderId`, `LocationId`, `Method` (`Cash`/`ManualEftpos`/`Integrated`), `Status` (`Created`/`Approved`/`Declined`/`Cancelled`/`TimedOut`/`Recorded`), `AmountRequested`, `AmountApproved?`, `IdempotencyKey` (globally unique), `TakenByUserId?`/`TakenByStaffMemberId?`, `RecordedAtUtc`, `ProviderReference?`. No `OrganisationId` column of its own — scoped entirely through `OrderId`, matching `OrderLine`'s precedent from Milestone A. Cash and manual EFTPOS are recorded immediately as `Recorded` with `AmountApproved` set equal to `AmountRequested` — neither method has an external system to await a result from, so `Created`/`Approved`/`Declined`/`Cancelled`/`TimedOut` are unreachable until PLAN-0009's integrated-adapter work actually uses them.
- `PaymentLedgerEntry`: append-only row per state transition (one row per payment in this milestone, since cash/manual jump straight to `Recorded`) — proves the ledger is genuinely append-only rather than `Payment.Status` being overwritten with no trail. Carries its own denormalized `TenantId` (not listed in the plan's literal field list, added for the same fail-closed-query-filter reason Milestone A added it to `OrderLine`/`OrderLineModifier`/`OrderLineTax`).
- `PaymentMethod.Integrated` is rejected at the endpoint (400) — no `IPaymentTerminalProvider` adapter exists yet to actually call a terminal. The route that accepts it is PLAN-0009's scope.
- **Idempotency (ADR-0010):** a retry with the same `IdempotencyKey` returns the already-recorded payment (200 OK), never a duplicate row — checked before the order-state check, so a retry still succeeds even if the first attempt's payment already closed the order.
- **Settlement (`DaxaPos.Application.Payments.PaymentSettlement`, TDD'd first):** the running total of `Recorded` payments against an order may never exceed `Order.GrandTotalAmount` (split payments must add up to it exactly, rejected with 400 if they would exceed it); reaching it exactly transitions `Order.Status` to `Completed` and sets `ClosedAtUtc` — the one place this milestone reaches back into Milestone A's state machine, reusing `OrderLifecycleDomainEvent`/`OrderLifecycleAuditHandler` with `Action = "Completed"` rather than a new event/handler.
- `IPaymentTerminalProvider` (in `DaxaPos.Application.Payments`, per ADR-0005's conceptual interface, reproduced verbatim): `StartPaymentAsync`, `RefundAsync`, `GetTerminalStatusAsync`, `CancelPaymentAsync` — interface and placeholder request/result DTOs only, no concrete adapter and no DI registration (nothing to register yet). Never called by any endpoint in this milestone — the only "adapter-shaped" surface Milestone B introduces, and it stays inert until PLAN-0009 implements Stripe Terminal against it.
- New permission `payments.record` — `Operational` category, staff-PIN-eligible, granted to `SystemAdmin`/`OrganisationOwner`/`VenueManager`/`Staff`, matching `orders.manage`'s exact grant set and reasoning.
- See `docs/plans/active/PLAN-0005-worker-notes.md`'s "Milestone B Report" for full detail and deviations.

## Refunds (PLAN-0005 Milestone C, 2026-07-06)

A `Payment` may now have one or more `Refund` rows linked against it (see `docs/modules/refunds.md`). Refunds are a pure reversal record per ADR-0010 — recording a refund never mutates the original `Payment` row; `Status`/`AmountApproved` stay exactly as they were at the time the payment was taken. `Refund` is scoped through `PaymentId`, and its own settlement rule (`RefundSettlement`) prevents the running total of refunds against a payment from exceeding `Payment.AmountApproved`.

## Related Plans

- [PLAN-0005 — Payments, Receipts, Printing](../plans/active/PLAN-0005-payments-receipts-printing-planning.md)
- [ADR-0005 — Payment Adapter Architecture](../adr/accepted/ADR-0005-payment-provider-adapter-architecture.md)
