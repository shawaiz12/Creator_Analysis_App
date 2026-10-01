using CreatorAnalytics.Strategy.Domain;

namespace CreatorAnalytics.Strategy.Tests;

public static class StrategyDocumentTestExtensions
{
    public static void AddContentAndSubmit(this StrategyDocument document)
    {
        document.AddRevision("Draft content", RevisionOrigin.Ai, null);
        document.SubmitForApproval();
    }

    public static void ApproveCurrent(this StrategyDocument document)
    => document.Approve(Guid.NewGuid(), document.CurrentRevision?.Id ?? Guid.NewGuid());

    public static void RejectCurrent(this StrategyDocument document, string reason)
        => document.Reject(Guid.NewGuid(), document.CurrentRevision?.Id ?? Guid.NewGuid(), reason);
}