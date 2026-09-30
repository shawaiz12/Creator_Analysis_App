using System;
using CreatorAnalytics.Strategy.Domain;
using Xunit;

namespace CreatorAnalytics.Strategy.Tests
{
    public class StrategyDocumentTests
    {
        private readonly Guid _tenantId = Guid.NewGuid();
        private readonly Guid _videoId = Guid.NewGuid();

        [Fact]
        public void New_Document_Starts_In_Draft_State()
        {
            var document = new StrategyDocument(_tenantId, _videoId);
            Assert.Equal(StrategyStatus.Draft, document.Status);
        }

        [Fact]
        public void Valid_Lifecycle_Transitions_Succeed()
        {
            var document = new StrategyDocument(_tenantId, _videoId);

            document.SubmitForApproval();
            Assert.Equal(StrategyStatus.PendingApproval, document.Status);

            document.Approve();
            Assert.Equal(StrategyStatus.Approved, document.Status);

            document.MarkImplemented();
            Assert.Equal(StrategyStatus.Implemented, document.Status);
        }

        [Fact]
        public void Reject_With_Reason_Transitions_To_NeedsRevision()
        {
            var document = new StrategyDocument(_tenantId, _videoId);
            document.SubmitForApproval();

            document.Reject("Pacing is too slow in the first 30 seconds.");
            Assert.Equal(StrategyStatus.NeedsRevision, document.Status);
        }

        [Fact]
        public void Illegal_Transition_Throws_InvalidOperationException()
        {
            var document = new StrategyDocument(_tenantId, _videoId);

            // Cannot approve a Draft directly
            var exception = Assert.Throws<InvalidOperationException>(() => document.Approve());
            Assert.Contains("Cannot approve from Draft", exception.Message);
        }

        [Fact]
        public void Reject_Without_Reason_Throws_ArgumentException()
        {
            var document = new StrategyDocument(_tenantId, _videoId);
            document.SubmitForApproval();

            // Passing an empty string should fail
            Assert.Throws<ArgumentException>(() => document.Reject("   "));
        }
    }
}
