using CreatorAnalytics.Integration.Contracts.Services;
using CreatorAnalytics.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Integration.Services;

public sealed class VideoAnalyticsQuery : IVideoAnalyticsQuery
{
    private readonly IntegrationDbContext _context;

    public VideoAnalyticsQuery(IntegrationDbContext context)
    {
        _context = context;
    }

    public async Task<VideoSummary?> FindVideoAsync(
        Guid videoId, CancellationToken cancellationToken = default)
    {
        // The tenant filter on the context means another tenant's video is simply not found.
        var video = await _context.Videos
            .AsNoTracking()
            .Where(v => v.Id == videoId)
            .Select(v => new { v.Id, v.Title, HasTranscript = v.Transcript != null })
            .SingleOrDefaultAsync(cancellationToken);

        return video is null ? null : new VideoSummary(video.Id, video.Title, video.HasTranscript);
    }

    public async Task<VideoInsightInput?> GetInsightInputAsync(
        Guid videoId, CancellationToken cancellationToken = default)
    {
        var video = await _context.Videos
            .AsNoTracking()
            .Where(v => v.Id == videoId)
            .Select(v => new { v.Id, v.Title, v.DurationSeconds, v.Transcript })
            .SingleOrDefaultAsync(cancellationToken);

        if (video is null)
            return null;

        var snapshot = await _context.RetentionSnapshots
            .AsNoTracking()
            .Where(s => s.VideoId == videoId)
            .OrderByDescending(s => s.FetchedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var samples = snapshot is null
            ? new List<RetentionSample>()
            : snapshot.GetPoints()
                .Select(p => new RetentionSample(p.ElapsedRatio, p.AudienceWatchRatio))
                .ToList();

        return new VideoInsightInput(
            video.Id, video.Title, video.DurationSeconds, video.Transcript, samples);
    }
}