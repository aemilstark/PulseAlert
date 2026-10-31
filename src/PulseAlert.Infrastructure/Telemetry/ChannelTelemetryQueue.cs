using System.Threading.Channels;
using Microsoft.Extensions.Options;
using PulseAlert.Core.Telemetry;

namespace PulseAlert.Infrastructure.Telemetry;

public sealed class ChannelTelemetryQueue : ITelemetryQueue
{
    private readonly Channel<IncomingPing> _channel;

    public ChannelTelemetryQueue(IOptions<TelemetryOptions> options)
    {
        // In Wait mode TryWrite returns false when the channel is full, rather than dropping a ping.
        _channel = Channel.CreateBounded<IncomingPing>(new BoundedChannelOptions(options.Value.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
        });
    }

    public bool TryEnqueue(IncomingPing ping) => _channel.Writer.TryWrite(ping);
}
