using CreatorAnalytics.SharedKernel.Tenancy;

namespace CreatorAnalytics.Integration.Domain;

public class ChannelConnection : IMustHaveTenant
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string YouTubeChannelId { get; private set; }
    public string Title { get; private set; }
    public ConnectionStatus Status { get; private set; }
    public Guid ConnectedByUserId { get; private set; }
    public DateTime ConnectedAtUtc { get; private set; }
    public DateTime? LastSyncedAtUtc { get; private set; }
    public DateTime? LastSyncAttemptAtUtc { get; private set; }

    private ChannelConnection()
    {
        YouTubeChannelId = string.Empty;
        Title = string.Empty;
    }

    public ChannelConnection(
        Guid tenantId, string youTubeChannelId, string title, Guid connectedByUserId, DateTime nowUtc)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (connectedByUserId == Guid.Empty) throw new ArgumentException("ConnectedByUserId is required.", nameof(connectedByUserId));
        if (string.IsNullOrWhiteSpace(youTubeChannelId)) throw new ArgumentException("The YouTube channel id is required.", nameof(youTubeChannelId));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("The channel title is required.", nameof(title));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        YouTubeChannelId = youTubeChannelId.Trim();
        Title = title.Trim();
        Status = ConnectionStatus.Connected;
        ConnectedByUserId = connectedByUserId;
        ConnectedAtUtc = nowUtc;
    }

    public void MarkSynced(DateTime nowUtc)
    {
        if (Status != ConnectionStatus.Connected)
            throw new InvalidOperationException($"Cannot sync a connection that is {Status}.");

        LastSyncedAtUtc = nowUtc;
    }

    public void RecordSyncAttempt(DateTime nowUtc) => LastSyncAttemptAtUtc = nowUtc;

    public void MarkNeedsReconnect() => Status = ConnectionStatus.NeedsReconnect;

    public void MarkReconnected() => Status = ConnectionStatus.Connected;
}