using CreatorAnalytics.SharedKernel.Tenancy;
using CreatorAnalytics.Strategy.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

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
        ProcessDomainEvents();
        EnsureNoCrossTenantWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ProcessDomainEvents();
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

    private void ProcessDomainEvents()
    {
        var entitiesWithEvents = ChangeTracker.Entries<StrategyDocument>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                var outboxMessage = new OutboxMessage(
                    domainEvent.GetType().Name,
                    JsonSerializer.Serialize(domainEvent));

                OutboxMessages.Add(outboxMessage);
            }
            entity.ClearDomainEvents();
        }
    }
}