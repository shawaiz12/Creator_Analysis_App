using CreatorAnalytics.Strategy.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CreatorAnalytics.Strategy.Contracts.Services;

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

        return services;
    }
}