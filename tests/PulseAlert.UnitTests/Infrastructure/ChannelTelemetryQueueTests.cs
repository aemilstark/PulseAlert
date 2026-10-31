using Microsoft.Extensions.Options;
using PulseAlert.Core.Telemetry;
using PulseAlert.Infrastructure.Telemetry;

namespace PulseAlert.UnitTests.Infrastructure;

public class ChannelTelemetryQueueTests
{
    internal static IncomingPing Ping(string imei = "300234063918230") => new(
        imei, "Explorer Alpha", "2.1.0", 35.7796m, -78.63m, 120.5, 84,
        IsSosActive: false, PowerOff: false, new DateTime(2026, 10, 17, 13, 42, 10, DateTimeKind.Utc));

    internal static ChannelTelemetryQueue CreateQueue(int capacity = 100) =>
        new(Options.Create(new TelemetryOptions { QueueCapacity = capacity }));

    [Fact]
    public void TryEnqueue_RejectsPingsOnceCapacityIsReached()
    {
        var queue = CreateQueue(capacity: 2);

        Assert.True(queue.TryEnqueue(Ping()));
        Assert.True(queue.TryEnqueue(Ping()));
        Assert.False(queue.TryEnqueue(Ping()));
    }

    [Fact]
    public async Task ReadBatchAsync_ReturnsPingsInArrivalOrder()
    {
        var queue = CreateQueue();
        queue.TryEnqueue(Ping("1"));
        queue.TryEnqueue(Ping("2"));
        queue.TryEnqueue(Ping("3"));

        var batch = await queue.ReadBatchAsync(10, CancellationToken.None);

        Assert.Equal(["1", "2", "3"], batch.Select(p => p.Imei));
    }

    [Fact]
    public async Task ReadBatchAsync_TakesAtMostMaxBatchSize()
    {
        var queue = CreateQueue();
        for (var i = 0; i < 5; i++)
        {
            queue.TryEnqueue(Ping(i.ToString()));
        }

        var first = await queue.ReadBatchAsync(3, CancellationToken.None);
        var second = await queue.ReadBatchAsync(3, CancellationToken.None);

        Assert.Equal(["0", "1", "2"], first.Select(p => p.Imei));
        Assert.Equal(["3", "4"], second.Select(p => p.Imei));
    }

    [Fact]
    public async Task ReadBatchAsync_WaitsUntilAPingArrives()
    {
        var queue = CreateQueue();

        var read = queue.ReadBatchAsync(10, CancellationToken.None).AsTask();
        Assert.False(read.IsCompleted);

        queue.TryEnqueue(Ping());

        Assert.Single(await read.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task ReadBatchAsync_ThrowsWhenCancelledWhileWaiting()
    {
        var queue = CreateQueue();
        using var cts = new CancellationTokenSource();

        var read = queue.ReadBatchAsync(10, cts.Token).AsTask();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
    }

    [Fact]
    public async Task ReadingFreesCapacity()
    {
        var queue = CreateQueue(capacity: 1);
        queue.TryEnqueue(Ping());

        await queue.ReadBatchAsync(10, CancellationToken.None);

        Assert.True(queue.TryEnqueue(Ping()));
    }
}
