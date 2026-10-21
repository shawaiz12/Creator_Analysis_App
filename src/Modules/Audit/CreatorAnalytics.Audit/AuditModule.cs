using CreatorAnalytics.Audit.Contracts.Services;
using CreatorAnalytics.Audit.Infrastructure;
using CreatorAnalytics.Audit.Services;
using CreatorAnalytics.SharedKernel.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreatorAnalytics.Audit;

public static class AuditModule
{
    public static IServiceCollection AddAuditModule(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AuditDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", AuditDbContext.Schema)));

        services.AddScoped<IAuditQueryService, AuditQueryService>();

        services.AddScoped<IAuditWriter, AuditWriter>();

        services.AddScoped<IDatabaseMigrator, AuditDatabaseMigrator>();

        return services;
    }
}