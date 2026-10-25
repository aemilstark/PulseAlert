using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseAlert.Core.Entities;

namespace PulseAlert.Infrastructure.Data.Configurations;

public class EmergencyIncidentConfiguration : IEntityTypeConfiguration<EmergencyIncident>
{
    public const string OneOpenIncidentPerDeviceIndex = "IX_EmergencyIncidents_DeviceId_Unresolved";

    public void Configure(EntityTypeBuilder<EmergencyIncident> builder)
    {
        builder.HasKey(i => i.IncidentId);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(i => i.Notes).HasColumnType("text");

        builder.HasOne(i => i.Device)
            .WithMany(d => d.EmergencyIncidents)
            .HasForeignKey(i => i.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.TriggerPing)
            .WithMany()
            .HasForeignKey(i => i.TriggerPingId)
            .OnDelete(DeleteBehavior.Restrict);

        // Partial unique index: a device can have at most one incident that is not resolved.
        builder.HasIndex(i => i.DeviceId)
            .HasDatabaseName(OneOpenIncidentPerDeviceIndex)
            .IsUnique()
            .HasFilter($"\"Status\" <> '{nameof(IncidentStatus.Resolved)}'");
    }
}
