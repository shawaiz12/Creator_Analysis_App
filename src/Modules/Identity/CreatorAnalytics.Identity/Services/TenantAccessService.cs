using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Identity.Services;

internal sealed class TenantAccessService : ITenantAccessService
{
    private readonly IdentityDbContext _context;

    public TenantAccessService(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetUserRoleAsync(Guid tenantId, string externalUserId)
    {
        var membership = await _context.TenantMemberships
            .AsNoTracking()
            .Join(_context.Users,
                m => m.UserId,
                u => u.Id,
                (m, u) => new { m.TenantId, u.ExternalId, m.Role })
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.ExternalId == externalUserId);

        return membership?.Role.ToString();
    }
}