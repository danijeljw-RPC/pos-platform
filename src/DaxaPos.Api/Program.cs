using System.Threading.RateLimiting;
using DaxaPos.Api;
using DaxaPos.Api.Audit;
using DaxaPos.Api.Authentication;
using DaxaPos.Api.Endpoints.Catalog;
using DaxaPos.Api.Endpoints.Identity;
using DaxaPos.Api.Endpoints.Menus;
using DaxaPos.Api.Endpoints.Tax;
using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Events;
using DaxaPos.Infrastructure;
using DaxaPos.Persistence;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaxaPersistence(builder.Configuration);
builder.Services.AddDaxaInfrastructure();

// Two Daxa WebAPI-native authentication schemes (ADR-0015 — neither is Keycloak/OIDC/JWT; that
// remains a separate, not-yet-wired concern for AuthMethod.CloudIdentityProvider, see ADR-0015
// §Follow-Up Work): "Session" validates the opaque, server-hashed session bearer token
// (Milestone C), "DeviceToken" validates a registered device credential (Milestone E). The
// default is a policy scheme that forwards by Authorization-header prefix, so
// .RequireAuthorization() endpoints accept both without per-endpoint scheme lists.
const string headerSelectorScheme = "SessionOrDeviceToken";
builder.Services
    .AddAuthentication(headerSelectorScheme)
    .AddPolicyScheme(headerSelectorScheme, "Session bearer token or device credential token", options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.Authorization.ToString()
                .StartsWith(DeviceTokenAuthenticationHandler.HeaderPrefix, StringComparison.OrdinalIgnoreCase)
                ? DeviceTokenAuthenticationHandler.SchemeName
                : SessionAuthenticationHandler.SchemeName;
    })
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationHandler.SchemeName, _ => { })
    .AddScheme<AuthenticationSchemeOptions, DeviceTokenAuthenticationHandler>(DeviceTokenAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();

// Pre-auth device registration is PIN-gated but unauthenticated, so it is rate-limited per remote
// IP (ADR-0008: "PIN attempts are rate-limited"). The permit limit is configuration-overridable so
// integration tests can raise it for non-rate-limit scenarios; the default is the approved
// 10/minute policy value.
var registrationPermitLimit = builder.Configuration.GetValue<int?>("DeviceRegistration:RateLimitPermitLimit")
    ?? DeviceRegistrationPinPolicy.RegistrationRateLimitPermitLimit;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(DeviceRegistrationEndpoints.RateLimitPolicyName, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = registrationPermitLimit,
                Window = DeviceRegistrationPinPolicy.RegistrationRateLimitWindow,
                QueueLimit = 0,
            }));
});

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

// PLAN-0003 Milestone E: device registration/credential lifecycle audit handlers (ADR-0008).
builder.Services.AddScoped<IDomainEventHandler<DeviceRegistrationPinCreatedDomainEvent>, DeviceRegistrationPinCreatedAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<DeviceRegistrationPinRevokedDomainEvent>, DeviceRegistrationPinRevokedAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<DeviceRegisteredDomainEvent>, DeviceRegisteredAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<DeviceRegistrationFailedDomainEvent>, DeviceRegistrationFailedAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<DeviceCredentialRotatedDomainEvent>, DeviceCredentialRotatedAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<DeviceRevokedDomainEvent>, DeviceRevokedAuditHandler>();

// PLAN-0003 Milestone F: staff-member lifecycle and staff PIN login audit handlers (ADR-0013's
// identity/permission-change and login audit requirements).
builder.Services.AddScoped<IDomainEventHandler<StaffMemberLifecycleDomainEvent>, StaffMemberLifecycleAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<StaffPinLoginSucceededDomainEvent>, StaffPinLoginSucceededAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<StaffPinLoginFailedDomainEvent>, StaffPinLoginFailedAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<StaffMemberDisabledDomainEvent>, StaffMemberDisabledAuditHandler>();

// PLAN-0004 Milestone C: tax configuration lifecycle audit handlers (OI-0007's audit requirement).
builder.Services.AddScoped<IDomainEventHandler<TaxDefinitionLifecycleDomainEvent>, TaxDefinitionLifecycleAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<TaxCategoryLifecycleDomainEvent>, TaxCategoryLifecycleAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<TaxCategoryDefinitionChangedDomainEvent>, TaxCategoryDefinitionChangedAuditHandler>();

// PLAN-0004 Milestone D: product catalogue lifecycle audit handlers (OI-0007's audit requirement).
builder.Services.AddScoped<IDomainEventHandler<ProductCategoryLifecycleDomainEvent>, ProductCategoryLifecycleAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<ProductLifecycleDomainEvent>, ProductLifecycleAuditHandler>();

// PLAN-0004 Milestone E: variant/modifier lifecycle audit handlers.
builder.Services.AddScoped<IDomainEventHandler<ProductVariantLifecycleDomainEvent>, ProductVariantLifecycleAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<ModifierGroupLifecycleDomainEvent>, ModifierGroupLifecycleAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<ModifierLifecycleDomainEvent>, ModifierLifecycleAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<ProductModifierGroupChangedDomainEvent>, ProductModifierGroupChangedAuditHandler>();

// PLAN-0004 Milestone F: location override / venue tax configuration audit handlers.
builder.Services.AddScoped<IDomainEventHandler<ProductLocationOverrideChangedDomainEvent>, ProductLocationOverrideChangedAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<VenueTaxConfigurationLifecycleDomainEvent>, VenueTaxConfigurationLifecycleAuditHandler>();

// PLAN-0004 Milestone G: menu construction audit handlers. ResolvedMenuEndpoints raises no domain
// event — it is a read-only projection, never mutates data.
builder.Services.AddScoped<IDomainEventHandler<MenuLifecycleDomainEvent>, MenuLifecycleAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<MenuSectionLifecycleDomainEvent>, MenuSectionLifecycleAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<MenuSectionItemChangedDomainEvent>, MenuSectionItemChangedAuditHandler>();
builder.Services.AddScoped<IDomainEventHandler<MenuAvailabilityRuleChangedDomainEvent>, MenuAvailabilityRuleChangedAuditHandler>();

// Health checks cover the API/database path only. Keycloak is scoped to cloud/admin/back-office
// auth (ADR-0013) and is intentionally not part of this check — the API must start and report
// healthy whether or not Keycloak is reachable.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DaxaDbContext>("database");

var app = builder.Build();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapOrganisationEndpoints();
app.MapLocationEndpoints();
app.MapTerminalEndpoints();
app.MapDeviceRegistrationPinEndpoints();
app.MapDeviceRegistrationEndpoints();
app.MapDeviceEndpoints();
app.MapStaffMemberEndpoints();
app.MapTaxDefinitionTemplateEndpoints();
app.MapTaxDefinitionEndpoints();
app.MapTaxCategoryEndpoints();
app.MapTaxCategoryDefinitionEndpoints();
app.MapProductCategoryEndpoints();
app.MapProductEndpoints();
app.MapProductVariantEndpoints();
app.MapModifierGroupEndpoints();
app.MapModifierEndpoints();
app.MapProductModifierGroupEndpoints();
app.MapProductLocationOverrideEndpoints();
app.MapProductSoldOutEndpoints();
app.MapVenueTaxConfigurationEndpoints();
app.MapMenuEndpoints();
app.MapMenuSectionEndpoints();
app.MapMenuSectionItemEndpoints();
app.MapMenuAvailabilityRuleEndpoints();
app.MapResolvedMenuEndpoints();

// Dev/local-only bootstrap admin seeding (PLAN-0003 Milestone C) — see BootstrapAdminSeeder for
// the production-safety rules (requires both env vars, idempotent, never overwrites an existing
// admin's password, no guessable fallback credential).
await BootstrapAdminSeeder.SeedAsync(app.Services);

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests. Top-level statements generate
// their Program class in the global namespace, so this partial must stay unwrapped to merge with it.
public partial class Program;
