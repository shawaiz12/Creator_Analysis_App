using System.Text.Json;
using CreatorAnalytics.Audit.Domain;
using CreatorAnalytics.Audit.Infrastructure;
using CreatorAnalytics.SharedKernel.Tenancy;
using CreatorAnalytics.Strategy.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CreatorAnalytics.Api.BackgroundServices;

public sealed class OutboxDispatcher
{
    private const int BatchSize = 20;

    private readonly IServiceProvider _rootServices;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(IServiceProvider rootServices, ILogger<OutboxDispatcher> logger)
    {
        _rootServices = rootServices;
        _logger = logger;
    }

    /// <summary>Processes one batch and returns how many messages were handled successfully.</summary>
    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        List<Guid> pendingIds;

        using (var listScope = _rootServices.CreateScope())
        {
            var context = listScope.ServiceProvider.GetRequiredService<StrategyDbContext>();
            pendingIds = await context.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null)
                .OrderBy(m => m.OccurredOnUtc)
                .Select(m => m.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
        }

        var handled = 0;

        foreach (var messageId in pendingIds)
        {
            // One scope per message, so each message gets a fresh TenantContext.
            using var scope = _rootServices.CreateScope();

            try
            {
                await ProcessOneAsync(scope.ServiceProvider, messageId, cancellationToken);
                handled++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", messageId);
            }
        }

        return handled;
    }

    private static async Task ProcessOneAsync(
        IServiceProvider services, Guid messageId, CancellationToken cancellationToken)
    {
        var strategyContext = services.GetRequiredService<StrategyDbContext>();
        var auditContext = services.GetRequiredService<AuditDbContext>();
        var tenantContext = services.GetRequiredService<TenantContext>();

        var message = await strategyContext.OutboxMessages
            .SingleAsync(m => m.Id == messageId, cancellationToken);

        using var json = JsonDocument.Parse(message.Content);
        var tenantId = json.RootElement.GetProperty("TenantId").GetGuid();
        tenantContext.Set(tenantId);

        var alreadyAudited = await auditContext.AuditLogs
            .AnyAsync(a => a.ProcessedMessageId == message.Id, cancellationToken);

        if (!alreadyAudited)
        {
            auditContext.AuditLogs.Add(
                new AuditLog(tenantId, message.Id, message.Type, message.Content));

            // 1) Audit first. A crash after this line only means a harmless retry.
            await auditContext.SaveChangesAsync(cancellationToken);
        }

        // 2) Only then mark the outbox message as done.
        message.MarkAsProcessed();
        await strategyContext.SaveChangesAsync(cancellationToken);
    }
}