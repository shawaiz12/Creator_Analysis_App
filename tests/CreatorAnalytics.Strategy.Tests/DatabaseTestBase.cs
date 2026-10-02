using CreatorAnalytics.Strategy.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


namespace CreatorAnalytics.Strategy.Tests
{
    public abstract class DatabaseTestBase : IDisposable
    {
        private readonly string _connectionString;

        protected DatabaseTestBase()
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets("6a8817ab-906e-4a7b-acd9-f3e6c3b62f50")
                .Build();

            var baseConnection = config.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string default not found in user secrets.");
            var builder = new SqlConnectionStringBuilder(baseConnection)
            {
                InitialCatalog = $"CreatorAnalytics_Test_{Guid.NewGuid():N}"
            };
            _connectionString = builder.ConnectionString;

            using var context = NewContext();
            context.Database.Migrate();

        }

        protected StrategyDbContext NewContext()
        {
            var options = new DbContextOptionsBuilder<StrategyDbContext>()
                .UseSqlServer(_connectionString, sql =>
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", StrategyDbContext.Schema))
                .Options;

            return new StrategyDbContext(options);
        }


        public void Dispose()
        {
            using var context = NewContext();
            context.Database.EnsureDeleted();
        }
    }
}
