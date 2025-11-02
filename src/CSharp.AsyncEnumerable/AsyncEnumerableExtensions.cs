using System.Runtime.CompilerServices;

namespace CSharp.AsyncEnumerable;

// Extension methods for async enumerables
public static class AsyncEnumerableExtensions
{
    // Take first N items
    public static async IAsyncEnumerable<T> TakeAsync<T>(
        this IAsyncEnumerable<T> source, 
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (count <= 0) yield break;

        var taken = 0;
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            if (taken >= count) break;
            
            yield return item;
            taken++;
        }
    }

    // Skip first N items
    public static async IAsyncEnumerable<T> SkipAsync<T>(
        this IAsyncEnumerable<T> source, 
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var skipped = 0;
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            if (skipped < count)
            {
                skipped++;
                continue;
            }
            
            yield return item;
        }
    }

    // Where filter
    public static async IAsyncEnumerable<T> WhereAsync<T>(
        this IAsyncEnumerable<T> source, 
        Func<T, bool> predicate,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            if (predicate(item))
            {
                yield return item;
            }
        }
    }

    // Select transformation
    public static async IAsyncEnumerable<TResult> SelectAsync<T, TResult>(
        this IAsyncEnumerable<T> source, 
        Func<T, TResult> selector,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            yield return selector(item);
        }
    }

    // Async select transformation
    public static async IAsyncEnumerable<TResult> SelectAsync<T, TResult>(
        this IAsyncEnumerable<T> source, 
        Func<T, Task<TResult>> selector,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            var result = await selector(item);
            yield return result;
        }
    }

    // Buffer items into batches
    public static async IAsyncEnumerable<IList<T>> BufferAsync<T>(
        this IAsyncEnumerable<T> source, 
        int batchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0) throw new ArgumentException("Batch size must be positive", nameof(batchSize));

        var buffer = new List<T>(batchSize);

        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            buffer.Add(item);

            if (buffer.Count >= batchSize)
            {
                yield return buffer.ToList();
                buffer.Clear();
            }
        }

        // Yield remaining items
        if (buffer.Count > 0)
        {
            yield return buffer;
        }
    }

    // Convert to regular enumerable (materialize)
    public static async Task<List<T>> ToListAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        var list = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            list.Add(item);
        }
        return list;
    }

    // Count items
    public static async Task<int> CountAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        var count = 0;
        await foreach (var _ in source.WithCancellation(cancellationToken))
        {
            count++;
        }
        return count;
    }

    // Check if any items match predicate
    public static async Task<bool> AnyAsync<T>(
        this IAsyncEnumerable<T> source,
        Func<T, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            if (predicate(item))
            {
                return true;
            }
        }
        return false;
    }

    // Get first item or default
    public static async Task<T?> FirstOrDefaultAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            return item;
        }
        return default(T);
    }

    // Distinct items based on equality comparer
    public static async IAsyncEnumerable<T> DistinctAsync<T>(
        this IAsyncEnumerable<T> source,
        IEqualityComparer<T>? comparer = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var seen = new HashSet<T>(comparer);
        
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            if (seen.Add(item))
            {
                yield return item;
            }
        }
    }

    // Concatenate two async enumerables
    public static async IAsyncEnumerable<T> ConcatAsync<T>(
        this IAsyncEnumerable<T> first,
        IAsyncEnumerable<T> second,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in first.WithCancellation(cancellationToken))
        {
            yield return item;
        }
        
        await foreach (var item in second.WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    // Merge multiple async enumerables concurrently
    public static async IAsyncEnumerable<T> MergeAsync<T>(
        this IEnumerable<IAsyncEnumerable<T>> sources,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var tasks = sources.Select(async source =>
        {
            var items = new List<T>();
            await foreach (var item in source.WithCancellation(cancellationToken))
            {
                items.Add(item);
            }
            return items;
        }).ToArray();

        var results = await Task.WhenAll(tasks);
        
        foreach (var result in results)
        {
            foreach (var item in result)
            {
                yield return item;
            }
        }
    }

    // Throttle the async enumerable to limit rate
    public static async IAsyncEnumerable<T> ThrottleAsync<T>(
        this IAsyncEnumerable<T> source,
        TimeSpan interval,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var lastEmit = DateTime.MinValue;
        
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            var now = DateTime.UtcNow;
            var elapsed = now - lastEmit;
            
            if (elapsed < interval)
            {
                var delay = interval - elapsed;
                await Task.Delay(delay, cancellationToken);
            }
            
            lastEmit = DateTime.UtcNow;
            yield return item;
        }
    }

    // Retry failed operations
    public static async IAsyncEnumerable<TResult> RetryAsync<T, TResult>(
        this IAsyncEnumerable<T> source,
        Func<T, Task<TResult>> operation,
        int maxRetries = 3,
        TimeSpan delay = default,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            var retries = 0;
            while (retries <= maxRetries)
            {
                try
                {
                    var result = await operation(item);
                    yield return result;
                    break;
                }
                catch when (retries < maxRetries)
                {
                    retries++;
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }
        }
    }
}