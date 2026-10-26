using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Identity.Services;

public sealed class MembershipQueryService : IMembershipQueryService
{
    private readonly IdentityDbContext _context;

    public MembershipQueryService(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MembershipInfo>> ListForUserAsync(
        string externalUserId, CancellationToken cancellationToken = default)
    {
        var rows = await _context.TenantMemberships
            .AsNoTracking()
            .Join(_context.Users, m => m.UserId, u => u.Id,
                (m, u) => new { m.TenantId, m.Role, u.ExternalId })
            .Where(x => x.ExternalId == externalUserId)
            .Join(_context.Organizations, x => x.TenantId, o => o.Id,
                (x, o) => new { OrganizationId = o.Id, o.Name, x.Role })
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new MembershipInfo(r.OrganizationId, r.Name, r.Role.ToString()))
            .ToList();
    }

    public async Task<IReadOnlyList<OrganizationMember>> ListMembersAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        // TenantMembership is exempt from the automatic tenant filter, so this Where is what
        // keeps organizations apart. Do not remove it.
        var rows = await _context.TenantMemberships
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .Join(_context.Users, m => m.UserId, u => u.Id,
                (m, u) => new { UserId = u.Id, u.Email, m.Role })
            .OrderBy(x => x.Email)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new OrganizationMember(r.UserId, r.Email, r.Role.ToString()))
            .ToList();
    }
}