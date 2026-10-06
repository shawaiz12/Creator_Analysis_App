namespace CreatorAnalytics.SharedKernel.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public void Set(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant id cannot be empty.", nameof(tenantId));

        if (TenantId is not null && TenantId != tenantId)
            throw new InvalidOperationException("The tenant for this request is already set.");

        TenantId = tenantId;
    }
}