using Microsoft.Extensions.Options;
using PulseAlert.Core.Telemetry;

namespace PulseAlert.Api.Workers;

/// <summary>The single consumer of the telemetry queue. Saves pings in batches, each with a fresh DbContext.</summary>
public sealed class TelemetryWorker(
    ITelemetryQueue queue,
    IServiceScopeFactory scopeFactory,
    IOptions<TelemetryOptions> options,
    ILogger<TelemetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IReadOnlyList<IncomingPing> batch;
            try
            {
                batch = await queue.ReadBatchAsync(options.Value.MaxBatchSize, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessBatchAsync(batch, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(IReadOnlyList<IncomingPing> batch, CancellationToken stoppingToken)
    {
        try
        {
            // A scope per batch gives each batch its own DbContext.
            await using var scope = scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<ITelemetryBatchProcessor>();
            await processor.ProcessAsync(batch, stoppingToken);

            logger.LogDebug("Saved a batch of {PingCount} pings.", batch.Count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning("Shutdown interrupted a batch of {PingCount} pings; they were not saved.", batch.Count);
        }
        catch (Exception ex)
        {
            // An unhandled exception would stop the whole host, so a failed batch is logged and dropped.
            logger.LogError(ex, "Failed to save a batch of {PingCount} pings; the batch was dropped.", batch.Count);
        }
    }
}
