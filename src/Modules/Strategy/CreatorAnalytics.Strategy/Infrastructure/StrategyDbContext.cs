using CreatorAnalytics.SharedKernel.Tenancy;
using CreatorAnalytics.Strategy.Domain;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Strategy.Infrastructure;

public class StrategyDbContext : DbContext
{
    public const string Schema = "strategy";

    private readonly ITenantContext _tenantContext;

    public StrategyDbContext(DbContextOptions<StrategyDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Guid.Empty matches no real tenant, so "no tenant" means "no data" (fail closed).
    private Guid CurrentTenantId => _tenantContext.TenantId ?? Guid.Empty;

    public DbSet<StrategyDocument> Documents => Set<StrategyDocument>();
    public DbSet<StrategyRevision> Revisions => Set<StrategyRevision>();
    public DbSet<StrategyReview> Reviews => Set<StrategyReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StrategyDbContext).Assembly);

        modelBuilder.Entity<StrategyDocument>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<StrategyRevision>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<StrategyReview>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureNoCrossTenantWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnsureNoCrossTenantWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnsureNoCrossTenantWrites()
    {
        foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>())
        {
            var isWrite = entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;

            if (isWrite && entry.Entity.TenantId != CurrentTenantId)
            {
                throw new InvalidOperationException(
                    $"Blocked a cross-tenant write on {entry.Metadata.ClrType.Name}.");
            }
        }
    }
}