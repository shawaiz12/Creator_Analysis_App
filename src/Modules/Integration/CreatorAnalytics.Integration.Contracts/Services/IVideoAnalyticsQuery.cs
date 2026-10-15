namespace CreatorAnalytics.Integration.Contracts.Services;

public sealed record VideoSummary(Guid VideoId, string Title, bool HasTranscript);

public interface IVideoAnalyticsQuery
{
    /// <summary>Finds a video in the current tenant. Returns null if it does not exist there.</summary>
    Task<VideoSummary?> FindVideoAsync(Guid videoId, CancellationToken cancellationToken = default);
}