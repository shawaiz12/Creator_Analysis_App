using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.Integration.Domain;
using CreatorAnalytics.Integration.Infrastructure;
using CreatorAnalytics.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CreatorAnalytics.Integration.Services;

public sealed class ChannelSyncDispatcher
{
    public static readonly TimeSpan SyncInterval = TimeSpan.FromHours(6);

    private readonly IServiceProvider _rootServices;
    private readonly ILogger<ChannelSyncDispatcher> _logger;

    public ChannelSyncDispatcher(IServiceProvider rootServices, ILogger<ChannelSyncDispatcher> logger)
    {
        _rootServices = rootServices;
        _logger = logger;
    }

    /// <summary>One pass over every organization. Returns how many channels were synced successfully.</summary>
    public async Task<int> RunOnceAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> tenantIds;

        using (var scope = _rootServices.CreateScope())
        {
            var directory = scope.ServiceProvider.GetRequiredService<IOrganizationDirectory>();
            tenantIds = await directory.ListOrganizationIdsAsync(cancellationToken);
        }

        var synced = 0;

        foreach (var tenantId in tenantIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            synced += await SyncTenantAsync(tenantId, nowUtc, cancellationToken);
        }

        return synced;
    }

    private async Task<int> SyncTenantAsync(Guid tenantId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        List<Guid> dueChannelIds;

        using (var scope = _rootServices.CreateScope())
        {
            // Work is always done for one explicitly chosen tenant at a time.
            scope.ServiceProvider.GetRequiredService<TenantContext>().Set(tenantId);
            var context = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

            var cutoff = nowUtc - SyncInterval;

            dueChannelIds = await context.ChannelConnections
                .Where(c => c.Status == ConnectionStatus.Connected
                            && (c.LastSyncAttemptAtUtc == null || c.LastSyncAttemptAtUtc <= cutoff))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
        }

        var synced = 0;

        foreach (var channelId in dueChannelIds)
        {
            // A fresh scope (and tenant context) for every channel.
            using var scope = _rootServices.CreateScope();
            scope.ServiceProvider.GetRequiredService<TenantContext>().Set(tenantId);
            var sync = scope.ServiceProvider.GetRequiredService<ChannelSyncService>();

            try
            {
                var result = await sync.SyncAsync(channelId, nowUtc, cancellationToken);

                if (result.Outcome == SyncOutcome.Succeeded)
                    synced++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync failed for channel {ChannelId} in tenant {TenantId}", channelId, tenantId);
            }
        }

        return synced;
    }
}