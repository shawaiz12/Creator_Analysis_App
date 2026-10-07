using System;

namespace CreatorAnalytics.Strategy.Domain;

public class OutboxMessage
{
    public Guid Id { get; private set; }

    // Identifies what happened (e.g., "StrategyApproved", "StrategyRejected")
    public string Type { get; private set; }

    // The serialized JSON data of the event
    public string Content { get; private set; }

    public DateTime OccurredOnUtc { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }
    public string? Error { get; private set; }

    public OutboxMessage(string type, string content)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type is required", nameof(type));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required", nameof(content));

        Id = Guid.NewGuid();
        Type = type;
        Content = content;
        OccurredOnUtc = DateTime.UtcNow;
    }

    public void MarkAsProcessed()
    {
        ProcessedOnUtc = DateTime.UtcNow;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
    }
}