using System.ComponentModel.DataAnnotations;
using PulseAlert.Core.Telemetry;

namespace PulseAlert.Api.Contracts;

/// <summary>The body of POST /api/telemetry.</summary>
/// <remarks>Value types are nullable so that a missing field fails [Required] instead of defaulting to zero or false.</remarks>
public sealed record TelemetryPingRequest
{
    [Required]
    [RegularExpression(@"^\d{1,15}$", ErrorMessage = "The IMEI must be 1 to 15 digits.")]
    public string? Imei { get; init; }

    [Required]
    [StringLength(100)]
    public string? ModelName { get; init; }

    [Required]
    [StringLength(50)]
    public string? FirmwareVersion { get; init; }

    [Required]
    [Range(-90.0, 90.0)]
    public decimal? Latitude { get; init; }

    [Required]
    [Range(-180.0, 180.0)]
    public decimal? Longitude { get; init; }

    public double? AltitudeMeters { get; init; }

    [Required]
    [Range(0, 100)]
    public int? BatteryPercentage { get; init; }

    [Required]
    public bool? IsSosActive { get; init; }

    [Required]
    public bool? PowerOff { get; init; }

    [Required]
    public DateTimeOffset? RecordedAtUtc { get; init; }

    /// <summary>Converts a request that has passed validation.</summary>
    public IncomingPing ToIncomingPing() => new(
        Imei!,
        ModelName!,
        FirmwareVersion!,
        Latitude!.Value,
        Longitude!.Value,
        AltitudeMeters,
        BatteryPercentage!.Value,
        IsSosActive!.Value,
        PowerOff!.Value,
        RecordedAtUtc!.Value.UtcDateTime);
}
