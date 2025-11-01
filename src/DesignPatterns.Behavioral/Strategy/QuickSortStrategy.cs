namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Quick sort implementation - efficient divide-and-conquer algorithm
/// </summary>
public class QuickSortStrategy<T> : ISortingStrategy<T> where T : IComparable<T>
{
    public string Name => "Quick Sort";
    public SortingComplexity TimeComplexity => new("O(n log n)", "O(n log n)", "O(n²)");
    public SortingComplexity SpaceComplexity => new("O(log n)", "O(log n)", "O(n)");
    public bool IsStable => false;

    public void Sort(T[] array)
    {
        QuickSort(array, 0, array.Length - 1);
    }

    private void QuickSort(T[] array, int low, int high)
    {
        if (low < high)
        {
            int pivotIndex = Partition(array, low, high);
            QuickSort(array, low, pivotIndex - 1);
            QuickSort(array, pivotIndex + 1, high);
        }
    }

    private int Partition(T[] array, int low, int high)
    {
        T pivot = array[high];
        int i = low - 1;

        for (int j = low; j < high; j++)
        {
            if (array[j].CompareTo(pivot) <= 0)
            {
                i++;
                (array[i], array[j]) = (array[j], array[i]);
            }
        }

        (array[i + 1], array[high]) = (array[high], array[i + 1]);
        return i + 1;
    }
}