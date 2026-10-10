using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CreatorAnalytics.Api.BackgroundServices;

public class OutboxProcessorBackgroundService : BackgroundService
{
    private readonly OutboxDispatcher _dispatcher;
    private readonly ILogger<OutboxProcessorBackgroundService> _logger;

    public OutboxProcessorBackgroundService(
        OutboxDispatcher dispatcher, ILogger<OutboxProcessorBackgroundService> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _dispatcher.ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred processing outbox messages.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}