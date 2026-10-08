namespace CreatorAnalytics.Identity.Contracts.Services;

public sealed record TenantAccess(Guid UserId, string Role);

public interface ITenantAccessService
{
    Task<TenantAccess?> GetAccessAsync(Guid tenantId, string externalUserId);
}