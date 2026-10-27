using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using PulseAlert.Core.Entities;
using PulseAlert.Infrastructure.Data;
using PulseAlert.Infrastructure.Data.Configurations;

namespace PulseAlert.UnitTests.Infrastructure;

// Building the model and its SQL needs the Npgsql provider but never opens a connection.
public class PulseAlertDbContextModelTests : IDisposable
{
    private readonly PulseAlertDbContext _context = new(
        new DbContextOptionsBuilder<PulseAlertDbContext>()
            .UseNpgsql("Host=unused")
            .Options);

    public void Dispose() => _context.Dispose();

    // The design-time model keeps details the runtime model drops, such as index sort order.
    private IModel Model => _context.GetService<IDesignTimeModel>().Model;

    private IEntityType Entity<T>() => Model.FindEntityType(typeof(T))!;

    [Fact]
    public void Imei_HasUniqueIndex()
    {
        var index = Entity<Device>().GetIndexes()
            .Single(i => i.Properties.Single().Name == nameof(Device.Imei));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void OpenIncidents_AreLimitedToOnePerDevice()
    {
        var index = Entity<EmergencyIncident>().GetIndexes()
            .Single(i => i.GetDatabaseName() == EmergencyIncidentConfiguration.OneOpenIncidentPerDeviceIndex);

        Assert.True(index.IsUnique);
        Assert.Equal(nameof(EmergencyIncident.DeviceId), index.Properties.Single().Name);
        Assert.Equal("\"Status\" <> 'Resolved'", index.GetFilter());
    }

    [Fact]
    public void TelemetryPings_HaveLatestPingIndex()
    {
        var index = Entity<TelemetryPing>().GetIndexes()
            .Single(i => i.Properties.Count == 2);

        Assert.Equal(
            [nameof(TelemetryPing.DeviceId), nameof(TelemetryPing.RecordedAtUtc)],
            index.Properties.Select(p => p.Name));
        Assert.Equal([false, true], index.IsDescending);
    }

    [Theory]
    [InlineData(typeof(Device), nameof(Device.Status))]
    [InlineData(typeof(EmergencyIncident), nameof(EmergencyIncident.Status))]
    public void Statuses_AreStoredAsStrings(Type entity, string property)
    {
        var status = Model.FindEntityType(entity)!.FindProperty(property)!;

        Assert.Equal(typeof(string), status.GetProviderClrType());
    }

    [Theory]
    [InlineData(nameof(TelemetryPing.Latitude))]
    [InlineData(nameof(TelemetryPing.Longitude))]
    public void Coordinates_HavePrecision9Scale6(string property)
    {
        var coordinate = Entity<TelemetryPing>().FindProperty(property)!;

        Assert.Equal(9, coordinate.GetPrecision());
        Assert.Equal(6, coordinate.GetScale());
    }

    [Fact]
    public void CreateScript_ContainsPartialUniqueIndex()
    {
        var script = _context.Database.GenerateCreateScript();

        Assert.Contains(
            "CREATE UNIQUE INDEX \"IX_EmergencyIncidents_DeviceId_Unresolved\" ON \"EmergencyIncidents\" (\"DeviceId\") WHERE \"Status\" <> 'Resolved';",
            script);
    }
}
