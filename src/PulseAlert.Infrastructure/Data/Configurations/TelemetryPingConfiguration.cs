using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseAlert.Core.Entities;

namespace PulseAlert.Infrastructure.Data.Configurations;

public class TelemetryPingConfiguration : IEntityTypeConfiguration<TelemetryPing>
{
    public void Configure(EntityTypeBuilder<TelemetryPing> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_TelemetryPings_Latitude", "\"Latitude\" BETWEEN -90 AND 90");
            t.HasCheckConstraint("CK_TelemetryPings_Longitude", "\"Longitude\" BETWEEN -180 AND 180");
            t.HasCheckConstraint("CK_TelemetryPings_BatteryPercentage", "\"BatteryPercentage\" BETWEEN 0 AND 100");
        });

        builder.HasKey(p => p.PingId);

        builder.Property(p => p.Latitude).HasPrecision(9, 6);

        builder.Property(p => p.Longitude).HasPrecision(9, 6);

        builder.HasOne(p => p.Device)
            .WithMany(d => d.TelemetryPings)
            .HasForeignKey(p => p.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Finds the latest ping for a device without scanning its whole history.
        builder.HasIndex(p => new { p.DeviceId, p.RecordedAtUtc })
            .IsDescending(false, true);
    }
}
