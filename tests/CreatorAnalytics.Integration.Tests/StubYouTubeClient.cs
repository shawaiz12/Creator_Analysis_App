using CreatorAnalytics.Integration.Domain;
using CreatorAnalytics.Integration.YouTube;

namespace CreatorAnalytics.Integration.Tests;

/// <summary>A test double whose answers (and failures) the test controls.</summary>
public sealed class StubYouTubeClient : IYouTubeClient
{
    public List<YouTubeVideoInfo> Videos { get; } = new();

    public Exception? ThrowOnList { get; set; }
    public string? FailOnlyForChannel { get; set; }

    public int ListCalls { get; private set; }

    public Task<IReadOnlyList<YouTubeVideoInfo>> ListVideosAsync(
        string channelId, CancellationToken cancellationToken = default)
    {
        ListCalls++;

        if (ThrowOnList is not null && (FailOnlyForChannel is null || FailOnlyForChannel == channelId))
            throw ThrowOnList;


        return Task.FromResult<IReadOnlyList<YouTubeVideoInfo>>(Videos.ToList());
    }

    public Task<IReadOnlyList<RetentionPoint>> GetRetentionAsync(
        string videoId, CancellationToken cancellationToken = default) =>
        new FakeYouTubeClient().GetRetentionAsync(videoId, cancellationToken);
}