using CreatorAnalytics.Integration.Contracts.Services;
using CreatorAnalytics.Strategy.Domain;
using CreatorAnalytics.Strategy.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Strategy.Insights;

public sealed class InsightInputBuilder
{
    public const int MaxTranscriptCharacters = 60_000;
    public const int MaxRejectionReasons = 5;

    private readonly StrategyDbContext _context;
    private readonly IVideoAnalyticsQuery _videos;

    public InsightInputBuilder(StrategyDbContext context, IVideoAnalyticsQuery videos)
    {
        _context = context;
        _videos = videos;
    }

    /// <summary>Gathers everything the generator may see. The caller must have set the tenant.</summary>
    public async Task<InsightInput> BuildAsync(Guid videoId, CancellationToken cancellationToken = default)
    {
        var video = await _videos.GetInsightInputAsync(videoId, cancellationToken)
            ?? throw new InsightGenerationException(
                "The video no longer exists in this organization.", isTransient: false);

        if (video.LatestRetention.Count == 0)
        {
            throw new InsightGenerationException(
                "No retention data has been fetched for this video yet. Sync the channel first, then try again.",
                isTransient: false);
        }

        var dropOffs = RetentionAnalyzer.FindDropOffs(video.LatestRetention, video.DurationSeconds);

        // The transcript is untrusted and can be huge, so only a bounded part is ever used.
        var transcript = video.Transcript;
        if (transcript is not null && transcript.Length > MaxTranscriptCharacters)
            transcript = transcript[..MaxTranscriptCharacters];

        var reasons = await RejectionReasonsAsync(videoId, cancellationToken);

        return new InsightInput(video.Title, video.DurationSeconds, transcript, dropOffs, reasons);
    }

    private async Task<IReadOnlyList<string>> RejectionReasonsAsync(
        Guid videoId, CancellationToken cancellationToken)
    {
        // Both tables carry the tenant filter, so only this organization's reviews are seen.
        var reasons = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.Decision == ReviewDecision.Rejected && r.Reason != null)
            .Join(_context.Documents,
                r => r.StrategyDocumentId,
                d => d.Id,
                (r, d) => new { r.Reason, r.ReviewedAtUtc, d.VideoId })
            .Where(x => x.VideoId == videoId)
            .OrderByDescending(x => x.ReviewedAtUtc)
            .Select(x => x.Reason!)
            .Take(MaxRejectionReasons * 2)
            .ToListAsync(cancellationToken);

        return reasons.Distinct().Take(MaxRejectionReasons).ToList();
    }
}