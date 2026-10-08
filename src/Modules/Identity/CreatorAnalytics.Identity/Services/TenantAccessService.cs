using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Identity.Services;

public sealed class TenantAccessService : ITenantAccessService
{
    private readonly IdentityDbContext _context;

    public TenantAccessService(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<TenantAccess?> GetAccessAsync(Guid tenantId, string externalUserId)
    {
        var membership = await _context.TenantMemberships
            .AsNoTracking()
            .Join(_context.Users,
                m => m.UserId,
                u => u.Id,
                (m, u) => new { m.TenantId, u.ExternalId, m.UserId, m.Role })
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.ExternalId == externalUserId);

        return membership is null
            ? null
            : new TenantAccess(membership.UserId, membership.Role.ToString());
    }
}