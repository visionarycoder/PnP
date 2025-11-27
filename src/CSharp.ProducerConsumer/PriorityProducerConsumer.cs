using System.Runtime.CompilerServices;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;

namespace CSharp.ProducerConsumer;

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