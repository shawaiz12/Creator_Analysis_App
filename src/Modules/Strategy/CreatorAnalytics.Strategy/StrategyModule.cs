using CreatorAnalytics.Strategy.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CreatorAnalytics.Strategy.Contracts.Services;
using CreatorAnalytics.Strategy.Insights;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CreatorAnalytics.Strategy;

public static class StrategyModule
{
    public static IServiceCollection AddStrategyModule(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<StrategyDbContext>(options =>
     options.UseSqlServer(connectionString, sql =>
         sql.MigrationsHistoryTable("__EFMigrationsHistory", StrategyDbContext.Schema)));


        services.AddScoped<IStrategyOutbox, StrategyOutboxService>();
        // The template writer stands in until the real AI generator exists.
        services.AddSingleton<IInsightGenerator, TemplateInsightGenerator>();
        services.AddScoped<InsightInputBuilder>();
        services.AddSingleton<GenerationDispatcher>();
        services.AddHostedService<GenerationBackgroundService>();

        return services;
    }

    /// <summary>Switches draft writing from the template writer to a model reached through IChatClient.</summary>
    public static IServiceCollection UseChatInsightGenerator(this IServiceCollection services)
    {
        services.RemoveAll<IInsightGenerator>();
        services.AddSingleton<IInsightGenerator, ChatInsightGenerator>();

        return services;
    }
}