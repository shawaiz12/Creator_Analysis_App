using CreatorAnalytics.Integration.Contracts.Services;
using CreatorAnalytics.Strategy.Domain;
using CreatorAnalytics.Strategy.Insights;

namespace CreatorAnalytics.Strategy.Tests;

public class InsightInputBuilderTests : DatabaseTestBase
{
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly Guid _videoId = Guid.NewGuid();
    private readonly StubVideoAnalyticsQuery _videos = new();

    private static List<RetentionSample> StepCurve() =>
        Enumerable.Range(1, 100)
            .Select(i =>
            {
                var t = i / 100.0;
                var watch = t <= 0.10 ? 1.0 - 4.0 * t : 0.6 - 0.4 * (t - 0.10) / 0.90;
                return new RetentionSample(t, Math.Round(watch, 4));
            })
            .ToList();

    private void GivenAVideo(string? transcript = null, IReadOnlyList<RetentionSample>? retention = null)
    {
        _videos.Input = new VideoInsightInput(
            _videoId, "How I plan a month of videos", 600, transcript, retention ?? StepCurve());
    }

    private void SeedRejectedStrategy(Guid tenantId, Guid videoId, string reason)
    {
        var document = new StrategyDocument(tenantId, videoId);
        document.AddContentAndSubmit();
        document.RejectCurrent(reason);

        using var context = NewContext(tenantId);
        context.Documents.Add(document);
        context.SaveChanges();
    }

    private async Task<InsightInput> Build()
    {
        using var context = NewContext(_tenantA);
        return await new InsightInputBuilder(context, _videos).BuildAsync(_videoId);
    }

    [Fact]
    public async Task The_Facts_Come_From_The_Video_And_Its_Retention()
    {
        GivenAVideo(transcript: "Hello and welcome.");

        var input = await Build();

        Assert.Equal("How I plan a month of videos", input.VideoTitle);
        Assert.Equal(600, input.DurationSeconds);
        Assert.Equal("Hello and welcome.", input.Transcript);
        Assert.Equal(0, input.DropOffs[0].StartSecond);
        Assert.Equal(60, input.DropOffs[0].EndSecond);
    }

    [Fact]
    public async Task A_Missing_Video_Is_A_Permanent_Failure()
    {
        _videos.Input = null;

        var failure = await Assert.ThrowsAsync<InsightGenerationException>(Build);

        Assert.False(failure.IsTransient);
    }

    [Fact]
    public async Task Missing_Retention_Is_A_Permanent_Failure_With_A_Helpful_Message()
    {
        GivenAVideo(retention: new List<RetentionSample>());

        var failure = await Assert.ThrowsAsync<InsightGenerationException>(Build);

        Assert.False(failure.IsTransient);
        Assert.Contains("Sync the channel", failure.Message);
    }

    [Fact]
    public async Task Rejection_Reasons_For_The_Same_Video_Are_Included_And_Unrelated_Ones_Are_Not()
    {
        GivenAVideo();
        SeedRejectedStrategy(_tenantA, _videoId, "Too slow at the start.");
        SeedRejectedStrategy(_tenantA, _videoId, "Avoid anything that needs 3D animation.");
        SeedRejectedStrategy(_tenantA, Guid.NewGuid(), "About a different video.");

        var input = await Build();

        Assert.Contains("Too slow at the start.", input.RejectionReasons);
        Assert.Contains("Avoid anything that needs 3D animation.", input.RejectionReasons);
        Assert.DoesNotContain("About a different video.", input.RejectionReasons);
    }

    [Fact]
    public async Task Another_Organizations_Rejections_Are_Never_Included()
    {
        GivenAVideo();
        SeedRejectedStrategy(_tenantB, _videoId, "A secret from the other organization.");

        var input = await Build();

        Assert.Empty(input.RejectionReasons);
    }

    [Fact]
    public async Task The_Same_Reason_Is_Listed_Only_Once()
    {
        GivenAVideo();
        SeedRejectedStrategy(_tenantA, _videoId, "Too slow at the start.");
        SeedRejectedStrategy(_tenantA, _videoId, "Too slow at the start.");

        var input = await Build();

        Assert.Single(input.RejectionReasons);
    }

    [Fact]
    public async Task At_Most_Five_Reasons_Are_Included()
    {
        GivenAVideo();

        for (var i = 1; i <= 7; i++)
            SeedRejectedStrategy(_tenantA, _videoId, $"Reason number {i}.");

        var input = await Build();

        Assert.Equal(InsightInputBuilder.MaxRejectionReasons, input.RejectionReasons.Count);
    }

    [Fact]
    public async Task A_Huge_Transcript_Is_Cut_To_The_Limit()
    {
        GivenAVideo(transcript: new string('a', InsightInputBuilder.MaxTranscriptCharacters + 10_000));

        var input = await Build();

        Assert.Equal(InsightInputBuilder.MaxTranscriptCharacters, input.Transcript!.Length);
    }
}