using CreatorAnalytics.Integration.Domain;

namespace CreatorAnalytics.Integration.Tests;

public class ChannelConnectionTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 9, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private ChannelConnection NewConnection() => new(_tenantId, "UC123", "Studio Channel", _userId, Now);

    [Fact]
    public void A_New_Connection_Is_Connected_And_Remembers_Who_Connected_It()
    {
        var connection = NewConnection();

        Assert.Equal(ConnectionStatus.Connected, connection.Status);
        Assert.Equal(_userId, connection.ConnectedByUserId);
        Assert.Equal(Now, connection.ConnectedAtUtc);
        Assert.Null(connection.LastSyncedAtUtc);
    }

    [Fact]
    public void A_Blank_Channel_Id_Is_Refused()
    {
        Assert.Throws<ArgumentException>(() => new ChannelConnection(_tenantId, "  ", "Title", _userId, Now));
    }

    [Fact]
    public void A_Blank_Title_Is_Refused()
    {
        Assert.Throws<ArgumentException>(() => new ChannelConnection(_tenantId, "UC123", " ", _userId, Now));
    }

    [Fact]
    public void Marking_Synced_Records_The_Time()
    {
        var connection = NewConnection();

        connection.MarkSynced(Now.AddHours(1));

        Assert.Equal(Now.AddHours(1), connection.LastSyncedAtUtc);
    }

    [Fact]
    public void A_Connection_That_Needs_Reconnecting_Cannot_Be_Synced()
    {
        var connection = NewConnection();
        connection.MarkNeedsReconnect();

        Assert.Throws<InvalidOperationException>(() => connection.MarkSynced(Now));
    }

    [Fact]
    public void Reconnecting_Allows_Syncing_Again()
    {
        var connection = NewConnection();
        connection.MarkNeedsReconnect();
        connection.MarkReconnected();

        connection.MarkSynced(Now);

        Assert.Equal(ConnectionStatus.Connected, connection.Status);
        Assert.Equal(Now, connection.LastSyncedAtUtc);
    }

    [Fact]
    public void Recording_A_Sync_Attempt_Stores_The_Time()
    {
        var connection = NewConnection();

        connection.RecordSyncAttempt(Now.AddMinutes(5));

        Assert.Equal<DateTime?>(Now.AddMinutes(5), connection.LastSyncAttemptAtUtc);
        Assert.Null(connection.LastSyncedAtUtc);
    }
}