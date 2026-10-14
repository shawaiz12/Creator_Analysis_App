using CreatorAnalytics.Integration.Domain;

namespace CreatorAnalytics.Integration.YouTube;

public sealed record YouTubeVideoInfo(
    string VideoId, string Title, DateTime PublishedAtUtc, int DurationSeconds);

/// <summary>Thrown when YouTube no longer accepts the stored authorization for a channel.</summary>
public sealed class YouTubeAuthorizationExpiredException : Exception
{
    public YouTubeAuthorizationExpiredException()
        : base("YouTube no longer accepts the stored authorization for this channel.")
    {
    }
}

public interface IYouTubeClient
{
    Task<IReadOnlyList<YouTubeVideoInfo>> ListVideosAsync(
        string channelId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetentionPoint>> GetRetentionAsync(
        string videoId, CancellationToken cancellationToken = default);
}