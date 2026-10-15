using CreatorAnalytics.Integration.Domain;
using CreatorAnalytics.Integration.Infrastructure;
using CreatorAnalytics.Integration.Services;
using CreatorAnalytics.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Integration.Endpoints;

public sealed record ConnectChannelRequest(string YouTubeChannelId, string Title);

public sealed record SetTranscriptRequest(string Transcript);

public sealed record ChannelDto(
    Guid Id, string YouTubeChannelId, string Title, string Status, DateTime? LastSyncedAtUtc);

public sealed record VideoDto(
    Guid Id, string YouTubeVideoId, string Title, DateTime PublishedAtUtc, int DurationSeconds, bool HasTranscript);

public sealed record RetentionDto(Guid VideoId, DateTime FetchedAtUtc, IReadOnlyList<RetentionPoint> Points);

public sealed record SyncResponse(int VideosAdded, int VideosUpdated, int SnapshotsStored);

public static class IntegrationEndpoints
{
    public static void MapIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/{tenantId}").RequireAuthorization();

        // TEMPORARY development shortcut: the real flow will connect a channel through
        // Google sign-in. Until then an Admin registers a channel by hand.
        group.MapPost("/channels", async (
            [FromRoute] Guid tenantId,
            [FromBody] ConnectChannelRequest request,
            [FromServices] ICurrentUser currentUser,
            [FromServices] IntegrationDbContext context) =>
        {
            if (currentUser.UserId is not Guid userId)
                return Results.Unauthorized();

            try
            {
                var connection = new ChannelConnection(
                    tenantId, request.YouTubeChannelId, request.Title, userId, DateTime.UtcNow);

                context.ChannelConnections.Add(connection);
                await context.SaveChangesAsync();

                return Results.Created($"/api/{tenantId}/channels", new { ChannelId = connection.Id });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { error = "This channel is already connected." });
            }
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/channels", async ([FromServices] IntegrationDbContext context) =>
        {
            var rows = await context.ChannelConnections
                .AsNoTracking()
                .OrderBy(c => c.Title)
                .ToListAsync();

            return Results.Ok(rows.Select(c => new ChannelDto(
                c.Id, c.YouTubeChannelId, c.Title, c.Status.ToString(), c.LastSyncedAtUtc)));
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Strategist"));

        group.MapPost("/channels/{id:guid}/sync", async (
            [FromRoute] Guid id,
            [FromServices] ChannelSyncService sync) =>
        {
            var result = await sync.SyncAsync(id, DateTime.UtcNow);

            return result.Outcome switch
            {
                SyncOutcome.Succeeded => Results.Ok(
                    new SyncResponse(result.VideosAdded, result.VideosUpdated, result.SnapshotsStored)),
                SyncOutcome.NotFound => Results.NotFound(),
                _ => Results.Conflict(new { error = "The YouTube connection needs to be reconnected." })
            };
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Strategist"));

        group.MapGet("/videos", async ([FromServices] IntegrationDbContext context) =>
        {
            // HasTranscript is computed in the database, so large transcripts are not loaded.
            var rows = await context.Videos
                .AsNoTracking()
                .OrderByDescending(v => v.PublishedAtUtc)
                .Select(v => new
                {
                    v.Id,
                    v.YouTubeVideoId,
                    v.Title,
                    v.PublishedAtUtc,
                    v.DurationSeconds,
                    HasTranscript = v.Transcript != null
                })
                .ToListAsync();

            return Results.Ok(rows.Select(v => new VideoDto(
                v.Id, v.YouTubeVideoId, v.Title, v.PublishedAtUtc, v.DurationSeconds, v.HasTranscript)));
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Strategist"));

        group.MapGet("/videos/{id:guid}/retention", async (
            [FromRoute] Guid id,
            [FromServices] IntegrationDbContext context) =>
        {
            var snapshot = await context.RetentionSnapshots
                .AsNoTracking()
                .Where(s => s.VideoId == id)
                .OrderByDescending(s => s.FetchedAtUtc)
                .FirstOrDefaultAsync();

            return snapshot is null
                ? Results.NotFound()
                : Results.Ok(new RetentionDto(snapshot.VideoId, snapshot.FetchedAtUtc, snapshot.GetPoints()));
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Strategist"));

        group.MapPut("/videos/{id:guid}/transcript", async (
            [FromRoute] Guid id,
            [FromBody] SetTranscriptRequest request,
            [FromServices] IntegrationDbContext context) =>
        {
            var video = await context.Videos.SingleOrDefaultAsync(v => v.Id == id);

            if (video is null)
                return Results.NotFound();

            try
            {
                video.SetTranscript(request.Transcript);
                await context.SaveChangesAsync();

                return Results.NoContent();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Strategist"));
    }
}