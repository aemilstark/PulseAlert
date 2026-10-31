using System.ComponentModel.DataAnnotations;

namespace PulseAlert.Core.Telemetry;

/// <summary>Settings for telemetry ingestion, bound from the "Telemetry" configuration section.</summary>
public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    /// <summary>The shared secret devices send in the X-Api-Key header.</summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>How many pings the queue holds before the webhook returns 503.</summary>
    [Range(1, int.MaxValue)]
    public int QueueCapacity { get; set; } = 10_000;

    /// <summary>The most pings the Telemetry Worker saves in one batch.</summary>
    [Range(1, 10_000)]
    public int MaxBatchSize { get; set; } = 500;
}
