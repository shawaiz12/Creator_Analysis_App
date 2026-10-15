using CreatorAnalytics.Integration.Domain;
using CreatorAnalytics.Integration.Services;
using CreatorAnalytics.Integration.YouTube;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreatorAnalytics.Integration.Tests;

public class ChannelSyncDispatcherTests : IntegrationDatabaseTestBase
{
    private static readonly DateTime Now = new(2026, 10, 14, 9, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();

    private void AddConnection(Guid tenantId, string channelId = "UC-demo", bool needsReconnect = false)
    {
        using var context = NewContext(tenantId);
        var connection = new ChannelConnection(tenantId, channelId, "Studio", Guid.NewGuid(), Now);

        if (needsReconnect)
            connection.MarkNeedsReconnect();

        context.ChannelConnections.Add(connection);
        context.SaveChanges();
    }

    private static Task<int> RunOnce(ServiceProvider provider, DateTime at) =>
        provider.GetRequiredService<ChannelSyncDispatcher>().RunOnceAsync(at);

    [Fact]
    public async Task Every_Tenants_Due_Channels_Are_Synced_And_Kept_Apart()
    {
        AddConnection(_tenantA, "UC-a");
        AddConnection(_tenantB, "UC-b");
        using var provider = BuildProvider(new[] { _tenantA, _tenantB }, new FakeYouTubeClient());

        var synced = await RunOnce(provider, Now);

        Assert.Equal(2, synced);
        using var contextA = NewContext(_tenantA);
        using var contextB = NewContext(_tenantB);
        Assert.Equal(3, await contextA.Videos.CountAsync());
        Assert.Equal(3, await contextB.Videos.CountAsync());
    }

    [Fact]
    public async Task A_Channel_Is_Not_Synced_Again_Until_The_Interval_Has_Passed()
    {
        AddConnection(_tenantA);
        using var provider = BuildProvider(new[] { _tenantA }, new FakeYouTubeClient());

        var first = await RunOnce(provider, Now);
        var tooSoon = await RunOnce(provider, Now.AddHours(1));
        var dueAgain = await RunOnce(provider, Now.AddHours(7));

        Assert.Equal(1, first);
        Assert.Equal(0, tooSoon);
        Assert.Equal(1, dueAgain);
    }

    [Fact]
    public async Task A_Connection_That_Needs_Reconnecting_Is_Skipped()
    {
        AddConnection(_tenantA, needsReconnect: true);
        var stub = new StubYouTubeClient();
        using var provider = BuildProvider(new[] { _tenantA }, stub);

        var synced = await RunOnce(provider, Now);

        Assert.Equal(0, synced);
        Assert.Equal(0, stub.ListCalls);
    }

    [Fact]
    public async Task One_Failing_Channel_Does_Not_Stop_The_Others_And_Is_Not_Retried_At_Once()
    {
        AddConnection(_tenantA, "UC-good");
        AddConnection(_tenantA, "UC-bad");
        var stub = new StubYouTubeClient
        {
            ThrowOnList = new InvalidOperationException("boom"),
            FailOnlyForChannel = "UC-bad"
        };
        stub.Videos.AddRange(FakeYouTubeClient.Videos);
        using var provider = BuildProvider(new[] { _tenantA }, stub);

        var synced = await RunOnce(provider, Now);

        Assert.Equal(1, synced);

        using (var context = NewContext(_tenantA))
        {
            var good = await context.ChannelConnections.SingleAsync(c => c.YouTubeChannelId == "UC-good");
            var bad = await context.ChannelConnections.SingleAsync(c => c.YouTubeChannelId == "UC-bad");

            Assert.Equal<DateTime?>(Now, good.LastSyncedAtUtc);
            Assert.Equal<DateTime?>(Now, bad.LastSyncAttemptAtUtc);
            Assert.Null(bad.LastSyncedAtUtc);
        }

        var callsAfterFirstRun = stub.ListCalls;
        var secondRun = await RunOnce(provider, Now.AddHours(1));

        Assert.Equal(0, secondRun);
        Assert.Equal(callsAfterFirstRun, stub.ListCalls);
    }
}