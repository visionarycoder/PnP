namespace CSharp.SpanOperations;

/// <summary>
/// Span-based searching and sorting algorithms
/// </summary>
public static class SpanAlgorithms
{
    /// <summary>
    /// Binary search on sorted span
    /// </summary>
    public static int BinarySearch<T>(ReadOnlySpan<T> span, T value) where T : IComparable<T>
    {
        int left = 0;
        int right = span.Length - 1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            int comparison = span[mid].CompareTo(value);

            if (comparison == 0)
                return mid;
            else if (comparison < 0)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return ~left; // Return bitwise complement for insertion point
    }

    /// <summary>
    /// Quick sort implementation for spans
    /// </summary>
    public static void QuickSort<T>(Span<T> span) where T : IComparable<T>
    {
        if (span.Length <= 1)
            return;

        QuickSortRecursive(span, 0, span.Length - 1);
    }

    private static void QuickSortRecursive<T>(Span<T> span, int low, int high) where T : IComparable<T>
    {
        if (low < high)
        {
            int pivotIndex = Partition(span, low, high);
            QuickSortRecursive(span, low, pivotIndex - 1);
            QuickSortRecursive(span, pivotIndex + 1, high);
        }
    }

    private static int Partition<T>(Span<T> span, int low, int high) where T : IComparable<T>
    {
        T pivot = span[high];
        int i = low - 1;

        for (int j = low; j < high; j++)
        {
            if (span[j].CompareTo(pivot) <= 0)
            {
                i++;
                (span[i], span[j]) = (span[j], span[i]);
            }
        }

        (span[i + 1], span[high]) = (span[high], span[i + 1]);
        return i + 1;
    }

    /// <summary>
    /// Insertion sort for small spans (more efficient for small arrays)
    /// </summary>
    public static void InsertionSort<T>(Span<T> span) where T : IComparable<T>
    {
        for (int i = 1; i < span.Length; i++)
        {
            T key = span[i];
            int j = i - 1;

            while (j >= 0 && span[j].CompareTo(key) > 0)
            {
                span[j + 1] = span[j];
                j--;
            }

            span[j + 1] = key;
        }
    }

    /// <summary>
    /// Hybrid sort that chooses algorithm based on size
    /// </summary>
    public static void HybridSort<T>(Span<T> span) where T : IComparable<T>
    {
        if (span.Length <= 16)
        {
            InsertionSort(span);
        }
        else
        {
            QuickSort(span);
        }
    }

    /// <summary>
    /// Find all indices where predicate is true
    /// </summary>
    public static void FindIndices<T>(ReadOnlySpan<T> span, Predicate<T> predicate, Span<int> indices, out int count)
    {
        count = 0;
        for (int i = 0; i < span.Length && count < indices.Length; i++)
        {
            if (predicate(span[i]))
            {
                indices[count++] = i;
            }
        }
    }

    /// <summary>
    /// Count elements matching predicate
    /// </summary>
    public static int Count<T>(ReadOnlySpan<T> span, Predicate<T> predicate)
    {
        int count = 0;
        for (int i = 0; i < span.Length; i++)
        {
            if (predicate(span[i]))
                count++;
        }
        return count;
    }

    /// <summary>
    /// Check if any element matches predicate
    /// </summary>
    public static bool Any<T>(ReadOnlySpan<T> span, Predicate<T> predicate)
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (predicate(span[i]))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Check if all elements match predicate
    /// </summary>
    public static bool All<T>(ReadOnlySpan<T> span, Predicate<T> predicate)
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (!predicate(span[i]))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Remove duplicates from sorted span (in-place)
    /// </summary>
    public static int RemoveDuplicates<T>(Span<T> span) where T : IEquatable<T>
    {
        if (span.Length <= 1)
            return span.Length;

        int writeIndex = 1;
        
        for (int readIndex = 1; readIndex < span.Length; readIndex++)
        {
            if (!span[readIndex].Equals(span[readIndex - 1]))
            {
                if (writeIndex != readIndex)
                {
                    span[writeIndex] = span[readIndex];
                }
                writeIndex++;
            }
        }

        return writeIndex;
    }
}