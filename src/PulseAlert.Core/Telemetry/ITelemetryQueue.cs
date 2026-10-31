namespace PulseAlert.Core.Telemetry;

/// <summary>The bounded in-memory queue between the webhook and the Telemetry Worker.</summary>
public interface ITelemetryQueue
{
    /// <summary>Adds a ping without waiting. Returns false when the queue is full.</summary>
    bool TryEnqueue(IncomingPing ping);
}
