using CreatorAnalytics.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CreatorAnalytics.Integration.Services;
using CreatorAnalytics.Integration.YouTube;

namespace CreatorAnalytics.Integration;

public static class IntegrationModule
{
    public static IServiceCollection AddIntegrationModule(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IntegrationDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", IntegrationDbContext.Schema)));

        // The fake stands in for YouTube until the real Google client exists.
        services.AddSingleton<IYouTubeClient, FakeYouTubeClient>();
        services.AddScoped<ChannelSyncService>();

        return services;
    }
}