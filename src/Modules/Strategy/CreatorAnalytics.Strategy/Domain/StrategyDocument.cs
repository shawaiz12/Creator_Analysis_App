using CreatorAnalytics.SharedKernel.Tenancy;
using System;



namespace CreatorAnalytics.Strategy.Domain
{
    public class StrategyDocument : IMustHaveTenant
    {
        public const int MaxAiRevisions = 5;
        public const int MaxRevisionLength = 50_000;
        private readonly List<object> _domainEvents = new();
        public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();
        public Guid Id { get; private set; }
        public Guid TenantId { get; private set; }
        public Guid VideoId { get; private set; }
        public StrategyStatus Status { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime LastModifiedAtUtc { get; private set; }

        public uint RowVersion { get; private set; }

        public StrategyDocument(Guid tenantId, Guid videoId)
        {
            Id = Guid.NewGuid();
            TenantId = tenantId;
            VideoId = videoId;
            Status = StrategyStatus.Draft;
            CreatedAtUtc = DateTime.UtcNow;
            LastModifiedAtUtc = CreatedAtUtc;
        }
        private StrategyDocument() { }


        private readonly List<StrategyRevision> _revisions = new();

        public IReadOnlyList<StrategyRevision> Revisions => _revisions;

        private readonly List<StrategyReview> _reviews = new();

        public IReadOnlyList<StrategyReview> Reviews => _reviews;

        private readonly List<GenerationRequest> _generationRequests = new();

        public IReadOnlyList<GenerationRequest> GenerationRequests => _generationRequests;

        public StrategyRevision? CurrentRevision =>
     _revisions.MaxBy(r => r.VersionNumber);

        public StrategyRevision AddRevision(string content, RevisionOrigin origin, Guid? authorUserId)
        {
            if (Status != StrategyStatus.Draft && Status != StrategyStatus.NeedsRevision)
                throw new InvalidOperationException(
                    $"Cannot edit from {Status}. Content is frozen outside Draft or NeedsRevision.");

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Revision content is required.", nameof(content));

            if (content.Length > MaxRevisionLength)
                throw new ArgumentException(
                    $"Revision content is longer than {MaxRevisionLength} characters.", nameof(content));

            if (origin == RevisionOrigin.Ai
                && _revisions.Count(r => r.Origin == RevisionOrigin.Ai) >= MaxAiRevisions)
            {
                throw new InvalidOperationException(
                    $"A strategy can have at most {MaxAiRevisions} AI drafts.");
            }

            var revision = new StrategyRevision(
                TenantId, Id, (CurrentRevision?.VersionNumber ?? 0) + 1, content, origin, authorUserId);

            _revisions.Add(revision);

            // Touching the strategy's own row switches on the RowVersion check, so a revision
            // cannot slip in after someone else submitted the strategy.
            var now = DateTime.UtcNow;
            LastModifiedAtUtc = now > LastModifiedAtUtc ? now : LastModifiedAtUtc.AddTicks(1);

            _domainEvents.Add(new Events.StrategyRevisionAddedEvent(
    Id, TenantId, revision.Id, revision.VersionNumber,
    origin.ToString(), authorUserId, DateTime.UtcNow));
            return revision;
        }

        public GenerationRequest RequestGeneration(Guid requestedByUserId, DateTime nowUtc)
        {
            if (Status != StrategyStatus.Draft && Status != StrategyStatus.NeedsRevision)
                throw new InvalidOperationException(
                    $"Cannot request a draft from {Status}. Must be Draft or NeedsRevision.");

            if (_generationRequests.Any(r => r.Status == GenerationStatus.Pending))
                throw new InvalidOperationException("A draft is already being generated for this strategy.");

            if (_revisions.Count(r => r.Origin == RevisionOrigin.Ai) >= MaxAiRevisions)
                throw new InvalidOperationException(
                    $"This strategy already has the maximum of {MaxAiRevisions} AI drafts.");

            var request = new GenerationRequest(TenantId, Id, requestedByUserId, nowUtc);
            _generationRequests.Add(request);

            _domainEvents.Add(new Events.StrategyGenerationRequestedEvent(
                Id, TenantId, request.Id, requestedByUserId, nowUtc));

            return request;
        }

        public StrategyRevision CompleteGeneration(
            Guid requestId, string content, string model, string promptVersion, DateTime nowUtc)
        {
            var request = FindPendingRequest(requestId);

            // Refuses if the strategy is no longer editable, the text is blank or too long,
            // or the AI draft limit is reached. In that case nothing is added.
            var revision = AddRevision(content, RevisionOrigin.Ai, null);

            request.MarkSucceeded(model, promptVersion, nowUtc);
            return revision;
        }

        public void FailGeneration(Guid requestId, string error, bool permanent, DateTime nowUtc)
        {
            FindPendingRequest(requestId).MarkAttemptFailed(error, permanent, nowUtc);
        }

        private GenerationRequest FindPendingRequest(Guid requestId)
        {
            var request = _generationRequests.SingleOrDefault(r => r.Id == requestId);

            if (request is null || request.Status != GenerationStatus.Pending)
                throw new InvalidOperationException("That generation request is not open.");

            return request;
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
