using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DaxaPos.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddDaxaPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DaxaDb")
            ?? throw new InvalidOperationException("Connection string 'DaxaDb' is not configured.");

        services.AddDbContext<DaxaDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
