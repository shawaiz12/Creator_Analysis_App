using CreatorAnalytics.Strategy.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreatorAnalytics.Strategy;

public static class StrategyModule
{
    public static IServiceCollection AddStrategyModule(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<StrategyDbContext>(options =>
     options.UseSqlServer(connectionString, sql =>
         sql.MigrationsHistoryTable("__EFMigrationsHistory", StrategyDbContext.Schema)));

        return services;
    }
}