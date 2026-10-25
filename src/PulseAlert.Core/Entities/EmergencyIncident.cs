namespace PulseAlert.Core.Entities;

/// <summary>An SOS emergency, open until an operator resolves it.</summary>
public class EmergencyIncident
{
    public Guid IncidentId { get; set; }

    public Guid DeviceId { get; set; }

    public Device? Device { get; set; }

    public Guid TriggerPingId { get; set; }

    public TelemetryPing? TriggerPing { get; set; }

    public IncidentStatus Status { get; set; }

    public string? Notes { get; set; }

    public DateTime OpenedAtUtc { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }
}
