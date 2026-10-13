using CreatorAnalytics.Integration.Domain;

namespace CreatorAnalytics.Integration.Tests;

public class VideoTests
{
    private static readonly DateTime Now = new(2026, 10, 14, 9, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _connectionId = Guid.NewGuid();

    private Video NewVideo() => new(_tenantId, _connectionId, "vid-1", "My first video", Now.AddDays(-3), 600, Now);

    [Fact]
    public void A_New_Video_Has_No_Transcript()
    {
        var video = NewVideo();

        Assert.Null(video.Transcript);
        Assert.Equal(Now, video.LastSyncedAtUtc);
    }

    [Fact]
    public void A_Video_Must_Have_A_Positive_Duration()
    {
        Assert.Throws<ArgumentException>(
            () => new Video(_tenantId, _connectionId, "vid-1", "Title", Now, 0, Now));
    }

    [Fact]
    public void Updating_From_YouTube_Changes_Title_And_Duration()
    {
        var video = NewVideo();

        video.UpdateFromYouTube("Renamed", 650, Now.AddDays(1));

        Assert.Equal("Renamed", video.Title);
        Assert.Equal(650, video.DurationSeconds);
        Assert.Equal(Now.AddDays(1), video.LastSyncedAtUtc);
    }

    [Fact]
    public void A_Transcript_Is_Trimmed_And_Stored()
    {
        var video = NewVideo();

        video.SetTranscript("  Hello and welcome.  ");

        Assert.Equal("Hello and welcome.", video.Transcript);
    }

    [Fact]
    public void A_Blank_Transcript_Is_Refused()
    {
        var video = NewVideo();

        Assert.Throws<ArgumentException>(() => video.SetTranscript("   "));
    }

    [Fact]
    public void An_Overlong_Transcript_Is_Refused()
    {
        var video = NewVideo();

        Assert.Throws<ArgumentException>(
            () => video.SetTranscript(new string('a', Video.MaxTranscriptLength + 1)));
    }
}