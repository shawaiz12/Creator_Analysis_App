using CreatorAnalytics.Strategy.Domain;
namespace CreatorAnalytics.Strategy.Tests
{
    public class StrategyDocumentReviewTests
    {
        private readonly Guid _reviewerId = Guid.NewGuid();

        private static StrategyDocument SubmittedDocument()
        {
            var document = new StrategyDocument(Guid.NewGuid(), Guid.NewGuid());
            document.AddContentAndSubmit();
            return document;
        }


        [Fact]
        public void Approve_Records_A_Review_For_The_Current_Revision()
        {
            var document = SubmittedDocument();
            var revisionId = document.CurrentRevision!.Id;

            document.Approve(_reviewerId, revisionId);

            var review = Assert.Single(document.Reviews);
            Assert.Equal(ReviewDecision.Approved, review.Decision);
            Assert.Equal(_reviewerId, review.ReviewerUserId);
            Assert.Equal(revisionId, review.RevisionId);
            Assert.Null(review.Reason);
        }

        [Fact]
        public void Reject_Records_The_Reason()
        {
            var document = SubmittedDocument();

            document.Reject(_reviewerId, document.CurrentRevision!.Id, "Hook is weak.");

            var review = Assert.Single(document.Reviews);
            Assert.Equal(ReviewDecision.Rejected, review.Decision);
            Assert.Equal("Hook is weak.", review.Reason);
        }

        [Fact]
        public void Approving_A_Stale_Revision_Is_Refused()
        {
            var document = SubmittedDocument();

            var exception = Assert.Throws<InvalidOperationException>(
                () => document.Approve(_reviewerId, Guid.NewGuid()));

            Assert.Contains("changed", exception.Message);
            Assert.Equal(StrategyStatus.PendingApproval, document.Status);
            Assert.Empty(document.Reviews);
        }
        [Fact]
        public void Reviews_Accumulate_Across_Rejection_And_Approval()
        {
            var document = SubmittedDocument();
            document.Reject(_reviewerId, document.CurrentRevision!.Id, "Too slow.");
            document.AddRevision("Faster pacing", RevisionOrigin.Human, Guid.NewGuid());
            document.SubmitForApproval();

            document.Approve(_reviewerId, document.CurrentRevision!.Id);

            Assert.Equal(2, document.Reviews.Count);
            Assert.Equal(ReviewDecision.Rejected, document.Reviews[0].Decision);
            Assert.Equal(ReviewDecision.Approved, document.Reviews[1].Decision);
        }
    }
}
