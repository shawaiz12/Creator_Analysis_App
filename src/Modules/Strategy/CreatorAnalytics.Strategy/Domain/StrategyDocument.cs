using System;
using System.IO.Pipes;


namespace CreatorAnalytics.Strategy.Domain
{
    public class StrategyDocument
    {
        public Guid Id { get; private set; }
        public Guid TenantId { get; private set; }
        public string VideoID { get; private set; }
        public StrategyStatus Status { get; private set; }

        public StrategyDocument(Guid tenantId, string videoId)
        {
            Id = Guid.NewGuid();
            TenantId = tenantId;
            VideoID = videoId;
            Status = StrategyStatus.Draft;
        }

        public void SubmitForApproval()
        {
            if (Status != StrategyStatus.Draft && Status != StrategyStatus.NeedsRevision)
                throw new InvalidOperationException($"Cannot submit from{Status}. Must be Draft or NeedsRevision");
            Status = StrategyStatus.PendingApproval;
        }

    }
}
