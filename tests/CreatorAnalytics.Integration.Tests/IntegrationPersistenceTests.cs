using CreatorAnalytics.Integration.Domain;
using CreatorAnalytics.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Integration.Tests;

public class IntegrationPersistenceTests : IntegrationDatabaseTestBase
{
    private static readonly DateTime Now = new(2026, 10, 14, 9, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private static List<RetentionPoint> Points() =>
        Enumerable.Range(1, 100).Select(i => new RetentionPoint(i / 100.0, 1.0 - i / 200.0)).ToList();

    private (Guid ConnectionId, Guid VideoId) Seed(Guid tenantId, string channelId = "UC123", string videoId = "vid-1")
    {
        using var context = NewContext(tenantId);

        var connection = new ChannelConnection(tenantId, channelId, "Studio", _userId, Now);
        context.ChannelConnections.Add(connection);
        context.SaveChanges();

        var video = new Video(tenantId, connection.Id, videoId, "First video", Now.AddDays(-2), 600, Now);
        context.Videos.Add(video);
        context.SaveChanges();

        return (connection.Id, video.Id);
    }

    [Fact]
    public void A_Connection_A_Video_And_A_Snapshot_Round_Trip()
    {
        var (_, videoId) = Seed(_tenantA);

        using (var write = NewContext(_tenantA))
        {
            write.RetentionSnapshots.Add(new RetentionSnapshot(_tenantA, videoId, Points(), Now));
            write.SaveChanges();
        }

        using var read = NewContext(_tenantA);
        var snapshot = Assert.Single(read.RetentionSnapshots);

        Assert.Equal(videoId, snapshot.VideoId);
        Assert.Equal(100, snapshot.GetPoints().Count);
        Assert.Equal(ConnectionStatus.Connected, Assert.Single(read.ChannelConnections).Status);
    }

    [Fact]
    public void A_Long_Transcript_Is_Saved_And_Loaded()
    {
        var (_, videoId) = Seed(_tenantA);
        var transcript = new string('x', 50_000);

        using (var write = NewContext(_tenantA))
        {
            var video = write.Videos.Single(v => v.Id == videoId);
            video.SetTranscript(transcript);
            write.SaveChanges();
        }

        using var read = NewContext(_tenantA);
        Assert.Equal(transcript, read.Videos.Single(v => v.Id == videoId).Transcript);
    }

    [Fact]
    public void The_Same_YouTube_Video_Cannot_Be_Stored_Twice_In_One_Tenant()
    {
        var (connectionId, _) = Seed(_tenantA);

        using var context = NewContext(_tenantA);
        context.Videos.Add(new Video(_tenantA, connectionId, "vid-1", "Duplicate", Now, 300, Now));

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void The_Same_Channel_Cannot_Be_Connected_Twice_In_One_Tenant()
    {
        Seed(_tenantA);

        using var context = NewContext(_tenantA);
        context.ChannelConnections.Add(new ChannelConnection(_tenantA, "UC123", "Again", _userId, Now));

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void Two_Tenants_Can_Each_Hold_The_Same_YouTube_Video()
    {
        Seed(_tenantA);
        Seed(_tenantB);

        using var contextA = NewContext(_tenantA);
        using var contextB = NewContext(_tenantB);

        Assert.Single(contextA.Videos);
        Assert.Single(contextB.Videos);
    }

    [Fact]
    public void Another_Tenant_Sees_Nothing()
    {
        var (_, videoId) = Seed(_tenantA);
        using (var write = NewContext(_tenantA))
        {
            write.RetentionSnapshots.Add(new RetentionSnapshot(_tenantA, videoId, Points(), Now));
            write.SaveChanges();
        }

        using var other = NewContext(_tenantB);

        Assert.Empty(other.ChannelConnections.ToList());
        Assert.Empty(other.Videos.ToList());
        Assert.Empty(other.RetentionSnapshots.ToList());
    }

    [Fact]
    public void No_Tenant_Means_No_Data()
    {
        Seed(_tenantA);

        using var none = NewContext(null);

        Assert.Empty(none.ChannelConnections.ToList());
        Assert.Empty(none.Videos.ToList());
    }

    [Fact]
    public void Writing_Another_Tenants_Data_Is_Blocked()
    {
        using var context = NewContext(_tenantB);
        context.ChannelConnections.Add(new ChannelConnection(_tenantA, "UC999", "Foreign", _userId, Now));

        Assert.Throws<TenantViolationException>(() => context.SaveChanges());
    }
}