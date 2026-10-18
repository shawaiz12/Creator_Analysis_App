using CreatorAnalytics.Integration.Contracts.Services;

namespace CreatorAnalytics.Strategy.Tests;

/// <summary>A stand-in for the Integration module whose answer the test controls.</summary>
public sealed class StubVideoAnalyticsQuery : IVideoAnalyticsQuery
{
    public VideoInsightInput? Input { get; set; }

    public Task<VideoSummary?> FindVideoAsync(Guid videoId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Input is null
            ? null
            : new VideoSummary(Input.VideoId, Input.Title, Input.Transcript is not null));

    public Task<VideoInsightInput?> GetInsightInputAsync(Guid videoId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Input);
}