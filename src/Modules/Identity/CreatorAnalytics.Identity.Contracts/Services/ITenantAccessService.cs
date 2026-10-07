using System;
using System.Threading.Tasks;

namespace CreatorAnalytics.Identity.Contracts.Services;

public interface ITenantAccessService
{
    Task<string?> GetUserRoleAsync(Guid tenantId, string externalUserId);
}