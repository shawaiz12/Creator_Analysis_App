
using CreatorAnalytics.SharedKernel.Tenancy;

namespace CreatorAnalytics.Strategy.Domain
{
    public class StrategyReview : IMustHaveTenant
    {
        public Guid Id { get; private set; }
        public Guid TenantId { get; private set; }
        public Guid StrategyDocumentId { get; private set; }
        public Guid RevisionId { get; private set; }
        public Guid ReviewerUserId { get; private set; }
        public ReviewDecision Decision { get; private set; }
        public string? Reason { get; private set; }
        public DateTime ReviewedAtUtc { get; private set; }

        internal StrategyReview(
            Guid tenantId,
            Guid strategyDocumentId,
            Guid revisionId,
            Guid reviewerUserId,
            ReviewDecision decision,
            string? reason)
        {
            Id = Guid.NewGuid();
            TenantId = tenantId;
            StrategyDocumentId = strategyDocumentId;
            RevisionId = revisionId;
            ReviewerUserId = reviewerUserId;
            Decision = decision;
            Reason = reason;
            ReviewedAtUtc = DateTime.UtcNow;
        }

        private StrategyReview() { }
    }
}
