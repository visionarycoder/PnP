using System.Threading.Channels;

namespace CSharp.ActorModel;

// Mailbox implementation
public interface IMailbox
{
    Task Post(IMessage message, IActorRef? sender = null);
    Task<MessageEnvelope?> Receive(CancellationToken cancellationToken);
    int Count { get; }
    bool HasMessages { get; }
}

public record MessageEnvelope(IMessage Message, IActorRef? Sender, DateTime ReceivedAt);

public class BoundedMailbox : IMailbox, IDisposable
{
    private readonly Channel<MessageEnvelope> channel;
    private readonly ChannelWriter<MessageEnvelope> writer;
    private readonly ChannelReader<MessageEnvelope> reader;
    private volatile bool isDisposed = false;

    public BoundedMailbox(int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        channel = Channel.CreateBounded<MessageEnvelope>(options);
        writer = channel.Writer;
        reader = channel.Reader;
    }

    public async Task Post(IMessage message, IActorRef? sender = null)
    {
        if (isDisposed) return;

        var envelope = new MessageEnvelope(message, sender, DateTime.UtcNow);
        
        try
        {
            await writer.WriteAsync(envelope);
        }
        catch (ObjectDisposedException)
        {
            // Mailbox has been disposed
        }
    }

    public async Task<MessageEnvelope?> Receive(CancellationToken cancellationToken)
    {
        try
        {
            return await reader.ReadAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public int Count => reader.CanCount ? reader.Count : 0;
    public bool HasMessages => reader.CanCount && reader.Count > 0;

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            writer.TryComplete();
        }
    }
}