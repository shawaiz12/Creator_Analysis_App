namespace CreatorAnalytics.Integration.Contracts.Services;

public sealed record VideoSummary(Guid VideoId, string Title, bool HasTranscript);

public sealed record RetentionSample(double ElapsedRatio, double AudienceWatchRatio);

public sealed record VideoInsightInput(
    Guid VideoId,
    string Title,
    int DurationSeconds,
    string? Transcript,
    IReadOnlyList<RetentionSample> LatestRetention);

public interface IVideoAnalyticsQuery
{
    /// <summary>Finds a video in the current tenant. Returns null if it does not exist there.</summary>
    Task<VideoSummary?> FindVideoAsync(Guid videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Everything the insight generator needs about a video: the transcript (if any) and the
    /// latest retention curve (empty if none has been fetched yet). Null if the video is not
    /// found in the current tenant.
    /// </summary>
    Task<VideoInsightInput?> GetInsightInputAsync(Guid videoId, CancellationToken cancellationToken = default);
}