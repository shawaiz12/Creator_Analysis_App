using CreatorAnalytics.Audit.Contracts.Services;
using CreatorAnalytics.Audit.Domain;
using CreatorAnalytics.Audit.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Audit.Services;

internal sealed class AuditWriter : IAuditWriter
{
    private readonly AuditDbContext _context;

    public AuditWriter(AuditDbContext context)
    {
        _context = context;
    }

    public async Task<bool> WriteAsync(
        Guid tenantId, Guid sourceMessageId, string eventType, string eventData,
        CancellationToken cancellationToken = default)
    {
        var alreadyRecorded = await _context.AuditLogs
            .AnyAsync(a => a.ProcessedMessageId == sourceMessageId, cancellationToken);

        if (alreadyRecorded)
            return false;

        _context.AuditLogs.Add(new AuditLog(tenantId, sourceMessageId, eventType, eventData));
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}