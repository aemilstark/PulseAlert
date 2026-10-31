using System.ComponentModel.DataAnnotations;
using System.Globalization;
using PulseAlert.Api.Contracts;

namespace PulseAlert.UnitTests.Api;

public class TelemetryPingRequestValidationTests
{
    internal static TelemetryPingRequest ValidRequest() => new()
    {
        Imei = "300234063918230",
        ModelName = "Explorer Alpha",
        FirmwareVersion = "2.1.0",
        Latitude = 35.779600m,
        Longitude = -78.630000m,
        AltitudeMeters = 120.5,
        BatteryPercentage = 84,
        IsSosActive = false,
        PowerOff = false,
        RecordedAtUtc = new DateTimeOffset(2026, 10, 17, 13, 42, 10, TimeSpan.Zero),
    };

    // Returns the names of the members that failed validation, in alphabetical order.
    private static List<string> InvalidMembers(TelemetryPingRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return [.. results.SelectMany(r => r.MemberNames).Order()];
    }

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        Assert.Empty(InvalidMembers(ValidRequest()));
    }

    [Fact]
    public void MissingAltitude_PassesValidation()
    {
        Assert.Empty(InvalidMembers(ValidRequest() with { AltitudeMeters = null }));
    }

    [Fact]
    public void EmptyRequest_FailsEveryRequiredField()
    {
        Assert.Equal(
            [
                nameof(TelemetryPingRequest.BatteryPercentage),
                nameof(TelemetryPingRequest.FirmwareVersion),
                nameof(TelemetryPingRequest.Imei),
                nameof(TelemetryPingRequest.IsSosActive),
                nameof(TelemetryPingRequest.Latitude),
                nameof(TelemetryPingRequest.Longitude),
                nameof(TelemetryPingRequest.ModelName),
                nameof(TelemetryPingRequest.PowerOff),
                nameof(TelemetryPingRequest.RecordedAtUtc),
            ],
            InvalidMembers(new TelemetryPingRequest()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("30023406391823A")]
    [InlineData("3002340639182301")]
    [InlineData(" 300234063918230")]
    public void InvalidImei_FailsValidation(string imei)
    {
        Assert.Equal([nameof(TelemetryPingRequest.Imei)], InvalidMembers(ValidRequest() with { Imei = imei }));
    }

    [Theory]
    [InlineData("-90.000001")]
    [InlineData("90.000001")]
    public void LatitudeOutOfRange_FailsValidation(string latitude)
    {
        var request = ValidRequest() with { Latitude = decimal.Parse(latitude, CultureInfo.InvariantCulture) };

        Assert.Equal([nameof(TelemetryPingRequest.Latitude)], InvalidMembers(request));
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(90)]
    public void LatitudeAtLimit_PassesValidation(int latitude)
    {
        Assert.Empty(InvalidMembers(ValidRequest() with { Latitude = latitude }));
    }

    [Theory]
    [InlineData("-180.000001")]
    [InlineData("180.000001")]
    public void LongitudeOutOfRange_FailsValidation(string longitude)
    {
        var request = ValidRequest() with { Longitude = decimal.Parse(longitude, CultureInfo.InvariantCulture) };

        Assert.Equal([nameof(TelemetryPingRequest.Longitude)], InvalidMembers(request));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void BatteryOutOfRange_FailsValidation(int battery)
    {
        var request = ValidRequest() with { BatteryPercentage = battery };

        Assert.Equal([nameof(TelemetryPingRequest.BatteryPercentage)], InvalidMembers(request));
    }

    [Fact]
    public void ModelNameTooLong_FailsValidation()
    {
        var request = ValidRequest() with { ModelName = new string('x', 101) };

        Assert.Equal([nameof(TelemetryPingRequest.ModelName)], InvalidMembers(request));
    }

    [Fact]
    public void ToIncomingPing_ConvertsRecordedTimeToUtc()
    {
        var recordedWithOffset = new DateTimeOffset(2026, 10, 17, 15, 42, 10, TimeSpan.FromHours(2));

        var ping = (ValidRequest() with { RecordedAtUtc = recordedWithOffset }).ToIncomingPing();

        Assert.Equal(new DateTime(2026, 10, 17, 13, 42, 10), ping.RecordedAtUtc);
        Assert.Equal(DateTimeKind.Utc, ping.RecordedAtUtc.Kind);
    }
}
