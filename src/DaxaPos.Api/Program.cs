using DaxaPos.Infrastructure;
using DaxaPos.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaxaPersistence(builder.Configuration);
builder.Services.AddDaxaInfrastructure();

// Health checks cover the API/database path only. Keycloak is scoped to cloud/admin/back-office
// auth (ADR-0013) and is intentionally not part of this check — the API must start and report
// healthy whether or not Keycloak is reachable.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DaxaDbContext>("database");

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests. Top-level statements generate
// their Program class in the global namespace, so this partial must stay unwrapped to merge with it.
public partial class Program;
