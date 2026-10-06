using CreatorAnalytics.SharedKernel.Tenancy;

namespace CreatorAnalytics.Strategy.Tests;

public sealed class FixedTenantContext : ITenantContext
{
    public FixedTenantContext(Guid? tenantId) => TenantId = tenantId;

    public Guid? TenantId { get; }
}