using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.Identity.Infrastructure;
using CreatorAnalytics.Identity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreatorAnalytics.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.Schema)));

        services.AddScoped<ITenantAccessService, TenantAccessService>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<IMembershipQueryService, MembershipQueryService>();

        return services;
    }
}