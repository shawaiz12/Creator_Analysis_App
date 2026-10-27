using CreatorAnalytics.Integration.Contracts.Services;
using CreatorAnalytics.Integration.Infrastructure;
using CreatorAnalytics.Integration.Services;
using CreatorAnalytics.Integration.YouTube;
using CreatorAnalytics.SharedKernel.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


namespace CreatorAnalytics.Integration;

public static class IntegrationModule
{
    public static IServiceCollection AddIntegrationModule(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IntegrationDbContext>(options =>
            options.UseNpgsql(connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", IntegrationDbContext.Schema)));

        // The fake stands in for YouTube until the real Google client exists.
        services.AddSingleton<IYouTubeClient, FakeYouTubeClient>();
        services.AddScoped<ChannelSyncService>();

        services.AddScoped<IVideoAnalyticsQuery, VideoAnalyticsQuery>();
        services.AddSingleton<ChannelSyncDispatcher>();
        services.AddHostedService<ChannelSyncBackgroundService>();


        services.AddScoped<IDatabaseMigrator, IntegrationDatabaseMigrator>();

        return services;
    }
}