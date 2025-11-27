namespace CSharp.LinqExtensions;

/// <summary>
/// Aggregation and statistical extensions for IEnumerable sequences.
/// </summary>
public static class AggregationExtensions
{
    /// <summary>
    /// Calculates the median value of a sequence.
    /// </summary>
    /// <param name="source">The source sequence of numeric values.</param>
    /// <returns>The median value.</returns>
    public static double Median(this IEnumerable<double> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var sorted = source.OrderBy(x => x).ToList();
        if (sorted.Count == 0)
            throw new InvalidOperationException("Sequence contains no elements");

        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }

    /// <summary>
    /// Calculates the standard deviation of a sequence.
    /// </summary>
    /// <param name="source">The source sequence of numeric values.</param>
    /// <returns>The standard deviation.</returns>
    public static double StandardDeviation(this IEnumerable<double> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var values = source.ToList();
        if (values.Count == 0)
            throw new InvalidOperationException("Sequence contains no elements");

        double mean = values.Average();
        double sumOfSquares = values.Sum(x => Math.Pow(x - mean, 2));
        return Math.Sqrt(sumOfSquares / values.Count);
    }

    /// <summary>
    /// Finds the mode (most frequent value) in a sequence.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>The most frequent value, or default if sequence is empty.</returns>
    public static T Mode<T>(this IEnumerable<T> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var mostFrequent = source
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
            
        return mostFrequent != null ? mostFrequent.Key : default(T)!;
    }
}