using System;
using System.IO.Pipes;


namespace CreatorAnalytics.Strategy.Domain
{
    public class StrategyDocument
    {
        public Guid Id { get; private set; }
        public Guid TenantId { get; private set; }
        public Guid VideoId { get; private set; }
        public StrategyStatus Status { get; private set; }

        public StrategyDocument(Guid tenantId, Guid videoId)
        {
            Id = Guid.NewGuid();
            TenantId = tenantId;
            VideoId = videoId;
            Status = StrategyStatus.Draft;
        }

        public void SubmitForApproval()
        {
            if (Status != StrategyStatus.Draft && Status != StrategyStatus.NeedsRevision)
                throw new InvalidOperationException($"Cannot submit from{Status}. Must be Draft or NeedsRevision");
            Status = StrategyStatus.PendingApproval;
        }

        public void Approve()
        {
            if (Status != StrategyStatus.PendingApproval)
                throw new InvalidOperationException($"Cannot approve from {Status}. Must be PendingApproval.");

            Status = StrategyStatus.Approved;
        }

        public void Reject(string reason)
        {
            if (Status != StrategyStatus.PendingApproval)
                throw new InvalidOperationException($"Cannot reject from {Status}. Must be PendingApproval.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A rejection reason is mandatory.", nameof(reason));

            Status = StrategyStatus.NeedsRevision;
        }

        public void MarkImplemented()
        {
            if (Status != StrategyStatus.Approved)
                throw new InvalidOperationException($"Cannot implement from {Status}. Must be Approved.");

            Status = StrategyStatus.Implemented;
        }

    }
}
