using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CreatorAnalytics.Audit.Domain;
using CreatorAnalytics.Audit.Infrastructure;
using CreatorAnalytics.SharedKernel.Tenancy;
using CreatorAnalytics.Strategy.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CreatorAnalytics.Api.BackgroundServices;

public class OutboxProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorBackgroundService> _logger;

    public OutboxProcessorBackgroundService(IServiceProvider serviceProvider, ILogger<OutboxProcessorBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred processing outbox messages.");
            }

            // Wait 5 seconds before checking again
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
    {
        // Create a new scope for dependency injection since BackgroundService is a singleton
        using var scope = _serviceProvider.CreateScope();

        var strategyContext = scope.ServiceProvider.GetRequiredService<StrategyDbContext>();
        var auditContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();

        // 1. Fetch unprocessed messages
        var messages = await strategyContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .Take(20)
            .ToListAsync(stoppingToken);

        if (!messages.Any()) return;

        foreach (var message in messages)
        {
            try
            {
                // 2. Extract the TenantId from the JSON payload
                using var document = JsonDocument.Parse(message.Content);
                var tenantId = document.RootElement.GetProperty("TenantId").GetGuid();

                // 3. Impersonate the tenant for this scoped execution
                tenantContext.Set(tenantId);

                // 4. Create the Audit Log entry
                var auditLog = new AuditLog(
                    tenantId: tenantId,
                    processedMessageId: message.Id,
                    eventType: message.Type,
                    eventData: message.Content);

                auditContext.AuditLogs.Add(auditLog);

                // 5. Mark the original message as processed
                message.MarkAsProcessed();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                message.MarkAsFailed(ex.Message);
            }
        }

        // 6. Commit the changes to both modules
        await auditContext.SaveChangesAsync(stoppingToken);
        await strategyContext.SaveChangesAsync(stoppingToken);
    }
}