using System;
using CreatorAnalytics.SharedKernel.Tenancy;

namespace CreatorAnalytics.Audit.Domain;

public class AuditLog : IMustHaveTenant
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    // Crucial for Idempotency: We track the Outbox Message ID so we can safely ignore it if it gets delivered twice
    public Guid ProcessedMessageId { get; private set; }

    public string EventType { get; private set; }
    public string EventData { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }

    public AuditLog(Guid tenantId, Guid processedMessageId, string eventType, string eventData)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (processedMessageId == Guid.Empty) throw new ArgumentException("ProcessedMessageId is required.", nameof(processedMessageId));
        if (string.IsNullOrWhiteSpace(eventType)) throw new ArgumentException("EventType is required.", nameof(eventType));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        ProcessedMessageId = processedMessageId;
        EventType = eventType;
        EventData = eventData;
        RecordedAtUtc = DateTime.UtcNow;
    }
}