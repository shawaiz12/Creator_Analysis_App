using CreatorAnalytics.Integration.Domain;
using CreatorAnalytics.Integration.Infrastructure;
using CreatorAnalytics.Integration.YouTube;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Integration.Services;

public enum SyncOutcome
{
    Succeeded,
    NotFound,
    NeedsReconnect
}

public sealed record SyncResult(SyncOutcome Outcome, int VideosAdded, int VideosUpdated, int SnapshotsStored)
{
    public static SyncResult NotFound() => new(SyncOutcome.NotFound, 0, 0, 0);

    public static SyncResult NeedsReconnect() => new(SyncOutcome.NeedsReconnect, 0, 0, 0);
}

public sealed class ChannelSyncService
{
    private readonly IntegrationDbContext _context;
    private readonly IYouTubeClient _client;

    public ChannelSyncService(IntegrationDbContext context, IYouTubeClient client)
    {
        _context = context;
        _client = client;
    }

    /// <summary>
    /// Pulls the channel's videos and retention from YouTube. Safe to run repeatedly:
    /// videos are inserted or updated by YouTube video id, and each run stores one
    /// new retention snapshot per video so history builds up over time.
    /// The caller must have set the current tenant.
    /// </summary>
    public async Task<SyncResult> SyncAsync(
        Guid channelConnectionId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var connection = await _context.ChannelConnections
            .SingleOrDefaultAsync(c => c.Id == channelConnectionId, cancellationToken);

        if (connection is null)
            return SyncResult.NotFound();

        if (connection.Status != ConnectionStatus.Connected)
            return SyncResult.NeedsReconnect();

        try
        {
            return await SyncCoreAsync(connection, nowUtc, cancellationToken);
        }
        catch (YouTubeAuthorizationExpiredException)
        {
            connection.MarkNeedsReconnect();
            await _context.SaveChangesAsync(cancellationToken);

            return SyncResult.NeedsReconnect();
        }
    }

    private async Task<SyncResult> SyncCoreAsync(
        ChannelConnection connection, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var remoteVideos = await _client.ListVideosAsync(connection.YouTubeChannelId, cancellationToken);

        var known = (await _context.Videos
                .Where(v => v.ChannelConnectionId == connection.Id)
                .ToListAsync(cancellationToken))
            .ToDictionary(v => v.YouTubeVideoId);

        var added = 0;
        var updated = 0;

        foreach (var info in remoteVideos)
        {
            if (known.TryGetValue(info.VideoId, out var video))
            {
                video.UpdateFromYouTube(info.Title, info.DurationSeconds, nowUtc);
                updated++;
            }
            else
            {
                video = new Video(
                    connection.TenantId, connection.Id, info.VideoId, info.Title,
                    info.PublishedAtUtc, info.DurationSeconds, nowUtc);

                _context.Videos.Add(video);
                known[info.VideoId] = video;
                added++;
            }
        }

        // Videos first, so the snapshots below always point at stored rows.
        await _context.SaveChangesAsync(cancellationToken);

        var snapshots = 0;

        foreach (var info in remoteVideos)
        {
            var points = await _client.GetRetentionAsync(info.VideoId, cancellationToken);

            _context.RetentionSnapshots.Add(
                new RetentionSnapshot(connection.TenantId, known[info.VideoId].Id, points, nowUtc));
            snapshots++;
        }

        connection.MarkSynced(nowUtc);
        await _context.SaveChangesAsync(cancellationToken);

        return new SyncResult(SyncOutcome.Succeeded, added, updated, snapshots);
    }
}