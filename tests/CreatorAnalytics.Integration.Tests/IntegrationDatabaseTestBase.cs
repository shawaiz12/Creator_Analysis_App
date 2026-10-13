using CreatorAnalytics.Integration.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CreatorAnalytics.Integration.Tests;

public abstract class IntegrationDatabaseTestBase : IDisposable
{
    private readonly string _connectionString;

    protected IntegrationDatabaseTestBase()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets("6a8817ab-906e-4a7b-acd9-f3e6c3b62f50")
            .Build();

        var baseConnection = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found in user secrets.");

        _connectionString = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"CreatorAnalytics_IntegrationTest_{Guid.NewGuid():N}"
        }.ConnectionString;

        using var context = NewContext(null);
        context.Database.Migrate();
    }

    protected IntegrationDbContext NewContext(Guid? tenantId)
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseSqlServer(_connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", IntegrationDbContext.Schema))
            .Options;

        return new IntegrationDbContext(options, new FixedTenantContext(tenantId));
    }

    public void Dispose()
    {
        using var context = NewContext(null);
        context.Database.EnsureDeleted();
    }
}