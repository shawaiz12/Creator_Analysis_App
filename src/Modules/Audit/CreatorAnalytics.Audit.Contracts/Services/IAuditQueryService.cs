namespace CreatorAnalytics.Audit.Contracts.Services;

public sealed record AuditEntry(Guid Id, string EventType, DateTime RecordedAtUtc, string EventData);

public interface IAuditQueryService
{
    Task<IReadOnlyList<AuditEntry>> ListAsync(
        Guid tenantId, int take, int skip, CancellationToken cancellationToken = default);
}