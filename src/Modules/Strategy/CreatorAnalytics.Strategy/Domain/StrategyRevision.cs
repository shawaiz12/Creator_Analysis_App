

using CreatorAnalytics.SharedKernel.Tenancy;

namespace CreatorAnalytics.Strategy.Domain
{
    public class StrategyRevision : IMustHaveTenant
    {
        public Guid Id { get; private set; }
        public Guid TenantId { get; private set; }
        public Guid StrategyDocumentId { get; private set; }
        public int VersionNumber { get; private set; }
        public string Content { get; private set; }
        public RevisionOrigin Origin { get; private set; }
        public Guid? AuthorUserId { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }

        internal StrategyRevision(
            Guid tenantId,
            Guid strategyDocumentId,
            int versionNumber,
            string content,
            RevisionOrigin origin,
            Guid? authorUserId)
        {
            Id = Guid.NewGuid();
            TenantId = tenantId;
            StrategyDocumentId = strategyDocumentId;
            VersionNumber = versionNumber;
            Content = content;
            Origin = origin;
            AuthorUserId = authorUserId;
            CreatedAtUtc = DateTime.UtcNow;
        }

        private StrategyRevision()
        {
            Content = string.Empty;
        }
    }
}