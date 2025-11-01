namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Merge sort implementation - stable and efficient
/// </summary>
public class MergeSortStrategy<T> : ISortingStrategy<T> where T : IComparable<T>
{
    public string Name => "Merge Sort";
    public SortingComplexity TimeComplexity => new("O(n log n)", "O(n log n)", "O(n log n)");
    public SortingComplexity SpaceComplexity => new("O(n)", "O(n)", "O(n)");
    public bool IsStable => true;

    public void Sort(T[] array)
    {
        if (array.Length <= 1) return;

        MergeSort(array, 0, array.Length - 1);
    }

    private void MergeSort(T[] array, int left, int right)
    {
        if (left < right)
        {
            int middle = left + (right - left) / 2;

            MergeSort(array, left, middle);
            MergeSort(array, middle + 1, right);
            Merge(array, left, middle, right);
        }
    }

    private void Merge(T[] array, int left, int middle, int right)
    {
        int n1 = middle - left + 1;
        int n2 = right - middle;

        T[] leftArray = new T[n1];
        T[] rightArray = new T[n2];

        Array.Copy(array, left, leftArray, 0, n1);
        Array.Copy(array, middle + 1, rightArray, 0, n2);

        int i = 0, j = 0, k = left;

        while (i < n1 && j < n2)
        {
            if (leftArray[i].CompareTo(rightArray[j]) <= 0)
            {
                array[k] = leftArray[i];
                i++;
            }
            else
            {
                array[k] = rightArray[j];
                j++;
            }

            k++;
        }

        while (i < n1)
        {
            array[k] = leftArray[i];
            i++;
            k++;
        }

        while (j < n2)
        {
            array[k] = rightArray[j];
            j++;
            k++;
        }
    }
}