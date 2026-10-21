using CreatorAnalytics.SharedKernel.Data;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Identity.Infrastructure;

internal sealed class IdentityDatabaseMigrator : IDatabaseMigrator
{
    private readonly IdentityDbContext _context;

    public IdentityDatabaseMigrator(IdentityDbContext context) => _context = context;

    public string Name => "identity";

    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        _context.Database.MigrateAsync(cancellationToken);
}