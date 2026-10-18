using CreatorAnalytics.Integration.Domain;
using CreatorAnalytics.Integration.Services;

namespace CreatorAnalytics.Integration.Tests;

public class VideoAnalyticsQueryTests : IntegrationDatabaseTestBase
{
    private static readonly DateTime Now = new(2026, 10, 14, 9, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();

    private Guid SeedVideo(Guid tenantId)
    {
        using var context = NewContext(tenantId);

        var connection = new ChannelConnection(tenantId, "UC123", "Studio", Guid.NewGuid(), Now);
        context.ChannelConnections.Add(connection);
        context.SaveChanges();

        var video = new Video(tenantId, connection.Id, "vid-1", "First video", Now, 600, Now);
        context.Videos.Add(video);
        context.SaveChanges();

        return video.Id;
    }

    [Fact]
    public async Task A_Video_Is_Found_Inside_Its_Own_Tenant()
    {
        var videoId = SeedVideo(_tenantA);

        using var context = NewContext(_tenantA);
        var found = await new VideoAnalyticsQuery(context).FindVideoAsync(videoId);

        Assert.NotNull(found);
        Assert.Equal("First video", found!.Title);
        Assert.False(found.HasTranscript);
    }

    [Fact]
    public async Task Another_Tenants_Video_Is_Not_Found()
    {
        var videoId = SeedVideo(_tenantA);

        using var context = NewContext(_tenantB);
        var found = await new VideoAnalyticsQuery(context).FindVideoAsync(videoId);

        Assert.Null(found);
    }

    [Fact]
    public async Task An_Unknown_Video_Is_Not_Found()
    {
        using var context = NewContext(_tenantA);

        Assert.Null(await new VideoAnalyticsQuery(context).FindVideoAsync(Guid.NewGuid()));
    }

    private static List<RetentionPoint> Level(double level) =>
    Enumerable.Range(1, 100).Select(i => new RetentionPoint(i / 100.0, level)).ToList();

    [Fact]
    public async Task Insight_Input_Uses_The_Transcript_And_The_Latest_Snapshot()
    {
        var videoId = SeedVideo(_tenantA);

        using (var write = NewContext(_tenantA))
        {
            var video = write.Videos.Single(v => v.Id == videoId);
            video.SetTranscript("Hello and welcome.");
            write.RetentionSnapshots.Add(new RetentionSnapshot(_tenantA, videoId, Level(0.9), Now));
            write.RetentionSnapshots.Add(new RetentionSnapshot(_tenantA, videoId, Level(0.5), Now.AddDays(1)));
            write.SaveChanges();
        }

        using var context = NewContext(_tenantA);
        var input = await new VideoAnalyticsQuery(context).GetInsightInputAsync(videoId);

        Assert.NotNull(input);
        Assert.Equal("Hello and welcome.", input!.Transcript);
        Assert.Equal(600, input.DurationSeconds);
        Assert.Equal(100, input.LatestRetention.Count);
        Assert.All(input.LatestRetention, p => Assert.Equal(0.5, p.AudienceWatchRatio));
    }

    [Fact]
    public async Task Insight_Input_Without_Snapshots_Has_An_Empty_Retention_Curve()
    {
        var videoId = SeedVideo(_tenantA);

        using var context = NewContext(_tenantA);
        var input = await new VideoAnalyticsQuery(context).GetInsightInputAsync(videoId);

        Assert.NotNull(input);
        Assert.Empty(input!.LatestRetention);
        Assert.Null(input.Transcript);
    }

    [Fact]
    public async Task Another_Tenant_Gets_No_Insight_Input()
    {
        var videoId = SeedVideo(_tenantA);

        using var context = NewContext(_tenantB);
        var input = await new VideoAnalyticsQuery(context).GetInsightInputAsync(videoId);

        Assert.Null(input);
    }
}