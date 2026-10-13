using CreatorAnalytics.SharedKernel.Tenancy;

namespace CreatorAnalytics.Integration.Domain;

public class Video : IMustHaveTenant
{
    public const int MaxTranscriptLength = 200_000;

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ChannelConnectionId { get; private set; }
    public string YouTubeVideoId { get; private set; }
    public string Title { get; private set; }
    public DateTime PublishedAtUtc { get; private set; }
    public int DurationSeconds { get; private set; }
    public string? Transcript { get; private set; }
    public DateTime LastSyncedAtUtc { get; private set; }

    private Video()
    {
        YouTubeVideoId = string.Empty;
        Title = string.Empty;
    }

    public Video(
        Guid tenantId, Guid channelConnectionId, string youTubeVideoId, string title,
        DateTime publishedAtUtc, int durationSeconds, DateTime nowUtc)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (channelConnectionId == Guid.Empty) throw new ArgumentException("ChannelConnectionId is required.", nameof(channelConnectionId));
        if (string.IsNullOrWhiteSpace(youTubeVideoId)) throw new ArgumentException("The YouTube video id is required.", nameof(youTubeVideoId));
        ValidateDetails(title, durationSeconds);

        Id = Guid.NewGuid();
        TenantId = tenantId;
        ChannelConnectionId = channelConnectionId;
        YouTubeVideoId = youTubeVideoId.Trim();
        Title = title.Trim();
        PublishedAtUtc = publishedAtUtc;
        DurationSeconds = durationSeconds;
        LastSyncedAtUtc = nowUtc;
    }

    public void UpdateFromYouTube(string title, int durationSeconds, DateTime nowUtc)
    {
        ValidateDetails(title, durationSeconds);

        Title = title.Trim();
        DurationSeconds = durationSeconds;
        LastSyncedAtUtc = nowUtc;
    }

    public void SetTranscript(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
            throw new ArgumentException("The transcript cannot be blank.", nameof(transcript));

        if (transcript.Length > MaxTranscriptLength)
            throw new ArgumentException($"The transcript is longer than {MaxTranscriptLength} characters.", nameof(transcript));

        Transcript = transcript.Trim();
    }

    private static void ValidateDetails(string title, int durationSeconds)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("The video title is required.", nameof(title));
        if (durationSeconds <= 0) throw new ArgumentException("The duration must be positive.", nameof(durationSeconds));
    }
}