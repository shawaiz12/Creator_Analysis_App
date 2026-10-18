namespace CreatorAnalytics.Strategy.Insights;

public sealed record InsightInput(
    string VideoTitle,
    int DurationSeconds,
    string? Transcript,
    IReadOnlyList<DropOff> DropOffs,
    IReadOnlyList<string> RejectionReasons);

public sealed record InsightResult(string Content, string Model, string PromptVersion);

/// <summary>A generator failure. Transient failures are worth retrying; others are final.</summary>
public sealed class InsightGenerationException : Exception
{
    public InsightGenerationException(string message, bool isTransient, Exception? inner = null)
        : base(message, inner)
    {
        IsTransient = isTransient;
    }

    public bool IsTransient { get; }
}

public interface IInsightGenerator
{
    Task<InsightResult> GenerateAsync(InsightInput input, CancellationToken cancellationToken = default);
}