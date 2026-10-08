namespace CreatorAnalytics.Strategy.Domain.Events;

public sealed record StrategyRejectedEvent(
    Guid DocumentId,
    Guid TenantId,
    Guid ReviewerId,
    Guid RevisionId,
    string Reason,
    DateTime OccurredOnUtc);