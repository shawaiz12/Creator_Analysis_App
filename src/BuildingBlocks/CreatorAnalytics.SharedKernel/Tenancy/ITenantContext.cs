namespace CreatorAnalytics.SharedKernel.Tenancy;

public interface ITenantContext
{
    Guid? TenantId { get; }
}