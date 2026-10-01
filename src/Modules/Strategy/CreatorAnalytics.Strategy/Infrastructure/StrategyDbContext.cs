using CreatorAnalytics.Strategy.Domain;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Strategy.Infrastructure;

public class StrategyDbContext : DbContext
{
    public const string Schema = "strategy";

    public StrategyDbContext(DbContextOptions<StrategyDbContext> options)
        : base(options)
    {
    }

    public DbSet<StrategyDocument> Documents => Set<StrategyDocument>();
    public DbSet<StrategyRevision> Revisions => Set<StrategyRevision>();
    public DbSet<StrategyReview> Reviews => Set<StrategyReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
    }
}