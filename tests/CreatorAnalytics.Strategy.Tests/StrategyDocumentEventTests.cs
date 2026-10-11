using CreatorAnalytics.Strategy.Domain;
using CreatorAnalytics.Strategy.Domain.Events;

namespace CreatorAnalytics.Strategy.Tests;

public class StrategyDocumentEventTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _reviewerId = Guid.NewGuid();

    private StrategyDocument SubmittedDocument()
    {
        var document = new StrategyDocument(_tenantId, Guid.NewGuid());
        document.AddContentAndSubmit();
        document.ClearDomainEvents();
        return document;
    }

    [Fact]
    public void Approve_Raises_One_Approved_Event()
    {
        var document = SubmittedDocument();

        document.Approve(_reviewerId, document.CurrentRevision!.Id);

        Assert.IsType<StrategyApprovedEvent>(Assert.Single(document.DomainEvents));
    }

    [Fact]
    public void Reject_Raises_A_Rejected_Event_With_Reviewer_And_Reason()
    {
        var document = SubmittedDocument();

        document.Reject(_reviewerId, document.CurrentRevision!.Id, "Hook is weak.");

        var raised = Assert.IsType<StrategyRejectedEvent>(Assert.Single(document.DomainEvents));
        Assert.Equal(_reviewerId, raised.ReviewerId);
        Assert.Equal("Hook is weak.", raised.Reason);
        Assert.Equal(_tenantId, raised.TenantId);
    }

    [Fact]
    public void A_Refused_Decision_Raises_No_Event()
    {
        var document = SubmittedDocument();

        Assert.Throws<InvalidOperationException>(
            () => document.Approve(_reviewerId, Guid.NewGuid()));

        Assert.Empty(document.DomainEvents);
    }

    [Fact]
    public void Adding_A_Revision_Raises_A_Revision_Added_Event()
    {
        var document = new StrategyDocument(_tenantId, Guid.NewGuid());
        var author = Guid.NewGuid();

        var revision = document.AddRevision("Draft", RevisionOrigin.Human, author);

        var raised = Assert.IsType<StrategyRevisionAddedEvent>(Assert.Single(document.DomainEvents));
        Assert.Equal(revision.Id, raised.RevisionId);
        Assert.Equal(1, raised.VersionNumber);
        Assert.Equal(author, raised.AuthorUserId);
    }

    [Fact]
    public void Submitting_Raises_A_Submitted_Event_With_The_Submitter()
    {
        var document = new StrategyDocument(_tenantId, Guid.NewGuid());
        document.AddRevision("Draft", RevisionOrigin.Ai, null);
        document.ClearDomainEvents();
        var submitter = Guid.NewGuid();

        document.SubmitForApproval(submitter);

        var raised = Assert.IsType<StrategySubmittedEvent>(Assert.Single(document.DomainEvents));
        Assert.Equal(submitter, raised.SubmittedByUserId);
        Assert.Equal(document.CurrentRevision!.Id, raised.RevisionId);
    }

    [Fact]
    public void Marking_Implemented_Raises_An_Implemented_Event()
    {
        var document = SubmittedDocument();
        document.Approve(_reviewerId, document.CurrentRevision!.Id);
        document.ClearDomainEvents();
        var editor = Guid.NewGuid();

        document.MarkImplemented(editor);

        var raised = Assert.IsType<StrategyImplementedEvent>(Assert.Single(document.DomainEvents));
        Assert.Equal(editor, raised.ImplementedByUserId);
    }

    [Fact]
    public void A_Refused_Submit_Raises_No_Event()
    {
        var document = new StrategyDocument(_tenantId, Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => document.SubmitForApproval(Guid.NewGuid()));

        Assert.Empty(document.DomainEvents);
    }
}