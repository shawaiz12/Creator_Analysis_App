using CreatorAnalytics.Strategy.Domain;

namespace CreatorAnalytics.Strategy.Tests;

public static class StrategyDocumentTestExtensions
{
    public static void AddContentAndSubmit(this StrategyDocument document)
    {
        document.AddRevision("Draft content", RevisionOrigin.Ai, null);
        document.SubmitForApproval();
    }
}