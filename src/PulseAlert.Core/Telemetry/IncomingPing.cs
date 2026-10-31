namespace PulseAlert.Core.Telemetry;

/// <summary>A validated ping waiting in the queue for the Telemetry Worker.</summary>
public sealed record IncomingPing(
    string Imei,
    string ModelName,
    string FirmwareVersion,
    decimal Latitude,
    decimal Longitude,
    double? AltitudeMeters,
    int BatteryPercentage,
    bool IsSosActive,
    bool PowerOff,
    DateTime RecordedAtUtc);
