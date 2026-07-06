using DaxaPos.Domain.Tenancy;
using DaxaPos.Infrastructure.Identity;
using DaxaPos.Infrastructure.Printing;
using DaxaPos.Persistence;
using DaxaPos.Workers.Processing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DaxaPos.Workers;

// Explicit namespace + Main (rather than top-level statements) so this entry point compiles as
// DaxaPos.Workers.Program, not the global ::Program that top-level statements would generate.
// DaxaPos.Api.Tests references both DaxaPos.Api (which exposes a global partial Program for
// WebApplicationFactory<Program>) and DaxaPos.Workers — two global-namespace Program types would
// be an unresolvable ambiguous reference wherever the test project uses the bare name.
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddDaxaPersistence(builder.Configuration);

        // DaxaPos.Workers has no HTTP request to derive a tenant from — AmbientCurrentTenantProvider reads
        // the per-work-item tenant OutboxProcessorWorker sets before processing each row.
        builder.Services.AddSingleton<ICurrentTenantProvider, AmbientCurrentTenantProvider>();

        builder.Services.Configure<NetworkPrinterOptions>(builder.Configuration.GetSection(NetworkPrinterOptions.SectionName));
        builder.Services.AddSingleton<IPrinterTransport, NetworkPrinterTransport>();

        builder.Services.AddScoped<PrintReceiptOutboxProcessor>();
        builder.Services.AddHostedService<OutboxProcessorWorker>();

        var host = builder.Build();
        host.Run();
    }
}
