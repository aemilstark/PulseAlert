using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PulseAlert.Core.Entities;

namespace PulseAlert.Infrastructure.Data.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.HasKey(d => d.DeviceId);

        builder.Property(d => d.Imei)
            .HasColumnName("IMEI")
            .HasMaxLength(15);

        builder.HasIndex(d => d.Imei).IsUnique();

        builder.Property(d => d.ModelName).HasMaxLength(100);

        builder.Property(d => d.FirmwareVersion).HasMaxLength(50);

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
