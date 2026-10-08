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
        List<Guid> pendingIds;

        using (var listScope = _serviceProvider.CreateScope())
        {
            var context = listScope.ServiceProvider.GetRequiredService<StrategyDbContext>();
            pendingIds = await context.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null)
                .Select(m => m.Id)
                .Take(20)
                .ToListAsync(stoppingToken);
        }

        foreach (var messageId in pendingIds)
        {
            // One scope per message, so each message gets a fresh TenantContext.
            using var scope = _serviceProvider.CreateScope();

            try
            {
                await ProcessOneAsync(scope.ServiceProvider, messageId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", messageId);
            }
        }
    }

    private static async Task ProcessOneAsync(
        IServiceProvider services, Guid messageId, CancellationToken stoppingToken)
    {
        var strategyContext = services.GetRequiredService<StrategyDbContext>();
        var auditContext = services.GetRequiredService<AuditDbContext>();
        var tenantContext = services.GetRequiredService<TenantContext>();

        var message = await strategyContext.OutboxMessages
            .SingleAsync(m => m.Id == messageId, stoppingToken);

        using var json = JsonDocument.Parse(message.Content);
        var tenantId = json.RootElement.GetProperty("TenantId").GetGuid();
        tenantContext.Set(tenantId);

        var alreadyAudited = await auditContext.AuditLogs
            .AnyAsync(a => a.ProcessedMessageId == message.Id, stoppingToken);

        if (!alreadyAudited)
        {
            auditContext.AuditLogs.Add(
                new AuditLog(tenantId, message.Id, message.Type, message.Content));

            // 1) Audit first. A crash after this line only means a harmless retry.
            await auditContext.SaveChangesAsync(stoppingToken);
        }

        // 2) Only then mark the outbox message as done.
        message.MarkAsProcessed();
        await strategyContext.SaveChangesAsync(stoppingToken);
    }
}