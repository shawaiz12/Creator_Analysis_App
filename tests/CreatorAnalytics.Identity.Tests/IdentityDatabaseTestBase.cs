using CreatorAnalytics.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CreatorAnalytics.Identity.Tests;

public abstract class IdentityDatabaseTestBase : IDisposable
{
    private readonly string _connectionString;

    protected IdentityDatabaseTestBase()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets("6a8817ab-906e-4a7b-acd9-f3e6c3b62f50")
            .AddEnvironmentVariables()
            .Build();

        var baseConnection = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found in user secrets.");

        // PostgreSQL limits database names to 63 characters, so keep the prefix short.
        _connectionString = new NpgsqlConnectionStringBuilder(baseConnection)
        {
            Database = $"ca_identity_{Guid.NewGuid():N}"
        }.ConnectionString;

        using var context = NewContext();
        context.Database.Migrate();
    }

    protected IdentityDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(_connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.Schema))
            .Options;

        return new IdentityDbContext(options);
    }

    public void Dispose()
    {
        // Pooled connections would keep the temporary database busy, so close them first.
        NpgsqlConnection.ClearAllPools();

        using var context = NewContext();
        context.Database.EnsureDeleted();
    }
}