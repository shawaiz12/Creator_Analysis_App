using CreatorAnalytics.Identity.Contracts.Services;

namespace CreatorAnalytics.Api.Tests;

public sealed class FakeTenantAccessService : ITenantAccessService
{
    private readonly Dictionary<(Guid TenantId, string ExternalUserId), TenantAccess> _members = new();

    public void AddMember(Guid tenantId, string externalUserId, Guid userId, string role) =>
        _members[(tenantId, externalUserId)] = new TenantAccess(userId, role);

    public Task<TenantAccess?> GetAccessAsync(Guid tenantId, string externalUserId) =>
        Task.FromResult(_members.TryGetValue((tenantId, externalUserId), out var access) ? access : null);
}