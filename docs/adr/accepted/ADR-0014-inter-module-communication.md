# ADR-0014 — Inter-Module Communication Pattern

## Status

Accepted

## Date

2026-07-01

## Context

PLAN-0001 (Architecture Foundation) requires a decision on how `DaxaPos.Modules.*` projects talk to each other before `DaxaPos.Api`/`Domain`/`Application`/`Infrastructure`/`Persistence` are scaffolded (PLAN-0002), because the answer determines project reference rules from the first commit. A repository-wide search of `docs/` turned up no existing decision — only an aspirational mention of "domain events" as a folder label under `DaxaPos.Domain` in `docs/architecture/overview.md`.

Daxa POS has many cross-module interactions from the start of the MVP, for example:

- `Modules.Orders` needs `Modules.Tax` to calculate tax for a line (needs an immediate return value).
- `Modules.Payments` needs to tell `Modules.Receipts` and `Modules.Audit` that a payment succeeded (one event, multiple independent consumers).
- `Modules.Sync` needs to know when orders, payments, and refunds are created so it can enqueue them for local-to-cloud sync (PLAN-0007), without `Modules.Orders`/`Modules.Payments` knowing sync exists.
- Future modules (loyalty, reporting, gift cards) will want to react to order/payment events without the core commerce modules being modified every time a new consumer is added.

The repository is currently a single-process monolith: one `DaxaPos.Api` host, one `DaxaPos.Workers` host, one PostgreSQL database per deployment (local, hybrid-local, or cloud instance), per ADR-0001/ADR-0002. The local server target is a small form-factor Linux PC (ADR-0012, OI-0003), which constrains how much infrastructure the local Docker Compose stack should carry.

## Options Compared

### 1. Direct in-process service calls

The caller resolves the callee's interface via DI and calls it directly, gets a return value synchronously.

- **Pros:** simplest possible model; no infrastructure; return values available immediately; trivial to unit test with a fake/mock.
- **Cons:** every caller must reference every callee's interface; adding a new consumer of "order paid" means editing `Modules.Payments` to call it; can silently grow into a dependency graph where core commerce modules reference audit, sync, receipts, reporting, etc.

### 2. Domain events inside the application boundary

A module raises an immutable event (e.g. `OrderPaidDomainEvent`); an in-process dispatcher (e.g. a MediatR-style `INotification`/`INotificationHandler`, or a small hand-rolled `IDomainEventDispatcher`) invokes zero or more handlers registered by other modules, within the same process and typically the same request/transaction.

- **Pros:** publisher doesn't know or care who (if anyone) is listening; new consumers (audit, sync outbox, future loyalty/reporting) are added without touching the publishing module; matches CLAUDE.md's principle that "pushed events are convenience notifications, not the source of truth" at the in-process level too — server state (the DB write) still happens before/independently of the event.
- **Cons:** handlers running synchronously in the same request add latency to the triggering call; a slow or failing handler can be mistaken for part of the core operation if not isolated carefully; easy to overuse and create a hidden coupling graph if used for things that really need a direct return value.

### 3. Durable queue / message broker

An external broker (RabbitMQ, Azure Service Bus, Kafka, etc.) carries messages between modules or services, with its own persistence, retry, and delivery guarantees.

- **Pros:** true durability and retry independent of the API process; natural fit once there are multiple processes/services that need to scale or fail independently; well-suited to unreliable/webhook-style external integrations (e.g. payment provider callbacks) at larger scale.
- **Cons:** adds an infrastructure dependency to every deployment mode, including the local small form-factor server (ADR-0012); nothing in the current MVP scope needs cross-process delivery — there is one API process and one worker process talking to one database; adds operational and testing burden (broker container, message contracts/versioning, dead-letter handling) with no MVP-stage payoff.

### 4. Hybrid approach

Use direct in-process calls for synchronous request/response needs, in-process domain events for internal fan-out/decoupling, and no external broker until a concrete requirement (cross-process scaling, durable async webhook processing, etc.) demands one.

- **Pros:** takes the simplicity of (1) where a return value is genuinely needed, the decoupling of (2) where fan-out is needed, and defers the operational cost of (3) until it's actually justified.
- **Cons:** requires a clear, documented rule for which situations use which mechanism, or teams will use the wrong one out of habit.

## Decision

Daxa POS adopts the **hybrid approach (Option 4)** for MVP:

- **Direct in-process calls**, via interfaces defined in `DaxaPos.Application`, for synchronous command/query workflows where the caller needs an immediate result — e.g. `OrderService` calling `ITaxEngine.CalculateTax(...)`, `PaymentService` calling `IPaymentTerminalProvider.StartPaymentAsync(...)`.
- **In-process domain events**, dispatched through an `IDomainEventDispatcher` abstraction (interface in `DaxaPos.Application`/`DaxaPos.Domain`, implementation in `DaxaPos.Infrastructure`), for one-to-many side effects that should not couple the originating module to every consumer — e.g. `OrderPaidDomainEvent` consumed independently by `Modules.Receipts`, `Modules.Audit`, and the sync outbox (PLAN-0007). Handlers run in-process, within or immediately after the triggering unit of work (e.g. after `SaveChangesAsync` succeeds), so they never fire for a write that didn't actually commit.
- **No external queue/message broker for MVP.** The local order queue described in PLAN-0007 is a PostgreSQL-backed outbox table specific to hybrid/local-to-cloud sync durability — it is not a general-purpose broker for other module-to-module concerns, and this ADR does not change it.
- Revisit this decision if/when a concrete requirement emerges that direct calls and in-process events cannot satisfy: cross-process horizontal scaling of the API, durable async retry for payment-provider webhooks beyond what an outbox table can economically provide, or cloud-mode multi-region fan-out.

## Handler I/O Rule

**Domain event handlers must not directly perform slow, unreliable, or external I/O inside the request path.**

An in-process handler that calls out to an external service, printer, payment provider, webhook, email service, cloud sync endpoint, or any other slow/unreliable dependency turns a fire-and-forget internal notification into an unbounded, failure-prone extension of the triggering HTTP request — exactly the failure mode the "Negative"/"Risks" sections below warn about.

If a handler needs that kind of work done, it must not do it inline. Instead it must:

1. Write a durable outbox/work item row to PostgreSQL, in the same transaction as the domain change that raised the event (or immediately after, using the outbox pattern), and
2. Let `DaxaPos.Workers` pick up and process that work item asynchronously, outside the request path, with its own retry policy.

Examples of work that must go through the outbox → `DaxaPos.Workers` path rather than a direct in-process handler call:

```text
Sending a receipt to a printer
Calling a payment provider adapter from a non-payment-initiating context (e.g. a reconciliation sweep)
Delivering a webhook
Sending an email or notification
Pushing to a cloud sync endpoint
Calling any other network-dependent or slow external system
```

Examples of work that is fine directly inside an in-process handler (fast, local, reliable):

```text
Writing an audit log row to the local database
Updating an in-memory read model / cache
Raising a further in-process domain event
```

This rule does not change the outbox mechanism itself — the PLAN-0007 local order queue/outbox remains the sync-specific durability table described elsewhere in this ADR. It generalises the same pattern (durable row + background worker, not inline I/O) to any domain event handler that needs to reach outside the current process.

## Rule of thumb

```text
Need a return value right now?            -> direct call through an Application-layer interface
One event, several independent reactors,
all fast/local/reliable work?             -> in-process domain event
One event, but a reactor needs slow,
unreliable, or external I/O?              -> in-process domain event handler writes a durable
                                              outbox/work item; DaxaPos.Workers processes it
Needs to survive a process crash/restart
before being handled, or cross a process
boundary, and isn't covered by the outbox
pattern above?                            -> not yet in MVP scope; revisit this ADR
```

## Impact on project references and module boundaries

- `DaxaPos.Domain` defines domain event contracts as simple immutable records (e.g. `OrderPaidDomainEvent`) with no dependency on `Infrastructure`.
- `DaxaPos.Application` defines the `IDomainEventDispatcher` abstraction and any cross-module interfaces used for direct calls (e.g. `ITaxEngine`, `IPaymentTerminalProvider`).
- `DaxaPos.Infrastructure` provides the in-process dispatcher implementation. Swapping in a real broker later is an `Infrastructure`-only change, not a `Domain`/`Application` rewrite.
- A `Modules.*` project must not take a direct project reference on another `Modules.*` project. Cross-module needs go through an interface resolved via DI (direct call) or a published domain event (fan-out) — never a hard reference from e.g. `Modules.Orders.csproj` to `Modules.Receipts.csproj`.
- `Modules.Orders` and `Modules.Payments` may both reference `Modules.Tax` only if tax calculation is treated as a shared kernel/interface rather than a project reference; preferred pattern is `Modules.Orders` depending on `ITaxEngine` (declared in `Application`) with `Modules.Tax` providing the implementation, registered in DI.

## Impact on testing

- Direct calls: standard unit tests using fakes/mocks of the called interface.
- Domain events: unit-test each handler in isolation (given event X, assert side effect Y), plus at least one integration test per financially significant flow asserting handlers actually fire end-to-end (e.g. "paying an order writes a receipt and an audit entry"), consistent with CLAUDE.md's rule that financial, tax, payment, refund, and audit logic must not be changed without tests.
- No message-broker test infrastructure (e.g. Testcontainers RabbitMQ/Kafka) is needed for MVP CI. PLAN-0008's CI pipeline only needs PostgreSQL (and Keycloak, for the cloud/admin auth path per ADR-0013) as external test dependencies.

## Impact on local deployment

- No broker container is added to the Docker Compose stack defined in ADR-0012 (`api`, `worker`, `db`, `keycloak`, `sync`). This keeps the local server's footprint within the small form-factor PC reference target (ADR-0012, OI-0003).
- The PLAN-0007 local order queue/outbox remains a PostgreSQL table, not a broker — unchanged by this decision.

## Impact on cloud deployment

- A single API process (or a small number of stateless instances behind a load balancer) can use in-process domain events without any cross-instance concern, since MVP cloud deployment does not yet require horizontal scaling across processes for this workload.
- Cross-instance real-time fan-out (e.g. notifying all connected SignalR clients across multiple API instances) already has its own mechanism — a SignalR backplane — which is a separate, already-anticipated concern under CLAUDE.md's "Realtime and sync" principles, not a general-purpose application message broker. This ADR does not introduce or replace that mechanism.

## Future migration to queues

If a real broker becomes necessary later (e.g. reliable async payment-provider webhook processing, multi-region cloud fan-out, or hybrid sync volume that outgrows a Postgres-backed outbox), the migration path is:

1. Keep domain event contracts unchanged — they are already immutable, serializable records.
2. Replace the `Infrastructure`-layer `IDomainEventDispatcher` implementation with a broker-backed publisher/consumer for the specific events that need durable/async delivery.
3. Leave modules that don't need durable/async delivery on the in-process dispatcher unchanged.

Only the `Infrastructure` implementation changes; `Domain`, `Application`, and the `Modules.*` projects that raise or handle events do not need to change when this migration happens.

## Consequences

### Positive

- Simple mental model for MVP: one process, one transaction boundary, no broker to operate or test against.
- No new infrastructure dependency added to the local small form-factor server target (ADR-0012).
- Matches the existing CLAUDE.md principle that realtime/pushed notifications are convenience, not the source of truth — applied here at the in-process level as well.
- New consumers of an event (audit, sync, future loyalty/reporting) can be added without modifying the publishing module.
- Straightforward migration path to a broker later, isolated to `Infrastructure`.

### Negative

- Domain event handlers running synchronously add latency to the triggering request if not kept fast; slow, unreliable, or external I/O must go through the outbox → `DaxaPos.Workers` path defined in the Handler I/O Rule above, not run inline.
- Requires discipline to keep the direct-call vs. domain-event boundary, and the in-process-handler vs. outbox boundary, consistent — this ADR's "rule of thumb" and "Handler I/O Rule" exist specifically to prevent that drift.

### Risks

- Domain events can become a hidden coupling graph if used for things that actually need an immediate, reliable return value — mitigated by the rule of thumb above and by code review attention to new event handlers.
- A handler that fails silently could mask a real side-effect gap (e.g. a receipt never generated); handler failures must be logged/audited, not swallowed.

## Alternatives Considered

- **Durable queue/broker from day one** — rejected for MVP. No current requirement needs cross-process delivery; adds infrastructure cost to every deployment mode including the constrained local server target, with no payoff yet.
- **Direct calls only, no events** — rejected. Would force `Modules.Orders`/`Modules.Payments` to be hard-referenced and explicitly modified every time a new consumer (receipts, audit, sync, future loyalty/reporting) needs to react to an order or payment, which conflicts with the module-boundary goal set out in PLAN-0001.
- **Domain events only, no direct calls** — rejected. Synchronous request/response needs (e.g. "calculate tax for this line", "get terminal status") need an immediate return value; routing everything through fire-and-forget events adds needless complexity and latency to those paths.

## Follow-Up Work

- Satisfies PLAN-0001 step 8 ("Document module communication patterns").
- PLAN-0002 scaffolds only the minimal `IDomainEvent`/`IDomainEventDispatcher` abstraction and a simple in-process dispatcher — no real domain events, no handlers, and no outbox table yet, since there is no business logic to raise events from in that plan.
- The first real domain event handler that needs external I/O (expected around PLAN-0005/PLAN-0007) must implement the generic outbox/work-item mechanism described in the Handler I/O Rule, not just the sync-specific queue — the two should likely share one outbox table/worker pattern rather than being built twice.
- PLAN-0007 (Sync) should reference this ADR to confirm the local order queue/outbox is one instance of this general outbox pattern, not a separate, unrelated mechanism.

## Related Documents

- [PLAN-0001 — Architecture Foundation](../../plans/active/PLAN-0001-architecture-foundation.md)
- [PLAN-0002 — Platform Skeleton](../../plans/active/PLAN-0002-platform-skeleton.md)
- [PLAN-0007 — Sync, Local, Hybrid](../../plans/active/PLAN-0007-sync-local-hybrid-planning.md)
- [ADR-0007 — Local/Hybrid Sync Principles](../accepted/ADR-0007-local-hybrid-sync-principles.md)
- [ADR-0012 — Docker and Docker Compose Local Deployment Strategy](../accepted/ADR-0012-docker-local-deployment-strategy.md)
- [Architecture Overview](../../architecture/overview.md)
- [Implementation Readiness Report](../../plans/active/implementation-readiness-report.md)

---

## Acceptance Addendum

ADR-0014 is accepted, with the Handler I/O Rule added before acceptance: domain event handlers must not perform slow, unreliable, or external I/O directly; that work must be written as a durable outbox/work item and processed by `DaxaPos.Workers`.

PLAN-0002 (Platform Skeleton) may include only the minimal communication abstractions needed for the skeleton — `IDomainEvent`, `IDomainEventDispatcher`, a simple in-process dispatcher, and basic DI registration. It must not add real domain events, event handlers for business workflows, or an outbox table; those arrive with the first plan that actually needs them.

## Status Update

Status: **Accepted**
