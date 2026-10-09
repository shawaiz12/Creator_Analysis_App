using CreatorAnalytics.Identity.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CreatorAnalytics.Identity.Tests;

public abstract class IdentityDatabaseTestBase : IDisposable
{
    private readonly string _connectionString;

    protected IdentityDatabaseTestBase()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets("6a8817ab-906e-4a7b-acd9-f3e6c3b62f50")
            .Build();

        var baseConnection = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found in user secrets.");

        _connectionString = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"CreatorAnalytics_IdentityTest_{Guid.NewGuid():N}"
        }.ConnectionString;

        using var context = NewContext();
        context.Database.Migrate();
    }

    protected IdentityDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer(_connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.Schema))
            .Options;

        return new IdentityDbContext(options);
    }

    public void Dispose()
    {
        using var context = NewContext();
        context.Database.EnsureDeleted();
    }
}