using DaxaPos.Application.Events;
using DaxaPos.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDaxaInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();

        return services;
    }
}
