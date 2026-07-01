using DaxaPos.Application.Events;
using DaxaPos.Application.Identity;
using DaxaPos.Domain.Tenancy;
using DaxaPos.Infrastructure.Events;
using DaxaPos.Infrastructure.Identity;
using DaxaPos.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDaxaInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();

        // PLAN-0003 Milestone A: AuthContext/tenant-context plumbing and credential hashing.
        // Nothing resolves these yet (no endpoints call them until Milestone C onward) — this is
        // the DI wiring the later milestones build on.
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthContextAccessor, HttpContextAuthContextAccessor>();
        services.AddScoped<ICurrentTenantProvider, CurrentTenantProvider>();
        services.AddSingleton<IPinHasher, Pbkdf2PinHasher>();
        services.AddSingleton<IDeviceCredentialHasher, HmacDeviceCredentialHasher>();
        services.AddSingleton<ISessionTokenService, RandomSessionTokenService>();

        return services;
    }
}
