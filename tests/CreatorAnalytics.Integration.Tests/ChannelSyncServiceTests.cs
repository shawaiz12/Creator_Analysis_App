using CreatorAnalytics.Integration.Domain;
using CreatorAnalytics.Integration.Services;
using CreatorAnalytics.Integration.YouTube;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Integration.Tests;

public class ChannelSyncServiceTests : IntegrationDatabaseTestBase
{
    private static readonly DateTime Now = new(2026, 10, 14, 9, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private Guid AddConnection(Guid tenantId, bool needsReconnect = false)
    {
        using var context = NewContext(tenantId);
        var connection = new ChannelConnection(tenantId, "UC123", "Studio", _userId, Now);

        if (needsReconnect)
            connection.MarkNeedsReconnect();

        context.ChannelConnections.Add(connection);
        context.SaveChanges();
        return connection.Id;
    }

    private async Task<SyncResult> Sync(
        Guid tenantId, Guid connectionId, IYouTubeClient client, DateTime? at = null)
    {
        using var context = NewContext(tenantId);
        return await new ChannelSyncService(context, client).SyncAsync(connectionId, at ?? Now);
    }

    private static StubYouTubeClient StubWithTheFakeVideos()
    {
        var stub = new StubYouTubeClient();
        stub.Videos.AddRange(FakeYouTubeClient.Videos);
        return stub;
    }

    [Fact]
    public async Task The_First_Sync_Stores_Every_Video_And_A_Snapshot_For_Each()
    {
        var connectionId = AddConnection(_tenantA);

        var result = await Sync(_tenantA, connectionId, new FakeYouTubeClient());

        Assert.Equal(SyncOutcome.Succeeded, result.Outcome);
        Assert.Equal(3, result.VideosAdded);
        Assert.Equal(0, result.VideosUpdated);
        Assert.Equal(3, result.SnapshotsStored);

        using var context = NewContext(_tenantA);
        Assert.Equal(3, await context.Videos.CountAsync());
        Assert.Equal(3, await context.RetentionSnapshots.CountAsync());
        Assert.Equal<DateTime?>(Now, (await context.ChannelConnections.SingleAsync()).LastSyncedAtUtc);
    }

    [Fact]
    public async Task Syncing_Twice_Never_Duplicates_Videos_But_Keeps_Retention_History()
    {
        var connectionId = AddConnection(_tenantA);

        await Sync(_tenantA, connectionId, new FakeYouTubeClient());
        var second = await Sync(_tenantA, connectionId, new FakeYouTubeClient(), Now.AddHours(1));

        Assert.Equal(0, second.VideosAdded);
        Assert.Equal(3, second.VideosUpdated);

        using var context = NewContext(_tenantA);
        Assert.Equal(3, await context.Videos.CountAsync());
        Assert.Equal(6, await context.RetentionSnapshots.CountAsync());
        Assert.Equal<DateTime?>(Now.AddHours(1), (await context.ChannelConnections.SingleAsync()).LastSyncedAtUtc);
    }

    [Fact]
    public async Task A_Change_On_YouTube_Is_Picked_Up_By_The_Next_Sync()
    {
        var connectionId = AddConnection(_tenantA);
        var stub = StubWithTheFakeVideos();
        await Sync(_tenantA, connectionId, stub);

        stub.Videos[0] = stub.Videos[0] with { Title = "Renamed upstream", DurationSeconds = 650 };
        await Sync(_tenantA, connectionId, stub, Now.AddHours(1));

        using var context = NewContext(_tenantA);
        var video = await context.Videos.SingleAsync(v => v.YouTubeVideoId == "fake-video-1");
        Assert.Equal("Renamed upstream", video.Title);
        Assert.Equal(650, video.DurationSeconds);
    }

    [Fact]
    public async Task A_Transcript_Survives_A_Later_Sync()
    {
        var connectionId = AddConnection(_tenantA);
        await Sync(_tenantA, connectionId, new FakeYouTubeClient());

        using (var edit = NewContext(_tenantA))
        {
            var video = await edit.Videos.SingleAsync(v => v.YouTubeVideoId == "fake-video-1");
            video.SetTranscript("Hello and welcome to the channel.");
            await edit.SaveChangesAsync();
        }

        await Sync(_tenantA, connectionId, new FakeYouTubeClient(), Now.AddHours(1));

        using var read = NewContext(_tenantA);
        var after = await read.Videos.SingleAsync(v => v.YouTubeVideoId == "fake-video-1");
        Assert.Equal("Hello and welcome to the channel.", after.Transcript);
    }

    [Fact]
    public async Task Expired_Authorization_Marks_The_Connection_As_Needing_A_Reconnect()
    {
        var connectionId = AddConnection(_tenantA);
        var stub = new StubYouTubeClient { ThrowOnList = new YouTubeAuthorizationExpiredException() };

        var result = await Sync(_tenantA, connectionId, stub);

        Assert.Equal(SyncOutcome.NeedsReconnect, result.Outcome);

        using var context = NewContext(_tenantA);
        Assert.Equal(ConnectionStatus.NeedsReconnect, (await context.ChannelConnections.SingleAsync()).Status);
        Assert.Equal(0, await context.Videos.CountAsync());
    }

    [Fact]
    public async Task A_Connection_That_Needs_Reconnecting_Is_Not_Even_Tried()
    {
        var connectionId = AddConnection(_tenantA, needsReconnect: true);
        var stub = StubWithTheFakeVideos();

        var result = await Sync(_tenantA, connectionId, stub);

        Assert.Equal(SyncOutcome.NeedsReconnect, result.Outcome);
        Assert.Equal(0, stub.ListCalls);
    }

    [Fact]
    public async Task An_Unknown_Connection_Is_NotFound()
    {
        var result = await Sync(_tenantA, Guid.NewGuid(), new FakeYouTubeClient());

        Assert.Equal(SyncOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Another_Tenants_Connection_Cannot_Be_Synced()
    {
        var connectionId = AddConnection(_tenantA);

        var result = await Sync(_tenantB, connectionId, new FakeYouTubeClient());

        Assert.Equal(SyncOutcome.NotFound, result.Outcome);

        using var context = NewContext(_tenantA);
        Assert.Equal(0, await context.Videos.CountAsync());
    }
}