using CreatorAnalytics.SharedKernel.Tenancy;

namespace CreatorAnalytics.Strategy.Domain;

public enum GenerationStatus
{
    Pending,
    Succeeded,
    Failed
}

public class GenerationRequest : IMustHaveTenant
{
    public const int MaxAttempts = 3;

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StrategyDocumentId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public GenerationStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTime NextAttemptAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? Error { get; private set; }
    public string? Model { get; private set; }
    public string? PromptVersion { get; private set; }

    private GenerationRequest()
    {
    }

    internal GenerationRequest(
        Guid tenantId, Guid strategyDocumentId, Guid requestedByUserId, DateTime nowUtc)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (strategyDocumentId == Guid.Empty) throw new ArgumentException("StrategyDocumentId is required.", nameof(strategyDocumentId));
        if (requestedByUserId == Guid.Empty) throw new ArgumentException("RequestedByUserId is required.", nameof(requestedByUserId));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        StrategyDocumentId = strategyDocumentId;
        RequestedByUserId = requestedByUserId;
        RequestedAtUtc = nowUtc;
        Status = GenerationStatus.Pending;
        Attempts = 0;
        NextAttemptAtUtc = nowUtc;
    }

    internal void MarkSucceeded(string model, string promptVersion, DateTime nowUtc)
    {
        Status = GenerationStatus.Succeeded;
        Model = model;
        PromptVersion = promptVersion;
        CompletedAtUtc = nowUtc;
        Error = null;
    }

    internal void MarkAttemptFailed(string error, bool permanent, DateTime nowUtc)
    {
        Attempts++;
        Error = error.Length > 1000 ? error[..1000] : error;

        if (permanent || Attempts >= MaxAttempts)
        {
            Status = GenerationStatus.Failed;
            CompletedAtUtc = nowUtc;
        }
        else
        {
            // Back off: 30 seconds after the first failure, 60 after the second.
            NextAttemptAtUtc = nowUtc.AddSeconds(30 * Attempts);
        }
    }
}