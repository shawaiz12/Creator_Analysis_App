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
}