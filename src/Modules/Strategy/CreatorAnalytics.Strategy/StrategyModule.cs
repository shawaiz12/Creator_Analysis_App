using CreatorAnalytics.Strategy.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CreatorAnalytics.Strategy.Contracts.Services;
using CreatorAnalytics.Strategy.Insights;

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

        return services;
    }
}