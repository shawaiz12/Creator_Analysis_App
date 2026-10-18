using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CreatorAnalytics.Strategy.Insights;

public sealed class GenerationBackgroundService : BackgroundService
{
    private readonly GenerationDispatcher _dispatcher;
    private readonly ILogger<GenerationBackgroundService> _logger;

    public GenerationBackgroundService(
        GenerationDispatcher dispatcher, ILogger<GenerationBackgroundService> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
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
                _logger.LogError(ex, "The draft generation pass failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}