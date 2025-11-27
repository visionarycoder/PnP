using System.Numerics;
using System.Runtime.InteropServices;

namespace CSharp.SpanOperations;

/// <summary>
/// Span-based numerical operations with vectorization support
/// </summary>
public static class SpanNumerics
{
    /// <summary>
    /// Sum array using span (vectorized when possible)
    /// </summary>
    public static int Sum(ReadOnlySpan<int> span)
    {
        if (Vector.IsHardwareAccelerated && span.Length >= Vector<int>.Count)
        {
            return SumVectorized(span);
        }

        int sum = 0;
        for (int i = 0; i < span.Length; i++)
        {
            sum += span[i];
        }
        return sum;
    }

    private static int SumVectorized(ReadOnlySpan<int> span)
    {
        var vectors = MemoryMarshal.Cast<int, Vector<int>>(span);
        var vectorSum = Vector<int>.Zero;

        for (int i = 0; i < vectors.Length; i++)
        {
            vectorSum += vectors[i];
        }

        int result = Vector.Dot(vectorSum, Vector<int>.One);

        // Handle remaining elements
        int remaining = span.Length % Vector<int>.Count;
        if (remaining > 0)
        {
            var remainingSpan = span.Slice(span.Length - remaining);
            for (int i = 0; i < remainingSpan.Length; i++)
            {
                result += remainingSpan[i];
            }
        }

        return result;
    }

    /// <summary>
    /// Find minimum value in span
    /// </summary>
    public static T Min<T>(ReadOnlySpan<T> span) where T : IComparable<T>
    {
        if (span.IsEmpty)
            throw new ArgumentException("Span cannot be empty");

        T min = span[0];
        for (int i = 1; i < span.Length; i++)
        {
            if (span[i].CompareTo(min) < 0)
                min = span[i];
        }
        return min;
    }

    /// <summary>
    /// Find maximum value in span
    /// </summary>
    public static T Max<T>(ReadOnlySpan<T> span) where T : IComparable<T>
    {
        if (span.IsEmpty)
            throw new ArgumentException("Span cannot be empty");

        T max = span[0];
        for (int i = 1; i < span.Length; i++)
        {
            if (span[i].CompareTo(max) > 0)
                max = span[i];
        }
        return max;
    }

    /// <summary>
    /// Calculate average for integer span
    /// </summary>
    public static double Average(ReadOnlySpan<int> span)
    {
        return span.IsEmpty ? 0.0 : (double)Sum(span) / span.Length;
    }

    /// <summary>
    /// Calculate average for double span
    /// </summary>
    public static double Average(ReadOnlySpan<double> span)
    {
        if (span.IsEmpty)
            return 0.0;

        double sum = 0.0;
        for (int i = 0; i < span.Length; i++)
        {
            sum += span[i];
        }
        return sum / span.Length;
    }

    /// <summary>
    /// Find index of minimum element
    /// </summary>
    public static int IndexOfMin<T>(ReadOnlySpan<T> span) where T : IComparable<T>
    {
        if (span.IsEmpty)
            return -1;

        int minIndex = 0;
        T min = span[0];

        for (int i = 1; i < span.Length; i++)
        {
            if (span[i].CompareTo(min) < 0)
            {
                min = span[i];
                minIndex = i;
            }
        }

        return minIndex;
    }

    /// <summary>
    /// Find index of maximum element
    /// </summary>
    public static int IndexOfMax<T>(ReadOnlySpan<T> span) where T : IComparable<T>
    {
        if (span.IsEmpty)
            return -1;

        int maxIndex = 0;
        T max = span[0];

        for (int i = 1; i < span.Length; i++)
        {
            if (span[i].CompareTo(max) > 0)
            {
                max = span[i];
                maxIndex = i;
            }
        }

        return maxIndex;
    }

    /// <summary>
    /// Calculate variance of double values
    /// </summary>
    public static double Variance(ReadOnlySpan<double> span)
    {
        if (span.Length < 2)
            return 0.0;

        double mean = Average(span);
        double sumSquaredDiffs = 0.0;

        for (int i = 0; i < span.Length; i++)
        {
            double diff = span[i] - mean;
            sumSquaredDiffs += diff * diff;
        }

        return sumSquaredDiffs / (span.Length - 1);
    }

    /// <summary>
    /// Calculate standard deviation of double values
    /// </summary>
    public static double StandardDeviation(ReadOnlySpan<double> span)
    {
        return Math.Sqrt(Variance(span));
    }
}