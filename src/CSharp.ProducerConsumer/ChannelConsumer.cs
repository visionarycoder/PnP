using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;

namespace CSharp.ProducerConsumer;

public class ChannelConsumer<T>(ChannelReader<T> reader, ILogger? logger = null) : IConsumer<T>
{
    private volatile bool isDisposed = false;

    public event EventHandler<ConsumerEventArgs<T>>? ItemConsumed;
    public event EventHandler<Exception>? ConsumptionError;

    public async Task<T> ConsumeAsync(CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(ChannelConsumer<T>));

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var item = await reader.ReadAsync(cancellationToken);
            stopwatch.Stop();

            logger?.LogTrace("Item consumed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            ItemConsumed?.Invoke(this, new ConsumerEventArgs<T>(item, stopwatch.Elapsed));
            
            return item;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error consuming item");
            ConsumptionError?.Invoke(this, ex);
            throw;
        }
    }

    public async Task<IEnumerable<T>> ConsumeBatchAsync(int maxItems, CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(ChannelConsumer<T>));

        var items = new List<T>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            for (int i = 0; i < maxItems && await reader.WaitToReadAsync(cancellationToken); i++)
            {
                if (reader.TryRead(out var item))
                {
                    items.Add(item);
                }
            }

            stopwatch.Stop();
            
            foreach (var item in items)
            {
                ItemConsumed?.Invoke(this, new ConsumerEventArgs<T>(item, stopwatch.Elapsed));
            }

            logger?.LogTrace("Consumed batch of {ItemCount} items in {ElapsedMs}ms", 
                items.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error consuming batch");
            ConsumptionError?.Invoke(this, ex);
            throw;
        }

        return items;
    }

    public async IAsyncEnumerable<T> ConsumeAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(ChannelConsumer<T>));

        await foreach (var item in reader.ReadAllAsync(cancellationToken))
        {
            var stopwatch = Stopwatch.StartNew();
            yield return item;
            stopwatch.Stop();

            ItemConsumed?.Invoke(this, new ConsumerEventArgs<T>(item, stopwatch.Elapsed));
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            logger?.LogInformation("Consumer disposed");
        }
    }
}