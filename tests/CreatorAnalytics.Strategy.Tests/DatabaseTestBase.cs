using CreatorAnalytics.Strategy.Infrastructure;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.Integration.Contracts.Services;
using CreatorAnalytics.SharedKernel.Tenancy;
using CreatorAnalytics.Strategy.Insights;
using Microsoft.Extensions.DependencyInjection;


namespace CreatorAnalytics.Strategy.Tests
{
    public abstract class DatabaseTestBase : IDisposable
    {
        private readonly string _connectionString;

        protected DatabaseTestBase()
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets("6a8817ab-906e-4a7b-acd9-f3e6c3b62f50")
                .AddEnvironmentVariables()
                .Build();

            var baseConnection = config.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string default not found in user secrets.");
            var builder = new NpgsqlConnectionStringBuilder(baseConnection)
            {
                Database = $"ca_strategy_{Guid.NewGuid():N}"
            };
            _connectionString = builder.ConnectionString;

            using var context = NewContext();
            context.Database.Migrate();

        }

        protected StrategyDbContext NewContext(Guid? tenantId = null)
        {
            var options = new DbContextOptionsBuilder<StrategyDbContext>()
                .UseNpgsql(_connectionString, sql =>
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", StrategyDbContext.Schema))
                .Options;

            return new StrategyDbContext(options, new FixedTenantContext(tenantId));
        }

        protected ServiceProvider BuildProvider(
    IEnumerable<Guid> tenantIds, IVideoAnalyticsQuery videos, IInsightGenerator generator)
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddScoped<TenantContext>();
            services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
            services.AddDbContext<StrategyDbContext>(options =>
                options.UseNpgsql(_connectionString, sql =>
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", StrategyDbContext.Schema)));
            services.AddSingleton<IVideoAnalyticsQuery>(videos);
            services.AddSingleton<IInsightGenerator>(generator);
            services.AddScoped<InsightInputBuilder>();
            services.AddSingleton<IOrganizationDirectory>(new FixedOrganizationDirectory(tenantIds));
            services.AddSingleton<GenerationDispatcher>();

            return services.BuildServiceProvider();
        }


        public void Dispose()
        {
            NpgsqlConnection.ClearAllPools();
            using var context = NewContext();
            context.Database.EnsureDeleted();
        }
    }
}
