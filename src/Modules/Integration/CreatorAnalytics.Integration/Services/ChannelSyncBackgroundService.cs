using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CreatorAnalytics.Integration.Services;

public sealed class ChannelSyncBackgroundService : BackgroundService
{
    private readonly ChannelSyncDispatcher _dispatcher;
    private readonly ILogger<ChannelSyncBackgroundService> _logger;

    public ChannelSyncBackgroundService(
        ChannelSyncDispatcher dispatcher, ILogger<ChannelSyncBackgroundService> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Give the application a moment to finish starting.
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _dispatcher.RunOnceAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled channel sync failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}