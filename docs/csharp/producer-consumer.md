# Producer-Consumer Patterns

**Description**: Comprehensive producer-consumer patterns with channels, bounded queues, backpressure handling, multiple producers/consumers, priority processing, batch operations, streaming data processing, and high-performance concurrent data pipelines for scalable applications.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Linq;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;
using System.Collections.Immutable;

// Base interfaces for producer-consumer patterns
public interface IProducer<T> : IDisposable
{
    Task ProduceAsync(T item, CancellationToken cancellationToken = default);
    Task ProduceBatchAsync(IEnumerable<T> items, CancellationToken cancellationToken = default);
    Task CompleteAsync();
    bool IsCompleted { get; }
    event EventHandler<ProducerEventArgs<T>> ItemProduced;
    event EventHandler<Exception> ProductionError;
}

public interface IConsumer<T> : IDisposable
{
    Task<T> ConsumeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ConsumeBatchAsync(int maxBatchSize, TimeSpan timeout, CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> ConsumeAllAsync(CancellationToken cancellationToken = default);
    bool HasItems { get; }
    event EventHandler<ConsumerEventArgs<T>> ItemConsumed;
    event EventHandler<Exception> ConsumptionError;
}

public interface IPipeline<TInput, TOutput> : IDisposable
{
    Task<TOutput> ProcessAsync(TInput input, CancellationToken cancellationToken = default);
    Task<IEnumerable<TOutput>> ProcessBatchAsync(IEnumerable<TInput> inputs, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TOutput> ProcessStreamAsync(IAsyncEnumerable<TInput> inputs, CancellationToken cancellationToken = default);
}

// Event arguments for producer-consumer events
public class ProducerEventArgs<T> : EventArgs
{
    public T Item { get; }
    public DateTime Timestamp { get; }
    public int QueueSize { get; }

    public ProducerEventArgs(T item, int queueSize)
    {
        Item = item;
        Timestamp = DateTime.UtcNow;
        QueueSize = queueSize;
    }
}

public class ConsumerEventArgs<T> : EventArgs
{
    public T Item { get; }
    public DateTime Timestamp { get; }
    public TimeSpan ProcessingTime { get; }
    public int RemainingItems { get; }

    public ConsumerEventArgs(T item, TimeSpan processingTime, int remainingItems)
    {
        Item = item;
        Timestamp = DateTime.UtcNow;
        ProcessingTime = processingTime;
        RemainingItems = remainingItems;
    }
}

// Channel-based producer implementation
public class ChannelProducer<T> : IProducer<T>
{
    private readonly ChannelWriter<T> writer;
    private readonly ILogger logger;
    private volatile bool isCompleted = false;
    private volatile bool isDisposed = false;
    private readonly SemaphoreSlim semaphore;

    public event EventHandler<ProducerEventArgs<T>> ItemProduced;
    public event EventHandler<Exception> ProductionError;

    public ChannelProducer(ChannelWriter<T> writer, ILogger logger = null, int maxConcurrency = 1)
    {
        this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
        this.logger = logger;
        semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    public bool IsCompleted => isCompleted;

    public async Task ProduceAsync(T item, CancellationToken cancellationToken = default)
    {
        if (isDisposed || isCompleted) return;

        await semaphore.WaitAsync(cancellationToken);
        
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            await writer.WriteAsync(item, cancellationToken);
            
            stopwatch.Stop();
            
            logger?.LogDebug("Produced item in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            // Get approximate queue size (if supported)
            var queueSize = writer.CanCount ? writer.Count : -1;
            
            ItemProduced?.Invoke(this, new ProducerEventArgs<T>(item, queueSize));
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error producing item");
            ProductionError?.Invoke(this, ex);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task ProduceBatchAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        if (isDisposed || isCompleted) return;

        await semaphore.WaitAsync(cancellationToken);
        
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var count = 0;
            
            foreach (var item in items)
            {
                await writer.WriteAsync(item, cancellationToken);
                count++;
                
                if (count % 100 == 0) // Periodic logging for large batches
                {
                    logger?.LogDebug("Produced batch of {Count} items so far", count);
                }
            }
            
            stopwatch.Stop();
            logger?.LogInformation("Produced batch of {Count} items in {ElapsedMs}ms", 
                count, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error producing batch");
            ProductionError?.Invoke(this, ex);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task CompleteAsync()
    {
        if (isCompleted || isDisposed) return;

        await semaphore.WaitAsync();
        
        try
        {
            if (!isCompleted)
            {
                writer.Complete();
                isCompleted = true;
                logger?.LogInformation("Producer completed");
            }
        }
        finally
        {
            semaphore.Release();
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
            
            semaphore?.Dispose();
        }
    }
}

// Channel-based consumer implementation
public class ChannelConsumer<T> : IConsumer<T>
{
    private readonly ChannelReader<T> reader;
    private readonly ILogger logger;
    private volatile bool isDisposed = false;
    private readonly SemaphoreSlim semaphore;

    public event EventHandler<ConsumerEventArgs<T>> ItemConsumed;
    public event EventHandler<Exception> ConsumptionError;

    public ChannelConsumer(ChannelReader<T> reader, ILogger logger = null, int maxConcurrency = 1)
    {
        this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        this.logger = logger;
        semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    public bool HasItems => reader.CanCount ? reader.Count > 0 : !reader.Completion.IsCompleted;

    public async Task<T> ConsumeAsync(CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(ChannelConsumer<T>));

        await semaphore.WaitAsync(cancellationToken);
        
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            var item = await reader.ReadAsync(cancellationToken);
            
            stopwatch.Stop();
            
            logger?.LogDebug("Consumed item in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            var remainingItems = reader.CanCount ? reader.Count : -1;
            
            ItemConsumed?.Invoke(this, new ConsumerEventArgs<T>(item, stopwatch.Elapsed, remainingItems));
            
            return item;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error consuming item");
            ConsumptionError?.Invoke(this, ex);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<T>> ConsumeBatchAsync(int maxBatchSize, TimeSpan timeout, 
        CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(ChannelConsumer<T>));

        await semaphore.WaitAsync(cancellationToken);
        
        try
        {
            var batch = new List<T>();
            var stopwatch = Stopwatch.StartNew();
            
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);
            
            while (batch.Count < maxBatchSize && stopwatch.Elapsed < timeout)
            {
                try
                {
                    var item = await reader.ReadAsync(combinedCts.Token);
                    batch.Add(item);
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                {
                    // Timeout occurred, return what we have
                    break;
                }
                catch (InvalidOperationException)
                {
                    // Channel completed, return what we have
                    break;
                }
            }
            
            stopwatch.Stop();
            
            logger?.LogDebug("Consumed batch of {Count} items in {ElapsedMs}ms", 
                batch.Count, stopwatch.ElapsedMilliseconds);
            
            return batch.AsReadOnly();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error consuming batch");
            ConsumptionError?.Invoke(this, ex);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async IAsyncEnumerable<T> ConsumeAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(ChannelConsumer<T>));

        await foreach (var item in reader.ReadAllAsync(cancellationToken))
        {
            var stopwatch = Stopwatch.StartNew();
            
            logger?.LogTrace("Yielding consumed item");
            
            yield return item;
            
            stopwatch.Stop();
            
            var remainingItems = reader.CanCount ? reader.Count : -1;
            ItemConsumed?.Invoke(this, new ConsumerEventArgs<T>(item, stopwatch.Elapsed, remainingItems));
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            semaphore?.Dispose();
        }
    }
}

// Priority producer-consumer with multiple priority levels
public class PriorityProducerConsumer<T> : IDisposable
{
    private readonly SortedDictionary<int, Channel<T>> priorityChannels;
    private readonly ReaderWriterLockSlim lockSlim;
    private readonly ILogger logger;
    private volatile bool isDisposed = false;

    public PriorityProducerConsumer(IEnumerable<int> priorities, int capacity = 1000, ILogger logger = null)
    {
        this.logger = logger;
        lockSlim = new ReaderWriterLockSlim();
        
        // Create channels for each priority level (sorted descending - higher priority first)
        priorityChannels = new SortedDictionary<int, Channel<T>>(Comparer<int>.Create((x, y) => y.CompareTo(x)));
        
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
                logger?.LogDebug("Produced item with priority {Priority}", priority);
            }
            else
            {
                throw new ArgumentException($"Unknown priority level: {priority}", nameof(priority));
            }
        }
        finally
        {
            lockSlim.ExitReadLock();
        }
    }

    public async Task<(T Item, int Priority)> ConsumeAsync(CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(PriorityProducerConsumer<T>));

        while (!cancellationToken.IsCancellationRequested)
        {
            lockSlim.EnterReadLock();
            try
            {
                // Try to consume from highest priority channel first
                foreach (var kvp in priorityChannels)
                {
                    var priority = kvp.Key;
                    var channel = kvp.Value;
                    
                    if (channel.Reader.TryRead(out var item))
                    {
                        logger?.LogDebug("Consumed item with priority {Priority}", priority);
                        return (item, priority);
                    }
                }
            }
            finally
            {
                lockSlim.ExitReadLock();
            }

            // No items available, wait briefly before retrying
            await Task.Delay(1, cancellationToken);
        }

        throw new OperationCanceledException();
    }

    public async IAsyncEnumerable<(T Item, int Priority)> ConsumeAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested && !isDisposed)
        {
            var hasItems = false;
            
            lockSlim.EnterReadLock();
            try
            {
                // Try to consume from highest priority channel first
                foreach (var kvp in priorityChannels)
                {
                    var priority = kvp.Key;
                    var channel = kvp.Value;
                    
                    if (channel.Reader.TryRead(out var item))
                    {
                        hasItems = true;
                        yield return (item, priority);
                        break; // Process one item at a time to maintain priority order
                    }
                }
            }
            finally
            {
                lockSlim.ExitReadLock();
            }

            if (!hasItems)
            {
                await Task.Delay(10, cancellationToken); // Wait longer when no items available
            }
        }
    }

    public void CompleteProduction(int? priority = null)
    {
        lockSlim.EnterWriteLock();
        try
        {
            if (priority.HasValue)
            {
                if (priorityChannels.TryGetValue(priority.Value, out var channel))
                {
                    channel.Writer.Complete();
                    logger?.LogInformation("Completed production for priority {Priority}", priority.Value);
                }
            }
            else
            {
                foreach (var channel in priorityChannels.Values)
                {
                    channel.Writer.Complete();
                }
                logger?.LogInformation("Completed production for all priorities");
            }
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
    private readonly ILogger logger;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly Task processingTask;
    private volatile bool isDisposed = false;

    public BatchProcessor(
        Func<IReadOnlyList<TInput>, CancellationToken, Task<IEnumerable<TOutput>>> batchProcessor,
        int batchSize = 100,
        TimeSpan? batchTimeout = null,
        int inputCapacity = 1000,
        int outputCapacity = 1000,
        ILogger logger = null)
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

        // Add input to channel
        await inputChannel.Writer.WriteAsync(input, cancellationToken);
        
        // Read the corresponding output
        return await outputChannel.Reader.ReadAsync(cancellationToken);
    }

    public async Task<IEnumerable<TOutput>> ProcessBatchAsync(IEnumerable<TInput> inputs, 
        CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(BatchProcessor<TInput, TOutput>));

        var inputList = inputs.ToList();
        var outputs = new List<TOutput>();

        // Write all inputs
        foreach (var input in inputList)
        {
            await inputChannel.Writer.WriteAsync(input, cancellationToken);
        }

        // Read corresponding outputs
        for (int i = 0; i < inputList.Count; i++)
        {
            var output = await outputChannel.Reader.ReadAsync(cancellationToken);
            outputs.Add(output);
        }

        return outputs;
    }

    public async IAsyncEnumerable<TOutput> ProcessStreamAsync(IAsyncEnumerable<TInput> inputs, 
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

        await inputTask; // Ensure input processing completes
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

            // Process any remaining items in the final batch
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
    private readonly ILogger logger;
    private readonly SemaphoreSlim rateLimitSemaphore;
    private volatile bool isCompleted = false;
    private volatile bool isDisposed = false;
    private volatile int currentRate = 100; // Items per second
    private readonly Timer rateLimitTimer;

    public event EventHandler<ProducerEventArgs<T>> ItemProduced;
    public event EventHandler<Exception> ProductionError;

    public BackpressureProducer(int capacity = 1000, int initialRate = 100, ILogger logger = null)
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
        rateLimitSemaphore = new SemaphoreSlim(currentRate, currentRate);

        // Timer to refill rate limit tokens
        rateLimitTimer = new Timer(RefillTokens, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public bool IsCompleted => isCompleted;
    public ChannelReader<T> Reader => channel.Reader;
    public int CurrentRate => currentRate;
    public int QueueSize => channel.Reader.CanCount ? channel.Reader.Count : -1;

    public async Task ProduceAsync(T item, CancellationToken cancellationToken = default)
    {
        if (isDisposed || isCompleted) return;

        // Apply rate limiting
        await rateLimitSemaphore.WaitAsync(cancellationToken);

        try
        {
            var queueSizeBefore = QueueSize;
            
            await channel.Writer.WriteAsync(item, cancellationToken);
            
            var queueSizeAfter = QueueSize;
            
            // Adaptive rate adjustment based on queue pressure
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

    private void RefillTokens(object state)
    {
        if (isDisposed) return;

        try
        {
            // Calculate how many tokens to add based on current rate
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
        const int highPressureThreshold = 800; // 80% of typical capacity
        const int lowPressureThreshold = 200;  // 20% of typical capacity
        
        if (queueSize > highPressureThreshold && currentRate > 10)
        {
            // High backpressure, reduce rate
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
            // Low backpressure, increase rate
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

// Streaming data processor with windowing and aggregation
public class StreamProcessor<T> : IDisposable
{
    private readonly Channel<T> inputChannel;
    private readonly TimeSpan windowSize;
    private readonly TimeSpan slideInterval;
    private readonly Func<IEnumerable<T>, object> aggregateFunction;
    private readonly ILogger logger;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly Task processingTask;
    private volatile bool isDisposed = false;

    public event EventHandler<WindowProcessedEventArgs<T>> WindowProcessed;

    public StreamProcessor(
        TimeSpan windowSize,
        TimeSpan slideInterval,
        Func<IEnumerable<T>, object> aggregateFunction,
        int capacity = 10000,
        ILogger logger = null)
    {
        this.windowSize = windowSize;
        this.slideInterval = slideInterval;
        this.aggregateFunction = aggregateFunction ?? throw new ArgumentNullException(nameof(aggregateFunction));
        this.logger = logger;

        var options = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        };

        inputChannel = Channel.CreateUnbounded<T>(options);
        cancellationTokenSource = new CancellationTokenSource();

        processingTask = ProcessStreamAsync(cancellationTokenSource.Token);
    }

    public async Task AddAsync(T item, CancellationToken cancellationToken = default)
    {
        if (isDisposed) return;
        
        await inputChannel.Writer.WriteAsync(item, cancellationToken);
    }

    public void Complete()
    {
        if (!isDisposed)
        {
            inputChannel.Writer.Complete();
        }
    }

    private async Task ProcessStreamAsync(CancellationToken cancellationToken)
    {
        try
        {
            var window = new List<(T Item, DateTime Timestamp)>();
            var lastSlide = DateTime.UtcNow;

            await foreach (var item in inputChannel.Reader.ReadAllAsync(cancellationToken))
            {
                var now = DateTime.UtcNow;
                window.Add((item, now));

                // Remove items outside the window
                var windowStart = now - windowSize;
                window.RemoveAll(x => x.Timestamp < windowStart);

                // Check if it's time to slide the window
                if (now - lastSlide >= slideInterval)
                {
                    if (window.Count > 0)
                    {
                        var windowItems = window.Select(x => x.Item).ToList();
                        var aggregateResult = aggregateFunction(windowItems);

                        var eventArgs = new WindowProcessedEventArgs<T>(
                            windowItems,
                            windowStart,
                            now,
                            aggregateResult);

                        WindowProcessed?.Invoke(this, eventArgs);

                        logger?.LogDebug("Processed window with {ItemCount} items. Aggregate: {Aggregate}",
                            windowItems.Count, aggregateResult);
                    }

                    lastSlide = now;
                }
            }

            // Process final window if there are remaining items
            if (window.Count > 0)
            {
                var finalWindowItems = window.Select(x => x.Item).ToList();
                var finalAggregate = aggregateFunction(finalWindowItems);
                
                var finalEventArgs = new WindowProcessedEventArgs<T>(
                    finalWindowItems,
                    DateTime.UtcNow - windowSize,
                    DateTime.UtcNow,
                    finalAggregate);

                WindowProcessed?.Invoke(this, finalEventArgs);
                
                logger?.LogInformation("Processed final window with {ItemCount} items", finalWindowItems.Count);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error in stream processing loop");
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            
            Complete();
            cancellationTokenSource.Cancel();
            
            try
            {
                processingTask.Wait(TimeSpan.FromSeconds(10));
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Error waiting for stream processing task to complete");
            }
            
            cancellationTokenSource?.Dispose();
        }
    }
}

public class WindowProcessedEventArgs<T> : EventArgs
{
    public IReadOnlyList<T> Items { get; }
    public DateTime WindowStart { get; }
    public DateTime WindowEnd { get; }
    public object AggregateResult { get; }

    public WindowProcessedEventArgs(IReadOnlyList<T> items, DateTime windowStart, DateTime windowEnd, object aggregateResult)
    {
        Items = items;
        WindowStart = windowStart;
        WindowEnd = windowEnd;
        AggregateResult = aggregateResult;
    }
}

// Performance metrics for producer-consumer scenarios
public class ProducerConsumerMetrics
{
    private volatile long itemsProduced = 0;
    private volatile long itemsConsumed = 0;
    private volatile long totalProducingTime = 0;
    private volatile long totalConsumingTime = 0;
    private volatile long peakQueueSize = 0;
    private readonly object lockObject = new object();
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
        
        // Update peak queue size
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
```

**Usage**:

```csharp
// Example 1: Basic Producer-Consumer with Channels
Console.WriteLine("Basic Producer-Consumer Examples:");

var channel = Channel.CreateBounded<string>(100);
var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("Example");

var producer = new ChannelProducer<string>(channel.Writer, logger);
var consumer = new ChannelConsumer<string>(channel.Reader, logger);
var metrics = new ProducerConsumerMetrics();

// Subscribe to events for metrics
producer.ItemProduced += (sender, args) => 
    metrics.RecordItemProduced(TimeSpan.FromMilliseconds(1), args.QueueSize);

consumer.ItemConsumed += (sender, args) => 
    metrics.RecordItemConsumed(args.ProcessingTime);

// Start producer task
var producerTask = Task.Run(async () =>
{
    for (int i = 1; i <= 1000; i++)
    {
        await producer.ProduceAsync($"Message-{i}");
        
        if (i % 100 == 0)
        {
            Console.WriteLine($"Produced {i} messages");
        }
        
        await Task.Delay(1); // Simulate production work
    }
    
    await producer.CompleteAsync();
});

// Start consumer task
var consumerTask = Task.Run(async () =>
{
    var consumedCount = 0;
    
    await foreach (var message in consumer.ConsumeAllAsync())
    {
        consumedCount++;
        
        // Simulate processing work
        await Task.Delay(2);
        
        if (consumedCount % 100 == 0)
        {
            Console.WriteLine($"Consumed {consumedCount} messages");
        }
    }
    
    Console.WriteLine($"Total consumed: {consumedCount}");
});

await Task.WhenAll(producerTask, consumerTask);

var stats = metrics.GetStats();
Console.WriteLine($"Throughput - Producing: {stats.ProducingThroughput:F0}/sec, " +
                 $"Consuming: {stats.ConsumingThroughput:F0}/sec");
Console.WriteLine($"Peak queue size: {stats.PeakQueueSize}");

// Example 2: Priority Producer-Consumer
Console.WriteLine("\nPriority Producer-Consumer Examples:");

var priorities = new[] { 1, 2, 3, 4, 5 }; // 5 is highest priority
var prioritySystem = new PriorityProducerConsumer<string>(priorities, 200, logger);

// Producer tasks with different priorities
var priorityProducerTasks = priorities.Select(priority =>
    Task.Run(async () =>
    {
        for (int i = 1; i <= 50; i++)
        {
            var message = $"Priority-{priority}-Message-{i}";
            await prioritySystem.ProduceAsync(message, priority);
            
            // Higher priority items produced less frequently
            await Task.Delay(priority * 10);
        }
    })
).ToArray();

// Consumer task
var priorityConsumerTask = Task.Run(async () =>
{
    var consumedByPriority = new Dictionary<int, int>();
    var totalConsumed = 0;
    
    await foreach (var (message, priority) in prioritySystem.ConsumeAllAsync())
    {
        consumedByPriority[priority] = consumedByPriority.GetValueOrDefault(priority) + 1;
        totalConsumed++;
        
        if (totalConsumed % 25 == 0)
        {
            Console.WriteLine($"Consumed {totalConsumed} messages. Last: {message}");
        }
        
        // Stop when all producers are done and no items remain
        if (priorityProducerTasks.All(t => t.IsCompleted) && 
            priorities.All(p => prioritySystem.GetQueueCount(p) == 0))
        {
            break;
        }
    }
    
    Console.WriteLine("Priority consumption distribution:");
    foreach (var kvp in consumedByPriority.OrderByDescending(x => x.Key))
    {
        Console.WriteLine($"  Priority {kvp.Key}: {kvp.Value} items");
    }
});

await Task.WhenAll(priorityProducerTasks);
prioritySystem.CompleteProduction();
await priorityConsumerTask;

// Example 3: Batch Processing Pipeline
Console.WriteLine("\nBatch Processing Examples:");

// Batch processor that simulates data transformation
var batchProcessor = new BatchProcessor<int, string>(
    async (batch, cancellationToken) =>
    {
        // Simulate batch processing work
        await Task.Delay(50, cancellationToken);
        
        return batch.Select(x => $"Processed-{x}-{DateTime.UtcNow.Ticks}");
    },
    batchSize: 10,
    batchTimeout: TimeSpan.FromMilliseconds(200),
    logger: logger
);

// Process stream of data
var streamData = Enumerable.Range(1, 250);
var batchResults = new List<string>();

var batchTask = Task.Run(async () =>
{
    await foreach (var result in batchProcessor.ProcessStreamAsync(streamData.ToAsyncEnumerable()))
    {
        batchResults.Add(result);
        
        if (batchResults.Count % 50 == 0)
        {
            Console.WriteLine($"Batch processed {batchResults.Count} results");
        }
    }
});

await batchTask;

Console.WriteLine($"Batch processing completed. Total results: {batchResults.Count}");

// Example 4: Backpressure-Aware Producer
Console.WriteLine("\nBackpressure Producer Examples:");

var backpressureProducer = new BackpressureProducer<int>(capacity: 500, initialRate: 200, logger: logger);
var backpressureMetrics = new ProducerConsumerMetrics();

// Fast producer
var fastProducerTask = Task.Run(async () =>
{
    for (int i = 1; i <= 2000; i++)
    {
        var stopwatch = Stopwatch.StartNew();
        await backpressureProducer.ProduceAsync(i);
        stopwatch.Stop();
        
        backpressureMetrics.RecordItemProduced(stopwatch.Elapsed, backpressureProducer.QueueSize);
        
        if (i % 200 == 0)
        {
            Console.WriteLine($"Produced {i} items. Rate: {backpressureProducer.CurrentRate}/sec, " +
                            $"Queue: {backpressureProducer.QueueSize}");
        }
    }
    
    await backpressureProducer.CompleteAsync();
});

// Slow consumer
var slowConsumerTask = Task.Run(async () =>
{
    var consumedCount = 0;
    
    await foreach (var item in backpressureProducer.Reader.ReadAllAsync())
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Simulate slow processing
        await Task.Delay(Random.Shared.Next(1, 10));
        
        stopwatch.Stop();
        consumedCount++;
        
        backpressureMetrics.RecordItemConsumed(stopwatch.Elapsed);
        
        if (consumedCount % 200 == 0)
        {
            Console.WriteLine($"Consumed {consumedCount} items");
        }
    }
    
    Console.WriteLine($"Backpressure consumer finished: {consumedCount} items");
});

await Task.WhenAll(fastProducerTask, slowConsumerTask);

var backpressureStats = backpressureMetrics.GetStats();
Console.WriteLine($"Backpressure Results - Efficiency: {backpressureStats.Efficiency:P2}, " +
                 $"Peak Queue: {backpressureStats.PeakQueueSize}");

// Example 5: Stream Processing with Windowing
Console.WriteLine("\nStream Processing Examples:");

var streamProcessor = new StreamProcessor<double>(
    windowSize: TimeSpan.FromSeconds(2),
    slideInterval: TimeSpan.FromSeconds(1),
    aggregateFunction: values => new
    {
        Count = values.Count(),
        Average = values.Any() ? values.Average() : 0,
        Min = values.Any() ? values.Min() : 0,
        Max = values.Any() ? values.Max() : 0
    },
    logger: logger
);

var windowResults = new List<object>();

streamProcessor.WindowProcessed += (sender, args) =>
{
    windowResults.Add(args.AggregateResult);
    Console.WriteLine($"Window [{args.WindowStart:HH:mm:ss.fff} - {args.WindowEnd:HH:mm:ss.fff}]: " +
                     $"{args.Items.Count} items, Aggregate: {args.AggregateResult}");
};

// Generate stream data
var streamTask = Task.Run(async () =>
{
    var random = new Random();
    
    for (int i = 0; i < 100; i++)
    {
        var value = random.NextDouble() * 100;
        await streamProcessor.AddAsync(value);
        
        // Variable rate to create interesting windows
        await Task.Delay(Random.Shared.Next(50, 150));
    }
    
    streamProcessor.Complete();
});

await streamTask;

// Wait for final processing
await Task.Delay(3000);

Console.WriteLine($"Stream processing completed. Processed {windowResults.Count} windows");

// Example 6: Multi-Producer Multi-Consumer Scenario
Console.WriteLine("\nMulti-Producer Multi-Consumer Examples:");

var multiChannel = Channel.CreateBounded<WorkItem>(1000);
var workItemMetrics = new ProducerConsumerMetrics();

// Multiple producers
var producerTasks = Enumerable.Range(1, 3).Select(producerId =>
    Task.Run(async () =>
    {
        var producer = new ChannelProducer<WorkItem>(multiChannel.Writer, logger);
        
        for (int i = 1; i <= 100; i++)
        {
            var workItem = new WorkItem($"Producer-{producerId}-Work-{i}", 
                TimeSpan.FromMilliseconds(Random.Shared.Next(10, 100)));
            
            var stopwatch = Stopwatch.StartNew();
            await producer.ProduceAsync(workItem);
            stopwatch.Stop();
            
            workItemMetrics.RecordItemProduced(stopwatch.Elapsed);
            
            await Task.Delay(Random.Shared.Next(5, 25));
        }
    })
).ToArray();

// Multiple consumers
var consumerTasks = Enumerable.Range(1, 2).Select(consumerId =>
    Task.Run(async () =>
    {
        var consumer = new ChannelConsumer<WorkItem>(multiChannel.Reader, logger);
        var processedCount = 0;
        
        try
        {
            while (true)
            {
                var workItem = await consumer.ConsumeAsync();
                var stopwatch = Stopwatch.StartNew();
                
                // Simulate work processing
                await Task.Delay(workItem.ProcessingTime);
                
                stopwatch.Stop();
                processedCount++;
                
                workItemMetrics.RecordItemConsumed(stopwatch.Elapsed);
                
                if (processedCount % 50 == 0)
                {
                    Console.WriteLine($"Consumer-{consumerId} processed {processedCount} items");
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Channel completed
            Console.WriteLine($"Consumer-{consumerId} finished with {processedCount} items");
        }
    })
).ToArray();

// Wait for all producers to complete
await Task.WhenAll(producerTasks);
multiChannel.Writer.Complete();

// Wait for all consumers to complete
await Task.WhenAll(consumerTasks);

var multiStats = workItemMetrics.GetStats();
Console.WriteLine($"Multi-producer/consumer results:");
Console.WriteLine($"  Items produced: {multiStats.ItemsProduced}");
Console.WriteLine($"  Items consumed: {multiStats.ItemsConsumed}");
Console.WriteLine($"  Producing throughput: {multiStats.ProducingThroughput:F1}/sec");
Console.WriteLine($"  Consuming throughput: {multiStats.ConsumingThroughput:F1}/sec");
Console.WriteLine($"  System efficiency: {multiStats.Efficiency:P2}");

// Example 7: Performance Comparison
Console.WriteLine("\nPerformance Comparison Examples:");

const int itemCount = 10000;

// Test different queue capacities
var capacities = new[] { 10, 100, 1000, 10000 };

foreach (var capacity in capacities)
{
    var testChannel = Channel.CreateBounded<int>(capacity);
    var testMetrics = new ProducerConsumerMetrics();
    
    var testStopwatch = Stopwatch.StartNew();
    
    var testProducer = Task.Run(async () =>
    {
        for (int i = 0; i < itemCount; i++)
        {
            var itemStopwatch = Stopwatch.StartNew();
            await testChannel.Writer.WriteAsync(i);
            itemStopwatch.Stop();
            
            testMetrics.RecordItemProduced(itemStopwatch.Elapsed, 
                testChannel.Reader.CanCount ? testChannel.Reader.Count : 0);
        }
        testChannel.Writer.Complete();
    });
    
    var testConsumer = Task.Run(async () =>
    {
        await foreach (var item in testChannel.Reader.ReadAllAsync())
        {
            var itemStopwatch = Stopwatch.StartNew();
            // Minimal processing
            itemStopwatch.Stop();
            
            testMetrics.RecordItemConsumed(itemStopwatch.Elapsed);
        }
    });
    
    await Task.WhenAll(testProducer, testConsumer);
    testStopwatch.Stop();
    
    var testStats = testMetrics.GetStats();
    Console.WriteLine($"Capacity {capacity,5}: {testStopwatch.ElapsedMilliseconds,4}ms total, " +
                     $"{testStats.ProducingThroughput,8:F0}/sec producing, " +
                     $"peak queue: {testStats.PeakQueueSize,5}");
}

// Cleanup
producer?.Dispose();
consumer?.Dispose();
prioritySystem?.Dispose();
batchProcessor?.Dispose();
backpressureProducer?.Dispose();
streamProcessor?.Dispose();

Console.WriteLine("\nProducer-Consumer examples completed!");

// Work item class for examples
public record WorkItem(string Id, TimeSpan ProcessingTime);
```

**Notes**:

- Implement comprehensive producer-consumer patterns with channels, bounded queues, and backpressure handling
- Support multiple producers and consumers with thread-safe coordination and load balancing
- Use priority queues for processing items based on importance or urgency levels
- Implement batch processing for efficient bulk operations and reduced overhead
- Support adaptive rate limiting and backpressure control to prevent memory exhaustion
- Provide stream processing with windowing capabilities for real-time data analysis
- Use comprehensive performance monitoring and metrics collection for operational visibility
- Support both bounded and unbounded channels depending on memory and latency requirements
- Implement proper disposal patterns and resource cleanup for long-running applications
- Support async enumerable patterns for streaming data processing and transformation
- Use configurable timeouts and cancellation tokens for robust error handling
- Provide extensive performance comparison examples for different configurations and use cases

**Prerequisites**:

- Understanding of producer-consumer patterns and concurrent programming concepts
- Knowledge of System.Threading.Channels and async/await programming
- Familiarity with backpressure handling and flow control mechanisms
- Experience with streaming data processing and windowing techniques
- Understanding of performance optimization and resource management in high-throughput scenarios

**Related Snippets**:

- [Concurrent Collections](concurrent-collections.md) - Thread-safe collections and lock-free data structures
- [Actor Model](actor-model.md) - Actor-based concurrency patterns with message-passing isolation
- [Async Enumerable](async-enumerable.md) - Streaming data processing with IAsyncEnumerable patterns
