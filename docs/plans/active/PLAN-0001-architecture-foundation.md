# PLAN-0001 — Architecture Foundation

## Status

Draft

## Goal

Define and document the foundational architecture of Daxa POS before any application code is written. This plan covers the .NET solution structure, database schema design, core domain model, and module boundaries.

## Scope

- .NET solution structure (`src/`, `tests/`).
- PostgreSQL database schema design.
- Core domain model and entity definitions.
- Module boundary definitions.
- Inter-module communication patterns.
- Background worker architecture.

## Non-goals

- Writing production application code.
- Database migrations (separate plan).
- UI implementation.
- Payment provider integration.

## Context Read

- `CLAUDE.md` — suggested .NET solution structure section.
- `docs/architecture/01-core-architecture.md`
- `docs/architecture/02-domain-primitives.md`
- `docs/adr/proposed/ADR-0001-single-codebase.md`
- `docs/adr/proposed/ADR-0002-cloud-local-hybrid-deployment.md`
- `docs/adr/proposed/ADR-0003-multi-location-by-default.md`

## Files Likely To Change

```
src/
  DaxaPos.Domain/
  DaxaPos.Application/
  DaxaPos.Infrastructure/
  DaxaPos.Persistence/
  DaxaPos.Modules.*/
docs/architecture/overview.md
docs/architecture/tenancy.md
docs/architecture/multi-location.md
```

## Architecture Assumptions

- .NET 9 or newer.
- PostgreSQL with EF Core.
- No `Startup.cs`; use modern `Program.cs`.
- Modular architecture with clear domain/application/infrastructure separation.
- Each module (`Catalog`, `Orders`, `Tax`, etc.) is a separate project.

## Domain Assumptions

- Tenant → Organisation → Region → Country → Location → Terminal hierarchy.
- All core domain primitives as listed in `CLAUDE.md`.

## Risks

- Over-engineering the module boundaries too early.
- Getting lost in architecture without producing any working code.
- EF Core configuration complexity for multi-tenant queries.

## Implementation / Documentation Steps

1. Define .NET solution file structure (directories and project names).
2. Define `DaxaPos.Domain` — entities, value objects, domain events.
3. Define `DaxaPos.Application` — use cases, interfaces, DTOs.
4. Define `DaxaPos.Infrastructure` — EF Core, external service wrappers.
5. Define `DaxaPos.Persistence` — EF Core `DbContext`, migrations.
6. Define module project boundaries and references.
7. Define background worker project.
8. Document module communication patterns (direct call, domain event, or message queue).
9. Update `docs/architecture/overview.md`.
10. Create initial ADR or open issue for any unresolved architecture decisions.

## Tests To Run Later

- Unit tests for domain model invariants.
- Integration tests for EF Core multi-tenant query filters.

## Documentation To Update

- `docs/architecture/overview.md`
- `docs/architecture/tenancy.md`
- `docs/architecture/multi-location.md`
- `docs/plans/active/planning-session-worker-notes.md`

## ADRs Required

- ADR-0001 through ADR-0003 are relevant (already proposed).

## Open Issues Required

- Any unresolved module boundary questions to be captured as open issues.

## Commit Sequence

```
chore: scaffold .NET solution structure
docs: update architecture foundation docs
```

## Handoff Notes

This plan should be executed before any code is written. Review proposed ADRs first. The architecture foundation plan directly enables PLAN-0002 (Platform Skeleton).
