using System;

namespace CreatorAnalytics.Strategy.Domain.Events;

public record StrategyApprovedEvent(
    Guid DocumentId,
    Guid TenantId,
    Guid ReviewerId,
    Guid RevisionId,
    DateTime OccurredOnUtc);