using Microsoft.Extensions.Options;
using PulseAlert.Core.Telemetry;
using PulseAlert.Infrastructure.Telemetry;

namespace PulseAlert.UnitTests.Infrastructure;

public class ChannelTelemetryQueueTests
{
    private static readonly IncomingPing Ping = new(
        "300234063918230", "Explorer Alpha", "2.1.0", 35.7796m, -78.63m, 120.5, 84,
        IsSosActive: false, PowerOff: false, new DateTime(2026, 10, 17, 13, 42, 10, DateTimeKind.Utc));

    [Fact]
    public void TryEnqueue_RejectsPingsOnceCapacityIsReached()
    {
        var queue = new ChannelTelemetryQueue(Options.Create(new TelemetryOptions { QueueCapacity = 2 }));

        Assert.True(queue.TryEnqueue(Ping));
        Assert.True(queue.TryEnqueue(Ping));
        Assert.False(queue.TryEnqueue(Ping));
    }
}
