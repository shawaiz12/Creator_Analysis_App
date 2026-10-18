using CreatorAnalytics.Integration.Contracts.Services;

namespace CreatorAnalytics.Strategy.Insights;

public sealed record DropOff(int StartSecond, int EndSecond, double PointsLost, double RetainedPercent);

public static class RetentionAnalyzer
{
    public const int WindowPoints = 10;

    /// <summary>
    /// Splits the video into ten equal windows and reports the windows where the most viewers
    /// leave, largest first. A loss below <paramref name="minimumPointsLost"/> percentage
    /// points is not worth reporting.
    /// </summary>
    public static IReadOnlyList<DropOff> FindDropOffs(
        IReadOnlyList<RetentionSample> points,
        int durationSeconds,
        int top = 3,
        double minimumPointsLost = 2.0)
    {
        if (points.Count != 100)
            throw new ArgumentException("Exactly 100 retention points are required.", nameof(points));

        if (durationSeconds <= 0)
            throw new ArgumentException("The duration must be positive.", nameof(durationSeconds));

        var windows = new List<DropOff>();

        for (var start = 0; start < points.Count; start += WindowPoints)
        {
            var end = start + WindowPoints - 1;

            var before = start == 0 ? 1.0 : points[start - 1].AudienceWatchRatio;
            var after = points[end].AudienceWatchRatio;
            var lost = (before - after) * 100.0;

            if (lost < minimumPointsLost)
                continue;

            var startRatio = start == 0 ? 0.0 : points[start - 1].ElapsedRatio;
            var endRatio = points[end].ElapsedRatio;

            windows.Add(new DropOff(
                (int)Math.Round(startRatio * durationSeconds),
                (int)Math.Round(endRatio * durationSeconds),
                Math.Round(lost, 1),
                Math.Round(after * 100.0, 1)));
        }

        return windows
            .OrderByDescending(w => w.PointsLost)
            .ThenBy(w => w.StartSecond)
            .Take(top)
            .ToList();
    }
}