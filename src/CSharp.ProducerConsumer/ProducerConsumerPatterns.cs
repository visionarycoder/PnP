using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace CSharp.ProducerConsumer;

// Core interfaces for producer-consumer patterns
public interface IProducer<T> : IDisposable
{
    event EventHandler<ProducerEventArgs<T>>? ItemProduced;
    event EventHandler<Exception>? ProductionError;
    bool IsCompleted { get; }
    Task ProduceAsync(T item, CancellationToken cancellationToken = default);
    Task ProduceBatchAsync(IEnumerable<T> items, CancellationToken cancellationToken = default);
    Task CompleteAsync();
}

public interface IConsumer<T> : IDisposable
{
    event EventHandler<ConsumerEventArgs<T>>? ItemConsumed;
    event EventHandler<Exception>? ConsumptionError;
    Task<T> ConsumeAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> ConsumeBatchAsync(int maxItems, CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> ConsumeAllAsync(CancellationToken cancellationToken = default);
}

public interface IPipeline<TInput, TOutput> : IDisposable
{
    Task<TOutput> ProcessAsync(TInput input, CancellationToken cancellationToken = default);
    Task<IEnumerable<TOutput>> ProcessBatchAsync(IEnumerable<TInput> inputs, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TOutput> ProcessStreamAsync(IAsyncEnumerable<TInput> inputs, CancellationToken cancellationToken = default);
}

// Event argument classes
public class ProducerEventArgs<T>(T item, long queueSize) : EventArgs
{
    public T Item { get; } = item;
    public long QueueSize { get; } = queueSize;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public class ConsumerEventArgs<T>(T item, TimeSpan processingTime) : EventArgs
{
    public T Item { get; } = item;
    public TimeSpan ProcessingTime { get; } = processingTime;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

// Channel-based producer implementation
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

// Channel-based consumer implementation
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

// Priority-based producer-consumer using multiple channels
public class PriorityProducerConsumer<T> : IDisposable
{
    private readonly Dictionary<int, Channel<T>> priorityChannels = new();
    private readonly int[] priorities;
    private readonly ILogger? logger;
    private readonly ReaderWriterLockSlim lockSlim = new();
    private volatile bool isDisposed = false;

    public PriorityProducerConsumer(int[] priorities, int capacity = 1000, ILogger? logger = null)
    {
        this.priorities = priorities.OrderByDescending(p => p).ToArray();
        this.logger = logger;

        foreach (var priority in priorities)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };
            priorityChannels[priority] = Channel.CreateBounded<T>(options);
        }
    }

    public async Task ProduceAsync(T item, int priority, CancellationToken cancellationToken = default)
    {
        if (isDisposed) return;

        lockSlim.EnterReadLock();
        try
        {
            if (priorityChannels.TryGetValue(priority, out var channel))
            {
                await channel.Writer.WriteAsync(item, cancellationToken);
                logger?.LogTrace("Item produced with priority {Priority}", priority);
            }
            else
            {
                throw new ArgumentException($"Invalid priority: {priority}");
            }
        }
        finally
        {
            lockSlim.ExitReadLock();
        }
    }

    public async IAsyncEnumerable<(T Item, int Priority)> ConsumeAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (isDisposed) yield break;

        var readers = priorityChannels
            .OrderByDescending(kvp => kvp.Key)
            .Select(kvp => (Priority: kvp.Key, Reader: kvp.Value.Reader))
            .ToArray();

        while (!cancellationToken.IsCancellationRequested)
        {
            var consumed = false;

            // Try to consume from highest priority channels first
            foreach (var (priority, reader) in readers)
            {
                if (reader.TryRead(out var item))
                {
                    yield return (item, priority);
                    consumed = true;
                    logger?.LogTrace("Consumed item with priority {Priority}", priority);
                    break;
                }
            }

            if (!consumed)
            {
                // Wait for any channel to have data
                var waitTasks = readers
                    .Where(r => !r.Reader.Completion.IsCompleted)
                    .Select(r => r.Reader.WaitToReadAsync(cancellationToken).AsTask())
                    .ToArray();

                if (waitTasks.Length == 0) break;

                try
                {
                    await Task.WhenAny(waitTasks);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    public void CompleteProduction()
    {
        lockSlim.EnterWriteLock();
        try
        {
            foreach (var channel in priorityChannels.Values)
            {
                channel.Writer.Complete();
            }
            logger?.LogInformation("All priority producers completed");
        }
        finally
        {
            lockSlim.ExitWriteLock();
        }
    }

    public int GetQueueCount(int priority)
    {
        lockSlim.EnterReadLock();
        try
        {
            if (priorityChannels.TryGetValue(priority, out var channel))
            {
                return channel.Reader.CanCount ? channel.Reader.Count : -1;
            }
            return 0;
        }
        finally
        {
            lockSlim.ExitReadLock();
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            CompleteProduction();
            lockSlim?.Dispose();
        }
    }
}

// Batch processor for efficient bulk operations
public class BatchProcessor<TInput, TOutput> : IPipeline<TInput, TOutput>, IDisposable
{
    private readonly Func<IReadOnlyList<TInput>, CancellationToken, Task<IEnumerable<TOutput>>> batchProcessor;
    private readonly Channel<TInput> inputChannel;
    private readonly Channel<TOutput> outputChannel;
    private readonly int batchSize;
    private readonly TimeSpan batchTimeout;
    private readonly ILogger? logger;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly Task processingTask;
    private volatile bool isDisposed = false;

    public BatchProcessor(
        Func<IReadOnlyList<TInput>, CancellationToken, Task<IEnumerable<TOutput>>> batchProcessor,
        int batchSize = 100,
        TimeSpan? batchTimeout = null,
        int inputCapacity = 1000,
        int outputCapacity = 1000,
        ILogger? logger = null)
    {
        this.batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
        this.batchSize = batchSize;
        this.batchTimeout = batchTimeout ?? TimeSpan.FromSeconds(1);
        this.logger = logger;

        // Create bounded channels
        var inputOptions = new BoundedChannelOptions(inputCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        var outputOptions = new BoundedChannelOptions(outputCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        };

        inputChannel = Channel.CreateBounded<TInput>(inputOptions);
        outputChannel = Channel.CreateBounded<TOutput>(outputOptions);
        cancellationTokenSource = new CancellationTokenSource();

        // Start background processing task
        processingTask = ProcessBatchesAsync(cancellationTokenSource.Token);
    }

    public async Task<TOutput> ProcessAsync(TInput input, CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(BatchProcessor<TInput, TOutput>));

        await inputChannel.Writer.WriteAsync(input, cancellationToken);
        return await outputChannel.Reader.ReadAsync(cancellationToken);
    }

    public async Task<IEnumerable<TOutput>> ProcessBatchAsync(IEnumerable<TInput> inputs, CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(BatchProcessor<TInput, TOutput>));

        var inputList = inputs.ToList();
        var outputs = new List<TOutput>();

        foreach (var input in inputList)
        {
            await inputChannel.Writer.WriteAsync(input, cancellationToken);
        }

        for (int i = 0; i < inputList.Count; i++)
        {
            var output = await outputChannel.Reader.ReadAsync(cancellationToken);
            outputs.Add(output);
        }

        return outputs;
    }

    public async IAsyncEnumerable<TOutput> ProcessStreamAsync(
        IAsyncEnumerable<TInput> inputs, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(BatchProcessor<TInput, TOutput>));

        var inputTask = Task.Run(async () =>
        {
            await foreach (var input in inputs.WithCancellation(cancellationToken))
            {
                await inputChannel.Writer.WriteAsync(input, cancellationToken);
            }
        }, cancellationToken);

        await foreach (var output in outputChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return output;
        }

        await inputTask;
    }

    private async Task ProcessBatchesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var batch = new List<TInput>();
            var lastBatchTime = DateTime.UtcNow;

            await foreach (var input in inputChannel.Reader.ReadAllAsync(cancellationToken))
            {
                batch.Add(input);

                var shouldProcessBatch = batch.Count >= batchSize ||
                                       DateTime.UtcNow - lastBatchTime >= batchTimeout;

                if (shouldProcessBatch && batch.Count > 0)
                {
                    await ProcessBatch(batch, cancellationToken);
                    batch.Clear();
                    lastBatchTime = DateTime.UtcNow;
                }
            }

            if (batch.Count > 0)
            {
                await ProcessBatch(batch, cancellationToken);
            }

            outputChannel.Writer.Complete();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error in batch processing loop");
            outputChannel.Writer.Complete(ex);
        }
    }

    private async Task ProcessBatch(List<TInput> batch, CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var outputs = await batchProcessor(batch.AsReadOnly(), cancellationToken);
            stopwatch.Stop();
            
            logger?.LogDebug("Processed batch of {BatchSize} items in {ElapsedMs}ms", 
                batch.Count, stopwatch.ElapsedMilliseconds);

            foreach (var output in outputs)
            {
                await outputChannel.Writer.WriteAsync(output, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error processing batch of {BatchSize} items", batch.Count);
            throw;
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            
            inputChannel.Writer.Complete();
            cancellationTokenSource.Cancel();
            
            try
            {
                processingTask.Wait(TimeSpan.FromSeconds(10));
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Error waiting for processing task to complete");
            }
            
            cancellationTokenSource?.Dispose();
        }
    }
}

// Backpressure-aware producer with adaptive rate limiting
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

// Performance metrics for producer-consumer scenarios
public class ProducerConsumerMetrics
{
    private long itemsProduced = 0;
    private long itemsConsumed = 0;
    private long totalProducingTime = 0;
    private long totalConsumingTime = 0;
    private long peakQueueSize = 0;
    private readonly object lockObject = new();
    private DateTime startTime = DateTime.UtcNow;

    public long ItemsProduced => itemsProduced;
    public long ItemsConsumed => itemsConsumed;
    public long ItemsInFlight => itemsProduced - itemsConsumed;
    public long PeakQueueSize => peakQueueSize;
    
    public double ProducingThroughput
    {
        get
        {
            var elapsed = DateTime.UtcNow - startTime;
            return elapsed.TotalSeconds > 0 ? itemsProduced / elapsed.TotalSeconds : 0.0;
        }
    }
    
    public double ConsumingThroughput
    {
        get
        {
            var elapsed = DateTime.UtcNow - startTime;
            return elapsed.TotalSeconds > 0 ? itemsConsumed / elapsed.TotalSeconds : 0.0;
        }
    }

    public TimeSpan AverageProducingTime => itemsProduced > 0 
        ? TimeSpan.FromTicks(totalProducingTime / itemsProduced) 
        : TimeSpan.Zero;

    public TimeSpan AverageConsumingTime => itemsConsumed > 0 
        ? TimeSpan.FromTicks(totalConsumingTime / itemsConsumed) 
        : TimeSpan.Zero;

    public void RecordItemProduced(TimeSpan producingTime, long queueSize = 0)
    {
        Interlocked.Increment(ref itemsProduced);
        Interlocked.Add(ref totalProducingTime, producingTime.Ticks);
        
        var currentPeak = peakQueueSize;
        while (queueSize > currentPeak && 
               Interlocked.CompareExchange(ref peakQueueSize, queueSize, currentPeak) != currentPeak)
        {
            currentPeak = peakQueueSize;
        }
    }

    public void RecordItemConsumed(TimeSpan consumingTime)
    {
        Interlocked.Increment(ref itemsConsumed);
        Interlocked.Add(ref totalConsumingTime, consumingTime.Ticks);
    }

    public void Reset()
    {
        lock (lockObject)
        {
            itemsProduced = 0;
            itemsConsumed = 0;
            totalProducingTime = 0;
            totalConsumingTime = 0;
            peakQueueSize = 0;
            startTime = DateTime.UtcNow;
        }
    }

    public ProducerConsumerStats GetStats()
    {
        return new ProducerConsumerStats
        {
            ItemsProduced = ItemsProduced,
            ItemsConsumed = ItemsConsumed,
            ItemsInFlight = ItemsInFlight,
            PeakQueueSize = PeakQueueSize,
            ProducingThroughput = ProducingThroughput,
            ConsumingThroughput = ConsumingThroughput,
            AverageProducingTime = AverageProducingTime,
            AverageConsumingTime = AverageConsumingTime,
            Efficiency = ProducingThroughput > 0 ? ConsumingThroughput / ProducingThroughput : 0.0
        };
    }
}

public class ProducerConsumerStats
{
    public long ItemsProduced { get; set; }
    public long ItemsConsumed { get; set; }
    public long ItemsInFlight { get; set; }
    public long PeakQueueSize { get; set; }
    public double ProducingThroughput { get; set; }
    public double ConsumingThroughput { get; set; }
    public TimeSpan AverageProducingTime { get; set; }
    public TimeSpan AverageConsumingTime { get; set; }
    public double Efficiency { get; set; }
}

// Work item class for examples
public record WorkItem(string Id, TimeSpan ProcessingTime);