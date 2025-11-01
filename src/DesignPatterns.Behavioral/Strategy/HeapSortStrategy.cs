namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Heap sort implementation - in-place sorting with guaranteed O(n log n)
/// </summary>
public class HeapSortStrategy<T> : ISortingStrategy<T> where T : IComparable<T>
{
    public string Name => "Heap Sort";
    public SortingComplexity TimeComplexity => new("O(n log n)", "O(n log n)", "O(n log n)");
    public SortingComplexity SpaceComplexity => new("O(1)", "O(1)", "O(1)");
    public bool IsStable => false;

    public void Sort(T[] array)
    {
        int n = array.Length;

        // Build heap (rearrange array)
        for (int i = n / 2 - 1; i >= 0; i--)
            Heapify(array, n, i);

        // Extract elements from heap one by one
        for (int i = n - 1; i > 0; i--)
        {
            (array[0], array[i]) = (array[i], array[0]);
            Heapify(array, i, 0);
        }
    }

    private void Heapify(T[] array, int n, int i)
    {
        int largest = i;
        int left = 2 * i + 1;
        int right = 2 * i + 2;

        if (left < n && array[left].CompareTo(array[largest]) > 0)
            largest = left;

        if (right < n && array[right].CompareTo(array[largest]) > 0)
            largest = right;

        if (largest != i)
        {
            (array[i], array[largest]) = (array[largest], array[i]);
            Heapify(array, n, largest);
        }
    }
}