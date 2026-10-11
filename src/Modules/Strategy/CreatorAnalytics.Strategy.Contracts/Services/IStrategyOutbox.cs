namespace CreatorAnalytics.Strategy.Contracts.Services;

public sealed record OutboxItem(Guid MessageId, Guid TenantId, string Type, string Content);

public interface IStrategyOutbox
{
    Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int take, CancellationToken cancellationToken = default);

    Task<OutboxItem> GetAsync(Guid messageId, CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>Records a failed attempt. Returns true when the message is now parked.</summary>
    Task<bool> MarkFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}