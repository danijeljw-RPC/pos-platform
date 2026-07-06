using DaxaPos.Domain.Tenancy;
using DaxaPos.Infrastructure.Identity;
using DaxaPos.Infrastructure.Printing;
using DaxaPos.Persistence;
using DaxaPos.Workers;
using DaxaPos.Workers.Processing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
