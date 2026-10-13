using CreatorAnalytics.Integration.Domain;
using CreatorAnalytics.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Integration.Infrastructure;

public class IntegrationDbContext : DbContext
{
    public const string Schema = "integration";

    private readonly ITenantContext _tenantContext;

    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Guid.Empty matches no real tenant, so "no tenant" means "no data" (fail closed).
    private Guid CurrentTenantId => _tenantContext.TenantId ?? Guid.Empty;

    public DbSet<ChannelConnection> ChannelConnections => Set<ChannelConnection>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<RetentionSnapshot> RetentionSnapshots => Set<RetentionSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IntegrationDbContext).Assembly);

        modelBuilder.Entity<ChannelConnection>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<Video>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
        modelBuilder.Entity<RetentionSnapshot>().HasQueryFilter(x => x.TenantId == CurrentTenantId);
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
                throw new TenantViolationException(
                    $"Blocked a cross-tenant write on {entry.Metadata.ClrType.Name}.");
            }
        }
    }
}