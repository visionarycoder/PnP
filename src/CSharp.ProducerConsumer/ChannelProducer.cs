using System.Threading.Channels;

using Microsoft.Extensions.Logging;

namespace CSharp.ProducerConsumer;

public class ChannelProducer<T>(ChannelWriter<T> writer, ILogger? logger = null) : IProducer<T>
{
    private volatile bool isCompleted = false;
    private volatile bool isDisposed = false;

    public event EventHandler<ProducerEventArgs<T>>? ItemProduced;
    public event EventHandler<Exception>? ProductionError;
    public bool IsCompleted => isCompleted;

    public async Task ProduceAsync(T item, CancellationToken cancellationToken = default)
    {
        if (isDisposed || isCompleted) return;

        try
        {
            await writer.WriteAsync(item, cancellationToken);
            
            var queueSize = -1; // Queue size not available from ChannelWriter
                
            logger?.LogTrace("Item produced. Queue size: {QueueSize}", queueSize);
            ItemProduced?.Invoke(this, new ProducerEventArgs<T>(item, queueSize));
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error producing item");
            ProductionError?.Invoke(this, ex);
            throw;
        }
    }

    public async Task ProduceBatchAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            await ProduceAsync(item, cancellationToken);
        }
    }

    public async Task CompleteAsync()
    {
        if (!isCompleted && !isDisposed)
        {
            writer.Complete();
            isCompleted = true;
            logger?.LogInformation("Producer completed");
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            if (!isCompleted)
            {
                CompleteAsync().Wait(TimeSpan.FromSeconds(5));
            }
        }
    }
}