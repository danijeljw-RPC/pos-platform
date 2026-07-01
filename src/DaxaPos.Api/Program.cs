using DaxaPos.Api;
using DaxaPos.Api.Audit;
using DaxaPos.Api.Authentication;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Application.Events;
using DaxaPos.Domain.Events;
using DaxaPos.Infrastructure;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaxaPersistence(builder.Configuration);
builder.Services.AddDaxaInfrastructure();

// Session bearer-token authentication (ADR-0015) — opaque, server-hashed, DB-validated. Not
// Keycloak/OIDC/JWT; that remains a separate, not-yet-wired concern for AuthMethod.CloudIdentityProvider
// (see ADR-0015 §Follow-Up Work). This is the only scheme wired in PLAN-0003 Milestone C; the
// DeviceToken scheme arrives with device registration (Milestone E).
builder.Services
    .AddAuthentication(SessionAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();

// Audit context plumbing (ADR-0014, ADR-0010): domain-event handlers writing AuditEvent rows.
// Hosted here in DaxaPos.Api rather than DaxaPos.Persistence — see ADR-0015 §4.
builder.Services.AddScoped<IDomainEventHandler<LocalUserLoginSucceededDomainEvent>, LocalUserLoginSucceededAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<LocalUserLoginFailedDomainEvent>, LocalUserLoginFailedAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<AuthSessionRevokedDomainEvent>, AuthSessionRevokedAuditHandler>();

// PLAN-0003 Milestone D: lifecycle audit handlers for Organisation/Location/Terminal create/update/
// deactivate/reactivate actions.
builder.Services.AddScoped<IDomainEventHandler<OrganisationLifecycleDomainEvent>, OrganisationLifecycleAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<LocationLifecycleDomainEvent>, LocationLifecycleAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<TerminalLifecycleDomainEvent>, TerminalLifecycleAuditHandler>();

// Health checks cover the API/database path only. Keycloak is scoped to cloud/admin/back-office
// auth (ADR-0013) and is intentionally not part of this check — the API must start and report
// healthy whether or not Keycloak is reachable.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DaxaDbContext>("database");

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapOrganisationEndpoints();
app.MapLocationEndpoints();
app.MapTerminalEndpoints();

// Dev/local-only bootstrap admin seeding (PLAN-0003 Milestone C) — see BootstrapAdminSeeder for
// the production-safety rules (requires both env vars, idempotent, never overwrites an existing
// admin's password, no guessable fallback credential).
await BootstrapAdminSeeder.SeedAsync(app.Services);

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests. Top-level statements generate
// their Program class in the global namespace, so this partial must stay unwrapped to merge with it.
public partial class Program;
