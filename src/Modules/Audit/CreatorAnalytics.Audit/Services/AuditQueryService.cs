using CreatorAnalytics.Audit.Contracts.Services;
using CreatorAnalytics.Audit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Audit.Services;

public sealed class AuditQueryService : IAuditQueryService
{
    private readonly AuditDbContext _context;

    public AuditQueryService(AuditDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AuditEntry>> ListAsync(
        Guid tenantId, int take, int skip, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        skip = Math.Max(skip, 0);

        var rows = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.RecordedAtUtc)
            .ThenByDescending(a => a.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return rows
            .Select(a => new AuditEntry(a.Id, a.EventType, a.RecordedAtUtc, a.EventData))
            .ToList();
    }
}