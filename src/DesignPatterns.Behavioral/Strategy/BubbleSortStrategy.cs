namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Bubble sort implementation - simple but inefficient
/// </summary>
public class BubbleSortStrategy<T> : ISortingStrategy<T> where T : IComparable<T>
{
    public string Name => "Bubble Sort";
    public SortingComplexity TimeComplexity => new("O(n)", "O(n²)", "O(n²)");
    public SortingComplexity SpaceComplexity => new("O(1)", "O(1)", "O(1)");
    public bool IsStable => true;

    public void Sort(T[] array)
    {
        for (int i = 0; i < array.Length - 1; i++)
        {
            bool swapped = false;
            for (int j = 0; j < array.Length - i - 1; j++)
            {
                if (array[j].CompareTo(array[j + 1]) > 0)
                {
                    (array[j], array[j + 1]) = (array[j + 1], array[j]);
                    swapped = true;
                }
            }

            if (!swapped) break; // Optimization: early termination if already sorted
        }
    }
}