using Microsoft.EntityFrameworkCore;
using PulseAlert.Core.Entities;
using PulseAlert.Core.Telemetry;
using PulseAlert.Infrastructure.Data;

namespace PulseAlert.Infrastructure.Telemetry;

/// <summary>Saves a batch of pings, registering devices whose IMEI has not been seen before.</summary>
public sealed class TelemetryBatchProcessor(PulseAlertDbContext db, TimeProvider timeProvider) : ITelemetryBatchProcessor
{
    public async Task ProcessAsync(IReadOnlyList<IncomingPing> batch, CancellationToken cancellationToken)
    {
        var receivedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        var imeis = batch.Select(p => p.Imei).Distinct().ToList();
        var devices = await db.Devices
            .Where(d => imeis.Contains(d.Imei))
            .ToDictionaryAsync(d => d.Imei, cancellationToken);

        foreach (var ping in batch)
        {
            if (!devices.TryGetValue(ping.Imei, out var device))
            {
                device = new Device
                {
                    DeviceId = Guid.CreateVersion7(),
                    Imei = ping.Imei,
                    ModelName = ping.ModelName,
                    FirmwareVersion = ping.FirmwareVersion,
                    Status = DeviceStatus.Active,
                    CreatedAtUtc = receivedAtUtc,
                };
                devices.Add(device.Imei, device);
                db.Devices.Add(device);
            }

            db.TelemetryPings.Add(new TelemetryPing
            {
                PingId = Guid.CreateVersion7(),
                DeviceId = device.DeviceId,
                Latitude = ping.Latitude,
                Longitude = ping.Longitude,
                AltitudeMeters = ping.AltitudeMeters,
                BatteryPercentage = ping.BatteryPercentage,
                IsSosActive = ping.IsSosActive,
                PowerOff = ping.PowerOff,
                RecordedAtUtc = ping.RecordedAtUtc,
                ReceivedAtUtc = receivedAtUtc,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
