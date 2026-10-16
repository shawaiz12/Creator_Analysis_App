using CreatorAnalytics.Strategy.Domain;
using CreatorAnalytics.Strategy.Domain.Events;

namespace CreatorAnalytics.Strategy.Tests;

public class StrategyDocumentGenerationTests
{
    private static readonly DateTime Now = new(2026, 10, 15, 9, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _strategistId = Guid.NewGuid();

    private StrategyDocument NewDocument() => new(_tenantId, Guid.NewGuid());

    [Fact]
    public void Requesting_A_Draft_Records_A_Pending_Request_And_Raises_An_Event()
    {
        var document = NewDocument();

        var request = document.RequestGeneration(_strategistId, Now);

        Assert.Equal(GenerationStatus.Pending, request.Status);
        Assert.Equal(_strategistId, request.RequestedByUserId);
        Assert.Equal(Now, request.NextAttemptAtUtc);

        var raised = Assert.IsType<StrategyGenerationRequestedEvent>(Assert.Single(document.DomainEvents));
        Assert.Equal(request.Id, raised.RequestId);
        Assert.Equal(_strategistId, raised.RequestedByUserId);
    }

    [Fact]
    public void A_Strategy_Under_Review_Cannot_Request_A_Draft()
    {
        var document = NewDocument();
        document.AddContentAndSubmit();

        Assert.Throws<InvalidOperationException>(() => document.RequestGeneration(_strategistId, Now));
    }

    [Fact]
    public void Only_One_Request_Can_Be_Open_At_A_Time()
    {
        var document = NewDocument();
        document.RequestGeneration(_strategistId, Now);

        Assert.Throws<InvalidOperationException>(() => document.RequestGeneration(_strategistId, Now));
    }

    [Fact]
    public void Completing_A_Request_Adds_An_Ai_Revision_Without_An_Author()
    {
        var document = NewDocument();
        var request = document.RequestGeneration(_strategistId, Now);

        var revision = document.CompleteGeneration(
            request.Id, "Open with the result.", "template", "v1", Now.AddSeconds(5));

        Assert.Equal(RevisionOrigin.Ai, revision.Origin);
        Assert.Null(revision.AuthorUserId);
        Assert.Same(revision, document.CurrentRevision);
        Assert.Equal(GenerationStatus.Succeeded, request.Status);
        Assert.Equal("template", request.Model);
        Assert.Equal("v1", request.PromptVersion);
        Assert.Equal<DateTime?>(Now.AddSeconds(5), request.CompletedAtUtc);
    }

    [Fact]
    public void A_Rejected_Strategy_Can_Request_A_New_Draft()
    {
        var document = NewDocument();
        document.AddContentAndSubmit();
        document.RejectCurrent("Hook is weak.");

        var request = document.RequestGeneration(_strategistId, Now);

        Assert.Equal(GenerationStatus.Pending, request.Status);
    }

    [Fact]
    public void A_Strategy_Allows_At_Most_Five_Ai_Drafts()
    {
        var document = NewDocument();

        for (var i = 0; i < StrategyDocument.MaxAiRevisions; i++)
        {
            var request = document.RequestGeneration(_strategistId, Now);
            document.CompleteGeneration(request.Id, $"Draft {i}", "template", "v1", Now);
        }

        Assert.Throws<InvalidOperationException>(() => document.RequestGeneration(_strategistId, Now));
        Assert.Throws<InvalidOperationException>(
            () => document.AddRevision("One more", RevisionOrigin.Ai, null));
        Assert.Equal(StrategyDocument.MaxAiRevisions, document.Revisions.Count(r => r.Origin == RevisionOrigin.Ai));
    }

    [Fact]
    public void A_Request_Whose_Strategy_Moved_On_Adds_Nothing_And_Can_Be_Failed()
    {
        var document = NewDocument();
        var request = document.RequestGeneration(_strategistId, Now);
        document.AddContentAndSubmit(); // meanwhile someone submitted the strategy for review
        var revisionsBefore = document.Revisions.Count;

        Assert.Throws<InvalidOperationException>(
            () => document.CompleteGeneration(request.Id, "Too late", "template", "v1", Now));

        Assert.Equal(revisionsBefore, document.Revisions.Count);

        document.FailGeneration(request.Id, "The strategy is no longer editable.", permanent: true, Now);

        Assert.Equal(GenerationStatus.Failed, request.Status);
    }

    [Fact]
    public void Transient_Failures_Back_Off_And_The_Third_Is_Final()
    {
        var document = NewDocument();
        var request = document.RequestGeneration(_strategistId, Now);

        document.FailGeneration(request.Id, "timeout", permanent: false, Now);
        Assert.Equal(GenerationStatus.Pending, request.Status);
        Assert.Equal(1, request.Attempts);
        Assert.Equal(Now.AddSeconds(30), request.NextAttemptAtUtc);

        document.FailGeneration(request.Id, "timeout", permanent: false, Now.AddMinutes(1));
        Assert.Equal(Now.AddMinutes(1).AddSeconds(60), request.NextAttemptAtUtc);

        document.FailGeneration(request.Id, "timeout", permanent: false, Now.AddMinutes(5));
        Assert.Equal(GenerationStatus.Failed, request.Status);
        Assert.Equal(GenerationRequest.MaxAttempts, request.Attempts);
        Assert.Equal("timeout", request.Error);
    }

    [Fact]
    public void An_Unknown_Or_Finished_Request_Cannot_Be_Completed()
    {
        var document = NewDocument();
        var request = document.RequestGeneration(_strategistId, Now);
        document.CompleteGeneration(request.Id, "Draft", "template", "v1", Now);

        Assert.Throws<InvalidOperationException>(
            () => document.CompleteGeneration(Guid.NewGuid(), "x", "template", "v1", Now));
        Assert.Throws<InvalidOperationException>(
            () => document.CompleteGeneration(request.Id, "again", "template", "v1", Now));
    }

    [Fact]
    public void Revision_Content_Is_Length_Limited()
    {
        var document = NewDocument();

        Assert.Throws<ArgumentException>(() => document.AddRevision(
            new string('a', StrategyDocument.MaxRevisionLength + 1), RevisionOrigin.Human, _strategistId));
    }
}