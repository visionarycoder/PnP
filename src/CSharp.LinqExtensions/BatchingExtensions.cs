using System.Collections;

namespace CSharp.LinqExtensions;

/// <summary>
/// Extensions for batching and chunking operations on IEnumerable sequences.
/// </summary>
public static class BatchingExtensions
{
    /// <summary>
    /// Splits a sequence into batches of the specified size.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The source sequence to batch.</param>
    /// <param name="batchSize">The size of each batch.</param>
    /// <returns>An enumerable of arrays, each containing a batch of elements.</returns>
    public static IEnumerable<T[]> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));

        return BatchIterator(source, batchSize);
    }

    private static IEnumerable<T[]> BatchIterator<T>(IEnumerable<T> source, int batchSize)
    {
        using var enumerator = source.GetEnumerator();
        
        while (enumerator.MoveNext())
        {
            var batch = new List<T>(batchSize) { enumerator.Current };
            
            for (int i = 1; i < batchSize && enumerator.MoveNext(); i++)
            {
                batch.Add(enumerator.Current);
            }
            
            yield return batch.ToArray();
        }
    }

    /// <summary>
    /// Splits a sequence into chunks with optional overlap.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The source sequence to chunk.</param>
    /// <param name="size">The size of each chunk.</param>
    /// <param name="overlap">The number of elements to overlap between chunks.</param>
    /// <returns>An enumerable of arrays, each containing a chunk of elements.</returns>
    public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int size, int overlap = 0)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        if (overlap < 0) throw new ArgumentOutOfRangeException(nameof(overlap));

        return ChunkIterator(source, size, overlap);
    }

    private static IEnumerable<T[]> ChunkIterator<T>(IEnumerable<T> source, int size, int overlap)
    {
        var buffer = new Queue<T>();
        
        foreach (var item in source)
        {
            buffer.Enqueue(item);
            
            if (buffer.Count == size)
            {
                yield return buffer.ToArray();
                
                // Remove non-overlapping elements
                for (int i = 0; i < size - overlap; i++)
                {
                    if (buffer.Count > 0)
                        buffer.Dequeue();
                }
            }
        }
        
        // Yield remaining elements if any
        if (buffer.Count > 0)
        {
            yield return buffer.ToArray();
        }
    }

    /// <summary>
    /// Splits a sequence at predicate boundaries.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The source sequence to split.</param>
    /// <param name="predicate">The predicate that determines split points.</param>
    /// <param name="includeDelimiter">Whether to include the delimiter in the result.</param>
    /// <returns>An enumerable of subsequences split at the predicate boundaries.</returns>
    public static IEnumerable<IEnumerable<T>> SplitAt<T>(
        this IEnumerable<T> source, 
        Func<T, bool> predicate,
        bool includeDelimiter = false)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        return SplitAtIterator(source, predicate, includeDelimiter);
    }

    private static IEnumerable<IEnumerable<T>> SplitAtIterator<T>(
        IEnumerable<T> source, 
        Func<T, bool> predicate,
        bool includeDelimiter)
    {
        var current = new List<T>();
        
        foreach (var item in source)
        {
            if (predicate(item))
            {
                if (includeDelimiter)
                    current.Add(item);
                
                if (current.Any())
                {
                    yield return current;
                    current = new List<T>();
                }
            }
            else
            {
                current.Add(item);
            }
        }
        
        if (current.Any())
        {
            yield return current;
        }
    }
}