using System.Threading.Channels;

using Microsoft.Extensions.Logging;

namespace CSharp.ProducerConsumer;

public class BackpressureProducer<T> : IProducer<T>
{
    private readonly Channel<T> channel;
    private readonly ILogger? logger;
    private readonly SemaphoreSlim rateLimitSemaphore;
    private volatile bool isCompleted = false;
    private volatile bool isDisposed = false;
    private volatile int currentRate = 100;
    private readonly Timer rateLimitTimer;

    public event EventHandler<ProducerEventArgs<T>>? ItemProduced;
    public event EventHandler<Exception>? ProductionError;

    public BackpressureProducer(int capacity = 1000, int initialRate = 100, ILogger? logger = null)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        channel = Channel.CreateBounded<T>(options);
        this.logger = logger;
        currentRate = initialRate;
        rateLimitSemaphore = new(currentRate, currentRate);

        rateLimitTimer = new Timer(RefillTokens, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public bool IsCompleted => isCompleted;
    public ChannelReader<T> Reader => channel.Reader;
    public int CurrentRate => currentRate;
    public int QueueSize => channel.Reader.CanCount ? channel.Reader.Count : -1;

    public async Task ProduceAsync(T item, CancellationToken cancellationToken = default)
    {
        if (isDisposed || isCompleted) return;

        await rateLimitSemaphore.WaitAsync(cancellationToken);

        try
        {
            var queueSizeBefore = QueueSize;
            
            await channel.Writer.WriteAsync(item, cancellationToken);
            
            var queueSizeAfter = QueueSize;
            
            AdjustRateBasedOnBackpressure(queueSizeAfter);
            
            logger?.LogTrace("Produced item. Queue size: {QueueSize}, Rate: {Rate}", 
                queueSizeAfter, currentRate);
            
            ItemProduced?.Invoke(this, new ProducerEventArgs<T>(item, queueSizeAfter));
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error producing item with backpressure control");
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
            channel.Writer.Complete();
            isCompleted = true;
            logger?.LogInformation("Backpressure producer completed");
        }
    }

    private void RefillTokens(object? state)
    {
        if (isDisposed) return;

        try
        {
            var tokensToAdd = Math.Max(0, currentRate - rateLimitSemaphore.CurrentCount);
            
            for (int i = 0; i < tokensToAdd; i++)
            {
                rateLimitSemaphore.Release();
            }
            
            if (tokensToAdd > 0)
            {
                logger?.LogTrace("Refilled {TokensAdded} rate limit tokens. Current rate: {Rate}", 
                    tokensToAdd, currentRate);
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Error refilling rate limit tokens");
        }
    }

    private void AdjustRateBasedOnBackpressure(int queueSize)
    {
        const int highPressureThreshold = 800;
        const int lowPressureThreshold = 200;
        
        if (queueSize > highPressureThreshold && currentRate > 10)
        {
            var newRate = Math.Max(10, (int)(currentRate * 0.9));
            if (newRate != currentRate)
            {
                currentRate = newRate;
                logger?.LogInformation("Reduced production rate to {Rate} due to backpressure (queue: {QueueSize})", 
                    currentRate, queueSize);
            }
        }
        else if (queueSize < lowPressureThreshold && currentRate < 1000)
        {
            var newRate = Math.Min(1000, (int)(currentRate * 1.1));
            if (newRate != currentRate)
            {
                currentRate = newRate;
                logger?.LogInformation("Increased production rate to {Rate} due to low backpressure (queue: {QueueSize})", 
                    currentRate, queueSize);
            }
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            
            rateLimitTimer?.Dispose();
            
            if (!isCompleted)
            {
                CompleteAsync().Wait(TimeSpan.FromSeconds(5));
            }
            
            rateLimitSemaphore?.Dispose();
        }
    }
}