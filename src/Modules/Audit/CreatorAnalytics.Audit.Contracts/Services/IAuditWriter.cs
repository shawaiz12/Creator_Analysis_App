namespace CreatorAnalytics.Audit.Contracts.Services;

public interface IAuditWriter
{
    /// <summary>Records an event once. Returns false if this message was already recorded.</summary>
    Task<bool> WriteAsync(
        Guid tenantId, Guid sourceMessageId, string eventType, string eventData,
        CancellationToken cancellationToken = default);
}