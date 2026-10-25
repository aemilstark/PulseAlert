using Microsoft.EntityFrameworkCore;
using PulseAlert.Core.Entities;

namespace PulseAlert.Infrastructure.Data;

public class PulseAlertDbContext(DbContextOptions<PulseAlertDbContext> options) : DbContext(options)
{
    public DbSet<Device> Devices => Set<Device>();

    public DbSet<TelemetryPing> TelemetryPings => Set<TelemetryPing>();

    public DbSet<EmergencyIncident> EmergencyIncidents => Set<EmergencyIncident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PulseAlertDbContext).Assembly);
    }
}
