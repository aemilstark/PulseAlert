using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PulseAlert.Api.Workers;
using PulseAlert.Core.Telemetry;
using PulseAlert.Infrastructure.Telemetry;
using PulseAlert.UnitTests.Infrastructure;

namespace PulseAlert.UnitTests.Api;

// These tests run the worker against a real ChannelTelemetryQueue and a mocked batch processor.
public class TelemetryWorkerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly ChannelTelemetryQueue _queue = ChannelTelemetryQueueTests.CreateQueue();
    private readonly Mock<ITelemetryBatchProcessor> _processor = new();
    private readonly List<IReadOnlyList<IncomingPing>> _processedBatches = [];
    private readonly SemaphoreSlim _batchProcessed = new(0);

    private TelemetryWorker CreateWorker(int maxBatchSize = 500)
    {
        var services = new ServiceCollection()
            .AddScoped(_ => _processor.Object)
            .BuildServiceProvider();

        return new TelemetryWorker(
            _queue,
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new TelemetryOptions { MaxBatchSize = maxBatchSize }),
            NullLogger<TelemetryWorker>.Instance);
    }

    // Records each batch, then signals the test, whether or not the processor call succeeds.
    private void RecordBatches(Func<int, Task>? behavior = null)
    {
        _processor
            .Setup(p => p.ProcessAsync(It.IsAny<IReadOnlyList<IncomingPing>>(), It.IsAny<CancellationToken>()))
            .Returns(async (IReadOnlyList<IncomingPing> batch, CancellationToken _) =>
            {
                lock (_processedBatches)
                {
                    _processedBatches.Add(batch);
                }

                try
                {
                    await (behavior?.Invoke(_processedBatches.Count) ?? Task.CompletedTask);
                }
                finally
                {
                    _batchProcessed.Release();
                }
            });
    }

    private async Task WaitForBatches(int count)
    {
        for (var i = 0; i < count; i++)
        {
            Assert.True(await _batchProcessed.WaitAsync(Timeout), $"Timed out waiting for batch {i + 1}.");
        }
    }

    [Fact]
    public async Task QueuedPings_AreHandedToTheProcessor()
    {
        RecordBatches();
        var worker = CreateWorker();
        await worker.StartAsync(CancellationToken.None);

        _queue.TryEnqueue(ChannelTelemetryQueueTests.Ping("1"));
        await WaitForBatches(1);
        _queue.TryEnqueue(ChannelTelemetryQueueTests.Ping("2"));
        await WaitForBatches(1);

        await worker.StopAsync(CancellationToken.None);
        Assert.Equal(["1", "2"], _processedBatches.SelectMany(b => b).Select(p => p.Imei));
    }

    [Fact]
    public async Task Batches_RespectMaxBatchSize()
    {
        RecordBatches();
        for (var i = 0; i < 5; i++)
        {
            _queue.TryEnqueue(ChannelTelemetryQueueTests.Ping(i.ToString()));
        }

        var worker = CreateWorker(maxBatchSize: 2);
        await worker.StartAsync(CancellationToken.None);
        await WaitForBatches(3);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal([2, 2, 1], _processedBatches.Select(b => b.Count));
    }

    [Fact]
    public async Task FailedBatch_DoesNotStopTheWorker()
    {
        RecordBatches(batchNumber => batchNumber == 1
            ? throw new InvalidOperationException("Database unavailable.")
            : Task.CompletedTask);
        var worker = CreateWorker();
        await worker.StartAsync(CancellationToken.None);

        _queue.TryEnqueue(ChannelTelemetryQueueTests.Ping("1"));
        await WaitForBatches(1);
        _queue.TryEnqueue(ChannelTelemetryQueueTests.Ping("2"));
        await WaitForBatches(1);

        await worker.StopAsync(CancellationToken.None);
        Assert.Equal(2, _processedBatches.Count);
        Assert.Equal("2", _processedBatches[1].Single().Imei);
    }

    [Fact]
    public async Task StopAsync_EndsTheWorkerWhileItWaitsForPings()
    {
        var worker = CreateWorker();
        await worker.StartAsync(CancellationToken.None);

        await worker.StopAsync(CancellationToken.None).WaitAsync(Timeout);

        // If StopAsync wins the race with startup, the task ends Canceled rather than RanToCompletion. Either is fine.
        Assert.True(worker.ExecuteTask!.IsCompleted);
        Assert.False(worker.ExecuteTask.IsFaulted);
    }
}
