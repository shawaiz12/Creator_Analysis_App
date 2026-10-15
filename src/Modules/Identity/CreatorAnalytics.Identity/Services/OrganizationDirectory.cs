using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Identity.Services;

public sealed class OrganizationDirectory : IOrganizationDirectory
{
    private readonly IdentityDbContext _context;

    public OrganizationDirectory(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Guid>> ListOrganizationIdsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Organizations
            .AsNoTracking()
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);
    }
}