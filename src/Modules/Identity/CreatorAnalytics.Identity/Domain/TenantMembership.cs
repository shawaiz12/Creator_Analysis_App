using System;

namespace CreatorAnalytics.Identity.Domain;

public class TenantMembership
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Role Role { get; private set; }

    public TenantMembership(Guid tenantId, Guid userId, Role role)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        UserId = userId;
        Role = role;
    }
}