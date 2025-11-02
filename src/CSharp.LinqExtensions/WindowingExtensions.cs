namespace CSharp.LinqExtensions;

/// <summary>
/// Extensions for windowing and sliding operations on IEnumerable sequences.
/// </summary>
public static class WindowingExtensions
{
    /// <summary>
    /// Creates a sliding window of the specified size over the sequence.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="windowSize">The size of the sliding window.</param>
    /// <returns>An enumerable of arrays, each representing a window.</returns>
    public static IEnumerable<T[]> SlidingWindow<T>(this IEnumerable<T> source, int windowSize)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (windowSize <= 0) throw new ArgumentOutOfRangeException(nameof(windowSize));

        return SlidingWindowIterator(source, windowSize);
    }

    private static IEnumerable<T[]> SlidingWindowIterator<T>(IEnumerable<T> source, int windowSize)
    {
        var buffer = new Queue<T>();
        
        foreach (var item in source)
        {
            buffer.Enqueue(item);
            
            if (buffer.Count > windowSize)
            {
                buffer.Dequeue();
            }
            
            if (buffer.Count == windowSize)
            {
                yield return buffer.ToArray();
            }
        }
    }

    /// <summary>
    /// Creates pairs of consecutive elements (sliding window of size 2).
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>An enumerable of tuples containing consecutive element pairs.</returns>
    public static IEnumerable<(T Previous, T Current)> Pairwise<T>(this IEnumerable<T> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        return PairwiseIterator(source);
    }

    private static IEnumerable<(T Previous, T Current)> PairwiseIterator<T>(IEnumerable<T> source)
    {
        using var enumerator = source.GetEnumerator();
        
        if (!enumerator.MoveNext())
            yield break;
            
        var previous = enumerator.Current;
        
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            yield return (previous, current);
            previous = current;
        }
    }

    /// <summary>
    /// Groups consecutive elements that have the same key.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="keySelector">Function to extract the key from each element.</param>
    /// <param name="comparer">Optional equality comparer for keys.</param>
    /// <returns>An enumerable of groupings of consecutive elements with the same key.</returns>
    public static IEnumerable<IGrouping<TKey, T>> GroupConsecutive<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        comparer ??= EqualityComparer<TKey>.Default;
        return GroupConsecutiveIterator(source, keySelector, comparer);
    }

    private static IEnumerable<IGrouping<TKey, T>> GroupConsecutiveIterator<T, TKey>(
        IEnumerable<T> source,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey> comparer)
    {
        using var enumerator = source.GetEnumerator();
        
        if (!enumerator.MoveNext())
            yield break;

        var currentKey = keySelector(enumerator.Current);
        var currentGroup = new List<T> { enumerator.Current };

        while (enumerator.MoveNext())
        {
            var itemKey = keySelector(enumerator.Current);
            
            if (comparer.Equals(currentKey, itemKey))
            {
                currentGroup.Add(enumerator.Current);
            }
            else
            {
                yield return new Grouping<TKey, T>(currentKey, currentGroup);
                currentKey = itemKey;
                currentGroup = new List<T> { enumerator.Current };
            }
        }
        
        if (currentGroup.Any())
        {
            yield return new Grouping<TKey, T>(currentKey, currentGroup);
        }
    }
}