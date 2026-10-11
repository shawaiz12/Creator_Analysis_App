namespace CreatorAnalytics.Strategy.Domain.Events;

public sealed record StrategyRevisionAddedEvent(
    Guid DocumentId,
    Guid TenantId,
    Guid RevisionId,
    int VersionNumber,
    string Origin,
    Guid? AuthorUserId,
    DateTime OccurredOnUtc);

public sealed record StrategySubmittedEvent(
    Guid DocumentId,
    Guid TenantId,
    Guid SubmittedByUserId,
    Guid RevisionId,
    DateTime OccurredOnUtc);

public sealed record StrategyImplementedEvent(
    Guid DocumentId,
    Guid TenantId,
    Guid ImplementedByUserId,
    DateTime OccurredOnUtc);