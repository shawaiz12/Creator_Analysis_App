using CreatorAnalytics.Integration.Domain;
using CreatorAnalytics.Integration.YouTube;

namespace CreatorAnalytics.Integration.Tests;

public class FakeYouTubeClientTests
{
    private readonly FakeYouTubeClient _client = new();

    [Fact]
    public async Task Every_Channel_Gets_The_Same_Three_Videos()
    {
        var first = await _client.ListVideosAsync("UC-one");
        var second = await _client.ListVideosAsync("UC-two");

        Assert.Equal(3, first.Count);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task The_Retention_Curve_Is_Accepted_As_A_Snapshot()
    {
        var points = await _client.GetRetentionAsync("fake-video-1");

        var snapshot = new RetentionSnapshot(Guid.NewGuid(), Guid.NewGuid(), points, DateTime.UtcNow);

        Assert.Equal(100, snapshot.GetPoints().Count);
    }

    [Fact]
    public async Task The_Steepest_Drop_Is_In_The_First_Ten_Percent()
    {
        var points = await _client.GetRetentionAsync("fake-video-1");

        var steepestIndex = Enumerable.Range(0, points.Count - 1)
            .MaxBy(i => points[i].AudienceWatchRatio - points[i + 1].AudienceWatchRatio);

        Assert.True(points[steepestIndex + 1].ElapsedRatio <= 0.10 + 1e-9);
    }
}