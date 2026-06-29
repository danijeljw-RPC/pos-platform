# PLAN-0002 — Platform Skeleton

## Status

Draft

## Goal

Stand up a working but empty Daxa POS platform skeleton. This includes the .NET solution, the API host, the PostgreSQL database with EF Core, Docker Compose for local development, and a basic health check endpoint. No business logic yet — just a running, connected system.

## Scope

- .NET solution with project structure from PLAN-0001.
- ASP.NET Core API host (`DaxaPos.Api`).
- PostgreSQL via Docker Compose.
- EF Core `DbContext` with initial migrations.
- Keycloak via Docker Compose (basic setup).
- Docker Compose file for local dev stack.
- Health check endpoint (`/health`).
- Basic CI pipeline skeleton.

## Non-goals

- Business logic (orders, payments, tax, etc.).
- Full identity/RBAC implementation.
- UI (MAUI or PWA).
- Payment provider integrations.

## Context Read

- `docs/plans/active/PLAN-0001-architecture-foundation.md`
- `docs/adr/proposed/ADR-0001-single-codebase.md`
- `docs/adr/proposed/ADR-0009-keycloak-or-identity-provider-strategy.md`
- `docs/adr/proposed/ADR-0012-docker-local-deployment-strategy.md`
- `docs/deployment/docker.md`

## Files Likely To Change

```
src/DaxaPos.Api/
src/DaxaPos.Domain/
src/DaxaPos.Application/
src/DaxaPos.Infrastructure/
src/DaxaPos.Persistence/
docker-compose.yml
docker-compose.override.yml
.env.example
```

## Architecture Assumptions

- .NET 9 Web API.
- PostgreSQL 16+.
- EF Core 9.
- Keycloak 24+.
- Docker Compose v2.

## Domain Assumptions

- Tenant, Organisation, Location, Terminal tables exist in the initial schema.
- Multi-tenant query filters are applied from the start.

## Risks

- Keycloak setup complexity on first run.
- EF Core multi-tenant configuration requires careful design early.
- `.env` secrets management must be correct from the start.

## Implementation / Documentation Steps

1. Create .NET solution file.
2. Create project scaffolding (Api, Domain, Application, Infrastructure, Persistence).
3. Configure `Program.cs` (no Startup.cs).
4. Add PostgreSQL Docker Compose service.
5. Add Keycloak Docker Compose service.
6. Configure EF Core with PostgreSQL.
7. Create initial migration (Tenant, Organisation, Location, Terminal).
8. Add health check endpoint.
9. Add `.env.example` with all required environment variables.
10. Add `.gitignore` entries for secrets.
11. Document Docker Compose startup in `docs/deployment/docker.md`.
12. Update `docs/plans/active/planning-session-worker-notes.md`.

## Tests To Run Later

- `dotnet build` passes.
- `docker compose up` brings up API + DB + Keycloak.
- `GET /health` returns 200.
- EF Core migration applies cleanly.

## Documentation To Update

- `docs/deployment/docker.md`
- `docs/deployment/local.md`

## ADRs Required

- ADR-0001, ADR-0009, ADR-0012 (already proposed).

## Open Issues Required

- None new; see existing open issues.

## Commit Sequence

```
chore: scaffold Daxa POS .NET solution
infra: add Docker Compose for local dev stack
feat(api): add health check endpoint
docs: update deployment docs for Docker Compose setup
```

## Handoff Notes

This plan depends on PLAN-0001 (Architecture Foundation). After completing this plan, the system is runnable locally. The next worker can build on this skeleton to implement identity (PLAN-0003) and the product catalogue (PLAN-0004).
