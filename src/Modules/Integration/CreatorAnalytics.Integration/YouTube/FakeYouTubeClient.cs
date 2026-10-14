using CreatorAnalytics.Integration.Domain;

namespace CreatorAnalytics.Integration.YouTube;

/// <summary>
/// A predictable stand-in for YouTube: every channel gets the same three videos,
/// and every video gets the same retention curve.
/// </summary>
public sealed class FakeYouTubeClient : IYouTubeClient
{
    private static readonly DateTime FirstPublished = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    public static readonly IReadOnlyList<YouTubeVideoInfo> Videos = new[]
    {
        new YouTubeVideoInfo("fake-video-1", "How I plan a month of videos", FirstPublished, 600),
        new YouTubeVideoInfo("fake-video-2", "Why my intros keep losing viewers", FirstPublished.AddDays(7), 480),
        new YouTubeVideoInfo("fake-video-3", "Thumbnail ideas that work", FirstPublished.AddDays(14), 720)
    };

    public Task<IReadOnlyList<YouTubeVideoInfo>> ListVideosAsync(
        string channelId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Videos);

    public Task<IReadOnlyList<RetentionPoint>> GetRetentionAsync(
        string videoId, CancellationToken cancellationToken = default)
    {
        // A steep drop over the first 10% of the video, then a slow decline to 20%.
        IReadOnlyList<RetentionPoint> points = Enumerable.Range(1, 100)
            .Select(i =>
            {
                var t = i / 100.0;
                var watch = t <= 0.10
                    ? 1.0 - 4.0 * t
                    : 0.6 - 0.4 * (t - 0.10) / 0.90;

                return new RetentionPoint(t, Math.Round(watch, 4));
            })
            .ToList();

        return Task.FromResult(points);
    }
}