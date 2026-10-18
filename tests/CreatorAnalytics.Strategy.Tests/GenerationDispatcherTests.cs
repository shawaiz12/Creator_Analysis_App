using CreatorAnalytics.Integration.Contracts.Services;
using CreatorAnalytics.Strategy.Domain;
using CreatorAnalytics.Strategy.Infrastructure;
using CreatorAnalytics.Strategy.Insights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreatorAnalytics.Strategy.Tests;

public class GenerationDispatcherTests : DatabaseTestBase
{
    private static readonly DateTime Now = new(2026, 10, 17, 9, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _videoId = Guid.NewGuid();
    private readonly StubVideoAnalyticsQuery _videos = new();
    private readonly StubInsightGenerator _generator = new();

    private static List<RetentionSample> StepCurve() =>
        Enumerable.Range(1, 100)
            .Select(i =>
            {
                var t = i / 100.0;
                var watch = t <= 0.10 ? 1.0 - 4.0 * t : 0.6 - 0.4 * (t - 0.10) / 0.90;
                return new RetentionSample(t, Math.Round(watch, 4));
            })
            .ToList();

    private void GivenAVideo(IReadOnlyList<RetentionSample>? retention = null)
    {
        _videos.Input = new VideoInsightInput(
            _videoId, "How I plan a month of videos", 600, null, retention ?? StepCurve());
    }

    private (Guid DocumentId, Guid RequestId) SeedPending(Guid tenantId)
    {
        var document = new StrategyDocument(tenantId, _videoId);
        var request = document.RequestGeneration(_userId, Now);

        using var context = NewContext(tenantId);
        context.Documents.Add(document);
        context.SaveChanges();

        return (document.Id, request.Id);
    }

    private static StrategyDocument LoadFrom(StrategyDbContext context, Guid documentId) =>
        context.Documents
            .Include(d => d.Revisions)
            .Include(d => d.Reviews)
            .Include(d => d.GenerationRequests)
            .Single(d => d.Id == documentId);

    private static Task<int> RunOnce(ServiceProvider provider, DateTime at) =>
        provider.GetRequiredService<GenerationDispatcher>().RunOnceAsync(at);

    [Fact]
    public async Task A_Pending_Request_Becomes_An_Ai_Draft()
    {
        GivenAVideo();
        var (documentId, _) = SeedPending(_tenantA);
        using var provider = BuildProvider(new[] { _tenantA }, _videos, _generator);

        var completed = await RunOnce(provider, Now);

        Assert.Equal(1, completed);
        using var context = NewContext(_tenantA);
        var document = LoadFrom(context, documentId);
        Assert.Equal(RevisionOrigin.Ai, document.CurrentRevision!.Origin);
        Assert.Contains("How I plan a month of videos", document.CurrentRevision.Content);
        var request = Assert.Single(document.GenerationRequests);
        Assert.Equal(GenerationStatus.Succeeded, request.Status);
        Assert.Equal("template", request.Model);
    }

    [Fact]
    public async Task Requests_In_Every_Organization_Are_Fulfilled_And_Kept_Apart()
    {
        GivenAVideo();
        var (docA, _) = SeedPending(_tenantA);
        var (docB, _) = SeedPending(_tenantB);
        using var provider = BuildProvider(new[] { _tenantA, _tenantB }, _videos, _generator);

        var completed = await RunOnce(provider, Now);

        Assert.Equal(2, completed);
        using var contextA = NewContext(_tenantA);
        using var contextB = NewContext(_tenantB);
        Assert.Single(LoadFrom(contextA, docA).Revisions);
        Assert.Single(LoadFrom(contextB, docB).Revisions);
    }

    [Fact]
    public async Task A_Transient_Failure_Is_Retried_Later_With_A_Delay()
    {
        GivenAVideo();
        var (documentId, _) = SeedPending(_tenantA);
        _generator.ThrowOnGenerate = new InsightGenerationException("The provider timed out.", isTransient: true);
        using var provider = BuildProvider(new[] { _tenantA }, _videos, _generator);

        var first = await RunOnce(provider, Now);

        Assert.Equal(0, first);
        using (var context = NewContext(_tenantA))
        {
            var request = Assert.Single(LoadFrom(context, documentId).GenerationRequests);
            Assert.Equal(GenerationStatus.Pending, request.Status);
            Assert.Equal(1, request.Attempts);
            Assert.Equal(Now.AddSeconds(30), request.NextAttemptAtUtc);
        }

        var tooSoon = await RunOnce(provider, Now.AddSeconds(10));
        Assert.Equal(0, tooSoon);
        Assert.Equal(1, _generator.Calls);

        _generator.ThrowOnGenerate = null;
        var later = await RunOnce(provider, Now.AddSeconds(31));

        Assert.Equal(1, later);
        using var after = NewContext(_tenantA);
        Assert.Equal(GenerationStatus.Succeeded, Assert.Single(LoadFrom(after, documentId).GenerationRequests).Status);
    }

    [Fact]
    public async Task A_Permanent_Failure_Is_Final_After_One_Attempt()
    {
        GivenAVideo();
        var (documentId, _) = SeedPending(_tenantA);
        _generator.ThrowOnGenerate = new InsightGenerationException("The model refused the request.", isTransient: false);
        using var provider = BuildProvider(new[] { _tenantA }, _videos, _generator);

        await RunOnce(provider, Now);

        using var context = NewContext(_tenantA);
        var request = Assert.Single(LoadFrom(context, documentId).GenerationRequests);
        Assert.Equal(GenerationStatus.Failed, request.Status);
        Assert.Equal(1, request.Attempts);
        Assert.Equal("The model refused the request.", request.Error);
    }

    [Fact]
    public async Task Missing_Retention_Fails_With_A_Helpful_Message()
    {
        GivenAVideo(retention: new List<RetentionSample>());
        var (documentId, _) = SeedPending(_tenantA);
        using var provider = BuildProvider(new[] { _tenantA }, _videos, _generator);

        await RunOnce(provider, Now);

        using var context = NewContext(_tenantA);
        var request = Assert.Single(LoadFrom(context, documentId).GenerationRequests);
        Assert.Equal(GenerationStatus.Failed, request.Status);
        Assert.Contains("Sync the channel", request.Error);
        Assert.Equal(0, _generator.Calls);
    }

    [Fact]
    public async Task A_Strategy_Submitted_Meanwhile_Gets_No_Ai_Draft()
    {
        GivenAVideo();
        var (documentId, _) = SeedPending(_tenantA);

        using (var edit = NewContext(_tenantA))
        {
            var document = LoadFrom(edit, documentId);
            document.AddContentAndSubmit();
            edit.SaveChanges();
        }

        using var provider = BuildProvider(new[] { _tenantA }, _videos, _generator);

        var completed = await RunOnce(provider, Now);

        Assert.Equal(0, completed);
        using var context = NewContext(_tenantA);
        var after = LoadFrom(context, documentId);
        Assert.Single(after.Revisions);
        Assert.Equal(StrategyStatus.PendingApproval, after.Status);
        Assert.Equal(GenerationStatus.Failed, Assert.Single(after.GenerationRequests).Status);
    }

    [Fact]
    public async Task Earlier_Rejection_Reasons_Reach_The_Generator()
    {
        GivenAVideo();

        var rejected = new StrategyDocument(_tenantA, _videoId);
        rejected.AddContentAndSubmit();
        rejected.RejectCurrent("Avoid anything that needs 3D animation.");
        using (var seed = NewContext(_tenantA))
        {
            seed.Documents.Add(rejected);
            seed.SaveChanges();
        }

        SeedPending(_tenantA);
        using var provider = BuildProvider(new[] { _tenantA }, _videos, _generator);

        await RunOnce(provider, Now);

        Assert.Contains("Avoid anything that needs 3D animation.", _generator.LastInput!.RejectionReasons);
    }

    [Fact]
    public async Task Nothing_Due_Means_Nothing_Happens()
    {
        GivenAVideo();
        using var provider = BuildProvider(new[] { _tenantA }, _videos, _generator);

        var completed = await RunOnce(provider, Now);

        Assert.Equal(0, completed);
        Assert.Equal(0, _generator.Calls);
    }

    [Fact]
    public async Task An_Unexpected_Error_Is_Treated_As_Transient()
    {
        GivenAVideo();
        var (documentId, _) = SeedPending(_tenantA);
        _generator.ThrowOnGenerate = new Exception("boom");
        using var provider = BuildProvider(new[] { _tenantA }, _videos, _generator);

        await RunOnce(provider, Now);

        using var context = NewContext(_tenantA);
        var request = Assert.Single(LoadFrom(context, documentId).GenerationRequests);
        Assert.Equal(GenerationStatus.Pending, request.Status);
        Assert.Equal(1, request.Attempts);
    }
}