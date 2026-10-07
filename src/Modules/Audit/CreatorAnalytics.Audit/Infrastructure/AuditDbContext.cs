using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CreatorAnalytics.Audit.Domain;
using CreatorAnalytics.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Audit.Infrastructure;

public class AuditDbContext : DbContext
{
    public const string Schema = "audit";
    private readonly ITenantContext _tenantContext;

    public AuditDbContext(DbContextOptions<AuditDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.EventType).IsRequired().HasMaxLength(100);
            b.Property(x => x.EventData).IsRequired();

            // Idempotency constraint: Prevent inserting the same outbox message twice for a tenant
            b.HasIndex(x => new { x.TenantId, x.ProcessedMessageId }).IsUnique();
        });

        // Global Query Filter for strict tenant isolation
        modelBuilder.Entity<AuditLog>().HasQueryFilter(x => x.TenantId == _tenantContext.TenantId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureNoCrossTenantWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnsureNoCrossTenantWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnsureNoCrossTenantWrites()
    {
        var tenantId = _tenantContext.TenantId;
        var entries = ChangeTracker.Entries<IMustHaveTenant>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity.TenantId != tenantId)
            {
                throw new InvalidOperationException(
                    $"Audit write violation: Entity belongs to tenant {entry.Entity.TenantId}, but request context is {tenantId}");
            }
        }
    }
}