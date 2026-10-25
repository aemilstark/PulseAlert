namespace PulseAlert.Core.Entities;

/// <summary>A single location and status report sent by a device.</summary>
public class TelemetryPing
{
    public Guid PingId { get; set; }

    public Guid DeviceId { get; set; }

    public Device? Device { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public double? AltitudeMeters { get; set; }

    public int BatteryPercentage { get; set; }

    public bool IsSosActive { get; set; }

    public bool PowerOff { get; set; }

    /// <summary>When the device's GPS hardware recorded the fix.</summary>
    public DateTime RecordedAtUtc { get; set; }

    /// <summary>When the server processed the ping.</summary>
    public DateTime ReceivedAtUtc { get; set; }
}
