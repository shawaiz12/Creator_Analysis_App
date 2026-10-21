using CreatorAnalytics.SharedKernel.Data;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Integration.Infrastructure;

internal sealed class IntegrationDatabaseMigrator : IDatabaseMigrator
{
    private readonly IntegrationDbContext _context;

    public IntegrationDatabaseMigrator(IntegrationDbContext context) => _context = context;

    public string Name => "integration";

    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        _context.Database.MigrateAsync(cancellationToken);
}