namespace CreatorAnalytics.SharedKernel.Data;

/// <summary>Brings one module's part of the database up to date.</summary>
public interface IDatabaseMigrator
{
    string Name { get; }

    Task MigrateAsync(CancellationToken cancellationToken = default);
}