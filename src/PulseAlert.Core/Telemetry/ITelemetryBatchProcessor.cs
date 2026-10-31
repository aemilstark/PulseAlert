namespace PulseAlert.Core.Telemetry;

/// <summary>Saves one batch of queued pings to the database.</summary>
public interface ITelemetryBatchProcessor
{
    Task ProcessAsync(IReadOnlyList<IncomingPing> batch, CancellationToken cancellationToken);
}
