using System.Text.Json;
using CreatorAnalytics.SharedKernel.Tenancy;

namespace CreatorAnalytics.Integration.Domain;

public sealed record RetentionPoint(double ElapsedRatio, double AudienceWatchRatio);

public class RetentionSnapshot : IMustHaveTenant
{
    public const int ExpectedPoints = 100;

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid VideoId { get; private set; }
    public DateTime FetchedAtUtc { get; private set; }
    public string PointsJson { get; private set; }

    private RetentionSnapshot()
    {
        PointsJson = "[]";
    }

    public RetentionSnapshot(Guid tenantId, Guid videoId, IReadOnlyList<RetentionPoint> points, DateTime nowUtc)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (videoId == Guid.Empty) throw new ArgumentException("VideoId is required.", nameof(videoId));
        ValidatePoints(points);

        Id = Guid.NewGuid();
        TenantId = tenantId;
        VideoId = videoId;
        FetchedAtUtc = nowUtc;
        PointsJson = JsonSerializer.Serialize(points);
    }

    public IReadOnlyList<RetentionPoint> GetPoints() =>
        JsonSerializer.Deserialize<List<RetentionPoint>>(PointsJson) ?? new List<RetentionPoint>();

    private static void ValidatePoints(IReadOnlyList<RetentionPoint>? points)
    {
        if (points is null || points.Count != ExpectedPoints)
            throw new ArgumentException($"A retention snapshot needs exactly {ExpectedPoints} points.", nameof(points));

        var previous = 0.0;

        foreach (var point in points)
        {
            if (double.IsNaN(point.ElapsedRatio) || point.ElapsedRatio <= previous || point.ElapsedRatio > 1.0)
                throw new ArgumentException("Elapsed ratios must increase and stay within (0, 1].", nameof(points));

            if (double.IsNaN(point.AudienceWatchRatio) || point.AudienceWatchRatio < 0)
                throw new ArgumentException("An audience watch ratio cannot be negative.", nameof(points));

            previous = point.ElapsedRatio;
        }
    }
}