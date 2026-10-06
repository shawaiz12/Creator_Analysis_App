namespace CreatorAnalytics.SharedKernel.Tenancy;

public interface IMustHaveTenant
{
    Guid TenantId { get; }
}