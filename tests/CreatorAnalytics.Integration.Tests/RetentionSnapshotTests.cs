using CreatorAnalytics.Integration.Domain;

namespace CreatorAnalytics.Integration.Tests;

public class RetentionSnapshotTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 9, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _videoId = Guid.NewGuid();

    private static List<RetentionPoint> Points(Func<int, double>? watch = null)
    {
        watch ??= n => 1.0 - n / 200.0;

        return Enumerable.Range(1, 100)
            .Select(i => new RetentionPoint(i / 100.0, watch(i)))
            .ToList();
    }

    [Fact]
    public void Valid_Points_Round_Trip_Through_The_Snapshot()
    {
        var snapshot = new RetentionSnapshot(_tenantId, _videoId, Points(), Now);

        var points = snapshot.GetPoints();

        Assert.Equal(100, points.Count);
        Assert.Equal(0.01, points[0].ElapsedRatio);
        Assert.Equal(1.0, points[99].ElapsedRatio);
        Assert.Equal(1.0 - 1 / 200.0, points[0].AudienceWatchRatio);
    }

    [Fact]
    public void The_Wrong_Number_Of_Points_Is_Refused()
    {
        var points = Points().Take(99).ToList();

        Assert.Throws<ArgumentException>(() => new RetentionSnapshot(_tenantId, _videoId, points, Now));
    }

    [Fact]
    public void Elapsed_Ratios_Must_Increase()
    {
        var points = Points();
        points[50] = points[50] with { ElapsedRatio = points[49].ElapsedRatio };

        Assert.Throws<ArgumentException>(() => new RetentionSnapshot(_tenantId, _videoId, points, Now));
    }

    [Fact]
    public void A_Negative_Watch_Ratio_Is_Refused()
    {
        var points = Points(n => n == 40 ? -0.1 : 0.5);

        Assert.Throws<ArgumentException>(() => new RetentionSnapshot(_tenantId, _videoId, points, Now));
    }

    [Fact]
    public void A_Watch_Ratio_Above_One_Is_Allowed_Because_Of_Replays()
    {
        var points = Points(n => n == 10 ? 1.2 : 0.5);

        var snapshot = new RetentionSnapshot(_tenantId, _videoId, points, Now);

        Assert.Equal(1.2, snapshot.GetPoints()[9].AudienceWatchRatio);
    }

    [Fact]
    public void Empty_Ids_Are_Refused()
    {
        Assert.Throws<ArgumentException>(() => new RetentionSnapshot(Guid.Empty, _videoId, Points(), Now));
        Assert.Throws<ArgumentException>(() => new RetentionSnapshot(_tenantId, Guid.Empty, Points(), Now));
    }
}