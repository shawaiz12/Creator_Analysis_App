using CreatorAnalytics.Strategy.Domain;

namespace CreatorAnalytics.Strategy.Tests;

public class StrategyDocumentRevisionTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _videoId = Guid.NewGuid();

    private StrategyDocument NewDocument() => new(_tenantId, _videoId);

    [Fact]
    public void First_Revision_Is_Version_One()
    {
        var document = NewDocument();

        var revision = document.AddRevision("First draft", RevisionOrigin.Ai, null);

        Assert.Equal(1, revision.VersionNumber);
    }

    [Fact]
    public void Versions_Increase_And_Current_Is_Latest()
    {
        var document = NewDocument();
        document.AddRevision("AI draft", RevisionOrigin.Ai, null);

        var second = document.AddRevision("Edited by strategist", RevisionOrigin.Human, Guid.NewGuid());

        Assert.Equal(2, second.VersionNumber);
        Assert.Equal(2, document.Revisions.Count);
        Assert.Same(second, document.CurrentRevision);
    }

    [Fact]
    public void Current_Revision_Is_Null_When_There_Are_No_Revisions()
    {
        var document = NewDocument();

        Assert.Null(document.CurrentRevision);
    }

    [Fact]
    public void Empty_Content_Throws_ArgumentException()
    {
        var document = NewDocument();

        Assert.Throws<ArgumentException>(
            () => document.AddRevision("   ", RevisionOrigin.Human, Guid.NewGuid()));
    }

    [Fact]
    public void Content_Is_Frozen_While_Pending_Approval()
    {
        var document = NewDocument();
        document.SubmitForApproval();

        var exception = Assert.Throws<InvalidOperationException>(
            () => document.AddRevision("Late edit", RevisionOrigin.Human, Guid.NewGuid()));

        Assert.Contains("Cannot edit from PendingApproval", exception.Message);
    }

    [Fact]
    public void Revision_Can_Be_Added_After_Rejection()
    {
        var document = NewDocument();
        document.SubmitForApproval();
        document.Reject("Hook is weak.");

        var revision = document.AddRevision("Stronger hook", RevisionOrigin.Human, Guid.NewGuid());

        Assert.Equal(1, revision.VersionNumber);
    }
}