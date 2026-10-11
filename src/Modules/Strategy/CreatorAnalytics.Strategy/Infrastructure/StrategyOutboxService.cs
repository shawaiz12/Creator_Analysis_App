using System.Text.Json;
using CreatorAnalytics.Strategy.Contracts.Services;
using CreatorAnalytics.Strategy.Domain;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Strategy.Infrastructure;

internal sealed class StrategyOutboxService : IStrategyOutbox
{
    private readonly StrategyDbContext _context;

    public StrategyOutboxService(StrategyDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Guid>> GetPendingIdsAsync(
        int take, CancellationToken cancellationToken = default)
    {
        // Messages that failed too many times are parked and no longer picked up.
        return await _context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.Attempts < OutboxMessage.MaxAttempts)
            .OrderBy(m => m.OccurredOnUtc)
            .Select(m => m.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<OutboxItem> GetAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(m => m.Id == messageId, cancellationToken);

        // The module knows the shape of its own events, so it reads the tenant out of them.
        using var json = JsonDocument.Parse(message.Content);
        var tenantId = json.RootElement.GetProperty("TenantId").GetGuid();

        return new OutboxItem(message.Id, tenantId, message.Type, message.Content);
    }

    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.SingleAsync(m => m.Id == messageId, cancellationToken);

        message.MarkAsProcessed();
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> MarkFailedAsync(
        Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.SingleAsync(m => m.Id == messageId, cancellationToken);

        message.MarkAsFailed(error);
        await _context.SaveChangesAsync(cancellationToken);

        return message.Attempts >= OutboxMessage.MaxAttempts;
    }
}