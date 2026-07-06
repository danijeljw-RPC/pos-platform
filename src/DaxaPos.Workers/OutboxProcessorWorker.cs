using DaxaPos.Application.Printing;
using DaxaPos.Domain.Enums;
using DaxaPos.Infrastructure.Identity;
using DaxaPos.Persistence;
using DaxaPos.Workers.Processing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DaxaPos.Workers;

/// <summary>
/// Polls the durable outbox for pending work and dispatches each item by <c>WorkType</c>
/// (PLAN-0005 Milestone E, ADR-0014's Handler I/O Rule). Thin composition-root/glue code — like
/// <c>Program.cs</c>/<c>BootstrapAdminSeeder</c> elsewhere in this codebase, it is not unit-tested
/// directly; the actual processing logic it delegates to (<see cref="PrintReceiptOutboxProcessor"/>)
/// is tested independently with a real database and a fake transport.
/// </summary>
public sealed class OutboxProcessorWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox poll iteration failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }
    }

    private async Task ProcessPendingBatchAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        List<(Guid Id, Guid TenantId)> claimable;
        await using (var pollScope = scopeFactory.CreateAsyncScope())
        {
            var pollDbContext = pollScope.ServiceProvider.GetRequiredService<DaxaDbContext>();

            // Cross-tenant scan — a documented, deliberate exception (see
            // IgnoreQueryFiltersUsageTests' approved list). This worker has no single tenant
            // context of its own until it claims a row; AmbientTenantContext is set per item below.
            claimable = await pollDbContext.OutboxWorkItems
                .IgnoreQueryFilters()
                .Where(item => item.Status == OutboxWorkItemStatus.Pending && item.NextAttemptAtUtc <= now)
                .OrderBy(item => item.CreatedAtUtc)
                .Take(BatchSize)
                .Select(item => new ValueTuple<Guid, Guid>(item.Id, item.TenantId))
                .ToListAsync(cancellationToken);
        }

        foreach (var (itemId, tenantId) in claimable)
        {
            AmbientTenantContext.Current = tenantId;
            try
            {
                await using var itemScope = scopeFactory.CreateAsyncScope();
                var itemDbContext = itemScope.ServiceProvider.GetRequiredService<DaxaDbContext>();
                var trackedItem = await itemDbContext.OutboxWorkItems.SingleOrDefaultAsync(item => item.Id == itemId, cancellationToken);
                if (trackedItem is null)
                {
                    continue;
                }

                switch (trackedItem.WorkType)
                {
                    case PrintReceiptWorkPayload.WorkType:
                        var processor = itemScope.ServiceProvider.GetRequiredService<PrintReceiptOutboxProcessor>();
                        await processor.ProcessAsync(itemDbContext, trackedItem, DateTimeOffset.UtcNow, cancellationToken);
                        break;
                    default:
                        logger.LogWarning("Unknown outbox work type {WorkType} for item {ItemId}; leaving as Pending.", trackedItem.WorkType, trackedItem.Id);
                        break;
                }
            }
            finally
            {
                AmbientTenantContext.Current = null;
            }
        }
    }
}
