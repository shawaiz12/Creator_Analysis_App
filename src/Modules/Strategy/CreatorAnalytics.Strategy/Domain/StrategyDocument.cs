using CreatorAnalytics.SharedKernel.Tenancy;
using System;



namespace CreatorAnalytics.Strategy.Domain
{
    public class StrategyDocument : IMustHaveTenant
    {
        private readonly List<object> _domainEvents = new();
        public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();
        public Guid Id { get; private set; }
        public Guid TenantId { get; private set; }
        public Guid VideoId { get; private set; }
        public StrategyStatus Status { get; private set; }

        public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

        public StrategyDocument(Guid tenantId, Guid videoId)
        {
            Id = Guid.NewGuid();
            TenantId = tenantId;
            VideoId = videoId;
            Status = StrategyStatus.Draft;
        }
        private StrategyDocument() { }


        private readonly List<StrategyRevision> _revisions = new();

        public IReadOnlyList<StrategyRevision> Revisions => _revisions;

        private readonly List<StrategyReview> _reviews = new();

        public IReadOnlyList<StrategyReview> Reviews => _reviews;

        public StrategyRevision? CurrentRevision =>
     _revisions.MaxBy(r => r.VersionNumber);

        public StrategyRevision AddRevision(string content, RevisionOrigin origin, Guid? authorUserId)
        {
            if (Status != StrategyStatus.Draft && Status != StrategyStatus.NeedsRevision)
                throw new InvalidOperationException(
                    $"Cannot edit from {Status}. Content is frozen outside Draft or NeedsRevision.");

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Revision content is required.", nameof(content));

            var revision = new StrategyRevision(
                TenantId, Id, (CurrentRevision?.VersionNumber ?? 0) + 1, content, origin, authorUserId);

            _revisions.Add(revision);

            _domainEvents.Add(new Events.StrategyRevisionAddedEvent(
    Id, TenantId, revision.Id, revision.VersionNumber,
    origin.ToString(), authorUserId, DateTime.UtcNow));
            return revision;
        }
        public void SubmitForApproval(Guid submittedByUserId)
        {
            if (Status != StrategyStatus.Draft && Status != StrategyStatus.NeedsRevision)
                throw new InvalidOperationException($"Cannot submit from {Status}. Must be Draft or NeedsRevision.");

            if (_revisions.Count == 0)
                throw new InvalidOperationException("Cannot submit a strategy with no content. Add a revision first.");

            Status = StrategyStatus.PendingApproval;
            _domainEvents.Add(new Events.StrategySubmittedEvent(
    Id, TenantId, submittedByUserId, CurrentRevision!.Id, DateTime.UtcNow));
        }

        public void Approve(Guid reviewerUserId, Guid revisionId)
        {
            if (Status != StrategyStatus.PendingApproval)
                throw new InvalidOperationException($"Cannot approve from {Status}. Must be PendingApproval.");

            EnsureReviewingCurrentRevision(revisionId);

            _reviews.Add(new StrategyReview(
                TenantId, Id, revisionId, reviewerUserId, ReviewDecision.Approved, null));

            Status = StrategyStatus.Approved;

            _domainEvents.Add(new Events.StrategyApprovedEvent(Id, TenantId, reviewerUserId, revisionId, DateTime.UtcNow));
        }

        public void Reject(Guid reviewerUserId, Guid revisionId, string reason)
        {
            if (Status != StrategyStatus.PendingApproval)
                throw new InvalidOperationException($"Cannot reject from {Status}. Must be PendingApproval.");

            EnsureReviewingCurrentRevision(revisionId);

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A rejection reason is mandatory.", nameof(reason));

            _reviews.Add(new StrategyReview(
                TenantId, Id, revisionId, reviewerUserId, ReviewDecision.Rejected, reason));
            Status = StrategyStatus.NeedsRevision;
            _domainEvents.Add(new Events.StrategyRejectedEvent(
    Id, TenantId, reviewerUserId, revisionId, reason, DateTime.UtcNow));
        }

        private void EnsureReviewingCurrentRevision(Guid revisionId)
        {
            if (CurrentRevision is null || CurrentRevision.Id != revisionId)
                throw new InvalidOperationException(
                    "The strategy changed since it was opened. Review the latest revision.");
        }

        public void MarkImplemented(Guid implementedByUserId)
        {
            if (Status != StrategyStatus.Approved)
                throw new InvalidOperationException($"Cannot implement from {Status}. Must be Approved.");

            Status = StrategyStatus.Implemented;
            _domainEvents.Add(new Events.StrategyImplementedEvent(
    Id, TenantId, implementedByUserId, DateTime.UtcNow));
        }

    }
}
