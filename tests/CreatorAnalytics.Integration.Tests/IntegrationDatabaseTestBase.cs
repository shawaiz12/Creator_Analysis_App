using CreatorAnalytics.Integration.Infrastructure;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.Integration.Services;
using CreatorAnalytics.Integration.YouTube;
using CreatorAnalytics.SharedKernel.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace CreatorAnalytics.Integration.Tests;

public abstract class IntegrationDatabaseTestBase : IDisposable
{
    private readonly string _connectionString;

    protected IntegrationDatabaseTestBase()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets("6a8817ab-906e-4a7b-acd9-f3e6c3b62f50")
            .AddEnvironmentVariables()
            .Build();

        var baseConnection = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found in user secrets.");

        var builder = new NpgsqlConnectionStringBuilder(baseConnection)
        {
            Database = $"creatoranalytics_integrationtest_{Guid.NewGuid():N}"
        };

        _connectionString = builder.ConnectionString;

        using var context = NewContext(null);
        context.Database.Migrate();
    }

    protected IntegrationDbContext NewContext(Guid? tenantId)
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseNpgsql(_connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", IntegrationDbContext.Schema))
            .Options;

        return new IntegrationDbContext(options, new FixedTenantContext(tenantId));
    }

    protected ServiceProvider BuildProvider(IEnumerable<Guid> tenantIds, IYouTubeClient client)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddDbContext<IntegrationDbContext>(options =>
            options.UseNpgsql(_connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", IntegrationDbContext.Schema)));
        services.AddSingleton(client);
        services.AddScoped<ChannelSyncService>();
        services.AddSingleton<IOrganizationDirectory>(new FixedOrganizationDirectory(tenantIds));
        services.AddSingleton<ChannelSyncDispatcher>();

        return services.BuildServiceProvider();
    }

    public void Dispose()
    {
        using var context = NewContext(null);
        context.Database.EnsureDeleted();
    }
}