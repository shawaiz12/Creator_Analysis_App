using CreatorAnalytics.Audit.Contracts.Services;
using CreatorAnalytics.SharedKernel.Tenancy;
using CreatorAnalytics.Strategy.Contracts.Services;
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
        IReadOnlyList<Guid> pendingIds;

        using (var listScope = _rootServices.CreateScope())
        {
            var outbox = listScope.ServiceProvider.GetRequiredService<IStrategyOutbox>();
            pendingIds = await outbox.GetPendingIdsAsync(BatchSize, cancellationToken);
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", messageId);
                await RecordFailureAsync(messageId, ex, cancellationToken);
            }
        }

        return handled;
    }

    private static async Task ProcessOneAsync(
        IServiceProvider services, Guid messageId, CancellationToken cancellationToken)
    {
        var outbox = services.GetRequiredService<IStrategyOutbox>();
        var audit = services.GetRequiredService<IAuditWriter>();
        var tenantContext = services.GetRequiredService<TenantContext>();

        var item = await outbox.GetAsync(messageId, cancellationToken);

        tenantContext.Set(item.TenantId);

        // 1) Audit first, and only once. A crash after this line only means a harmless retry.
        await audit.WriteAsync(item.TenantId, item.MessageId, item.Type, item.Content, cancellationToken);

        // 2) Only then mark the outbox message as done.
        await outbox.MarkProcessedAsync(messageId, cancellationToken);
    }

    private async Task RecordFailureAsync(
        Guid messageId, Exception exception, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _rootServices.CreateScope();
            var outbox = scope.ServiceProvider.GetRequiredService<IStrategyOutbox>();

            var parked = await outbox.MarkFailedAsync(messageId, exception.Message, cancellationToken);

            if (parked)
            {
                _logger.LogCritical(
                    "Outbox message {MessageId} failed too many times and is now parked.", messageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not record the failure of outbox message {MessageId}", messageId);
        }
    }
}