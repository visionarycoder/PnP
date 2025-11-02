namespace CSharp.LinqExtensions;

/// <summary>
/// Advanced distinct operations for IEnumerable sequences.
/// </summary>
public static class DistinctExtensions
{
    /// <summary>
    /// Returns distinct elements based on a key selector function.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="keySelector">Function to extract the key from each element.</param>
    /// <param name="comparer">Optional equality comparer for keys.</param>
    /// <returns>A sequence of distinct elements based on the key selector.</returns>
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        return DistinctByIterator(source, keySelector, comparer ?? EqualityComparer<TKey>.Default);
    }

    private static IEnumerable<T> DistinctByIterator<T, TKey>(
        IEnumerable<T> source,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey> comparer)
    {
        var seenKeys = new HashSet<TKey>(comparer);
        
        foreach (var item in source)
        {
            var key = keySelector(item);
            if (seenKeys.Add(key))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Returns distinct elements, keeping the last occurrence of duplicates.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="comparer">Optional equality comparer for elements.</param>
    /// <returns>A sequence with distinct elements, keeping last occurrences.</returns>
    public static IEnumerable<T> DistinctLast<T>(
        this IEnumerable<T> source,
        IEqualityComparer<T>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        comparer ??= EqualityComparer<T>.Default;
        
        var items = source.ToList();
        var seen = new HashSet<T>(comparer);
        
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (seen.Add(items[i]))
            {
                yield return items[i];
            }
        }
    }

    /// <summary>
    /// Returns only the duplicate elements from the sequence.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="comparer">Optional equality comparer for elements.</param>
    /// <returns>A sequence containing only duplicate elements.</returns>
    public static IEnumerable<T> Duplicates<T>(
        this IEnumerable<T> source,
        IEqualityComparer<T>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        comparer ??= EqualityComparer<T>.Default;
        
        var seen = new HashSet<T>(comparer);
        var duplicates = new HashSet<T>(comparer);
        
        foreach (var item in source)
        {
            if (!seen.Add(item) && duplicates.Add(item))
            {
                yield return item;
            }
        }
    }
}