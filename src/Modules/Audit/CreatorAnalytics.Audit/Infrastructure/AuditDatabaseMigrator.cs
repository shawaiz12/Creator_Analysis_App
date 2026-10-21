using CreatorAnalytics.SharedKernel.Data;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Audit.Infrastructure;

internal sealed class AuditDatabaseMigrator : IDatabaseMigrator
{
    private readonly AuditDbContext _context;

    public AuditDatabaseMigrator(AuditDbContext context) => _context = context;

    public string Name => "audit";

    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        _context.Database.MigrateAsync(cancellationToken);
}