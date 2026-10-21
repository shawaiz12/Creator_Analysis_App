using CreatorAnalytics.SharedKernel.Data;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Strategy.Infrastructure;

internal sealed class StrategyDatabaseMigrator : IDatabaseMigrator
{
    private readonly StrategyDbContext _context;

    public StrategyDatabaseMigrator(StrategyDbContext context) => _context = context;

    public string Name => "strategy";

    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        _context.Database.MigrateAsync(cancellationToken);
}