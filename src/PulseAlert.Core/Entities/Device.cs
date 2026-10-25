namespace PulseAlert.Core.Entities;

/// <summary>A GPS tracking unit, identified externally by its IMEI.</summary>
public class Device
{
    public Guid DeviceId { get; set; }

    public required string Imei { get; set; }

    public required string ModelName { get; set; }

    public required string FirmwareVersion { get; set; }

    public DeviceStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<TelemetryPing> TelemetryPings { get; set; } = [];

    public List<EmergencyIncident> EmergencyIncidents { get; set; } = [];
}
