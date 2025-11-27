namespace CSharp.LinqExtensions;

/// <summary>
/// Conditional and filtering extensions for IEnumerable sequences.
/// </summary>
public static class ConditionalExtensions
{
    /// <summary>
    /// Conditionally applies a transformation to the sequence.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="transform">The transformation to apply if condition is true.</param>
    /// <returns>The transformed sequence if condition is true, otherwise the original sequence.</returns>
    public static IEnumerable<T> WhereIf<T>(
        this IEnumerable<T> source,
        bool condition,
        Func<IEnumerable<T>, IEnumerable<T>> transform)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (transform == null) throw new ArgumentNullException(nameof(transform));

        return condition ? transform(source) : source;
    }

    /// <summary>
    /// Filters elements based on a condition, only if the condition parameter is true.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="condition">Whether to apply the filter.</param>
    /// <param name="predicate">The predicate to filter with.</param>
    /// <returns>The filtered sequence if condition is true, otherwise the original sequence.</returns>
    public static IEnumerable<T> WhereIf<T>(
        this IEnumerable<T> source,
        bool condition,
        Func<T, bool> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        return condition ? source.Where(predicate) : source;
    }

    /// <summary>
    /// Returns elements that are not null.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>A sequence with null elements filtered out.</returns>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        return source.Where(x => x != null)!;
    }

    /// <summary>
    /// Returns elements that are not null for nullable value types.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>A sequence with null elements filtered out and values unwrapped.</returns>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : struct
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        return source.Where(x => x.HasValue).Select(x => x!.Value);
    }
}