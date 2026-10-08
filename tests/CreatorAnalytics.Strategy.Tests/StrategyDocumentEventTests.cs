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
}