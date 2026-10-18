using CreatorAnalytics.Integration.Contracts.Services;
using CreatorAnalytics.Strategy.Insights;

namespace CreatorAnalytics.Strategy.Tests;

public class RetentionAnalyzerTests
{
    // The same shape the fake YouTube client produces: a steep drop in the first 10%, then a slow decline.
    private static List<RetentionSample> StepCurve() =>
        Enumerable.Range(1, 100)
            .Select(i =>
            {
                var t = i / 100.0;
                var watch = t <= 0.10 ? 1.0 - 4.0 * t : 0.6 - 0.4 * (t - 0.10) / 0.90;
                return new RetentionSample(t, Math.Round(watch, 4));
            })
            .ToList();

    private static List<RetentionSample> Flat(double level) =>
        Enumerable.Range(1, 100).Select(i => new RetentionSample(i / 100.0, level)).ToList();

    [Fact]
    public void The_First_Window_Is_The_Biggest_Drop_For_A_Step_Curve()
    {
        var drops = RetentionAnalyzer.FindDropOffs(StepCurve(), 600);

        var first = drops[0];
        Assert.Equal(0, first.StartSecond);
        Assert.Equal(60, first.EndSecond);
        Assert.Equal(40.0, first.PointsLost);
        Assert.Equal(60.0, first.RetainedPercent);
    }

    [Fact]
    public void Results_Are_Sorted_Largest_First_And_Limited()
    {
        var drops = RetentionAnalyzer.FindDropOffs(StepCurve(), 600, top: 2);

        Assert.Equal(2, drops.Count);
        Assert.True(drops[0].PointsLost >= drops[1].PointsLost);
    }

    [Fact]
    public void A_Flat_Curve_Has_No_Drop_Offs()
    {
        Assert.Empty(RetentionAnalyzer.FindDropOffs(Flat(1.0), 600));
    }

    [Fact]
    public void Small_Losses_Below_The_Minimum_Are_Ignored()
    {
        // 0.1 percentage points lost per point is 1 per window, below the default minimum of 2.
        var gentle = Enumerable.Range(1, 100)
            .Select(i => new RetentionSample(i / 100.0, 1.0 - i / 1000.0))
            .ToList();

        Assert.Empty(RetentionAnalyzer.FindDropOffs(gentle, 600));
    }

    [Fact]
    public void The_Wrong_Number_Of_Points_Is_Refused()
    {
        var points = StepCurve().Take(99).ToList();

        Assert.Throws<ArgumentException>(() => RetentionAnalyzer.FindDropOffs(points, 600));
    }

    [Fact]
    public void Seconds_Scale_With_The_Length_Of_The_Video()
    {
        var drops = RetentionAnalyzer.FindDropOffs(StepCurve(), 1200);

        Assert.Equal(120, drops[0].EndSecond);
    }
}