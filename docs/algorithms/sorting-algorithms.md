# Sorting Algorithms

**Description**: Implementation of common sorting algorithms with performance analysis and use cases.
**Language/Technology**: C#, Algorithms
**Performance Complexity**: Various O(n) to O(n²) depending on algorithm

## Quick Sort

**Code**:

```csharp
using System;

public static class QuickSort
{
    public static void Sort<T>(T[] array, int low, int high, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        
        if (low < high)
        {
            int partitionIndex = Partition(array, low, high, comparer);
            Sort(array, low, partitionIndex - 1, comparer);
            Sort(array, partitionIndex + 1, high, comparer);
        }
    }
    
    private static int Partition<T>(T[] array, int low, int high, IComparer<T> comparer)
        where T : IComparable<T>
    {
        T pivot = array[high];
        int i = low - 1;
        
        for (int j = low; j < high; j++)
        {
            if (comparer.Compare(array[j], pivot) <= 0)
            {
                i++;
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
        
        (array[i + 1], array[high]) = (array[high], array[i + 1]);
        return i + 1;
    }
}
```

**Usage**:

```csharp
int[] numbers = { 64, 34, 25, 12, 22, 11, 90 };
QuickSort.Sort(numbers, 0, numbers.Length - 1);
Console.WriteLine(string.Join(", ", numbers)); // Output: 11, 12, 22, 25, 34, 64, 90
```

## Merge Sort

**Code**:

```csharp
public static class MergeSort
{
    public static void Sort<T>(T[] array, IComparer<T> comparer = null) 
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        MergeSortRecursive(array, 0, array.Length - 1, comparer);
    }
    
    private static void MergeSortRecursive<T>(T[] array, int left, int right, IComparer<T> comparer)
        where T : IComparable<T>
    {
        if (left < right)
        {
            int middle = left + (right - left) / 2;
            MergeSortRecursive(array, left, middle, comparer);
            MergeSortRecursive(array, middle + 1, right, comparer);
            Merge(array, left, middle, right, comparer);
        }
    }
    
    private static void Merge<T>(T[] array, int left, int middle, int right, IComparer<T> comparer)
        where T : IComparable<T>
    {
        T[] leftArray = new T[middle - left + 1];
        T[] rightArray = new T[right - middle];
        
        Array.Copy(array, left, leftArray, 0, leftArray.Length);
        Array.Copy(array, middle + 1, rightArray, 0, rightArray.Length);
        
        int i = 0, j = 0, k = left;
        
        while (i < leftArray.Length && j < rightArray.Length)
        {
            if (comparer.Compare(leftArray[i], rightArray[j]) <= 0)
                array[k++] = leftArray[i++];
            else
                array[k++] = rightArray[j++];
        }
        
        while (i < leftArray.Length)
            array[k++] = leftArray[i++];
        while (j < rightArray.Length)
            array[k++] = rightArray[j++];
    }
}
```

**Usage**:

```csharp
string[] words = { "banana", "apple", "orange", "grape" };
MergeSort.Sort(words);
Console.WriteLine(string.Join(", ", words)); // Output: apple, banana, grape, orange
```

## Heap Sort

**Code**:

```csharp
public static class HeapSort
{
    public static void Sort<T>(T[] array, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        int n = array.Length;
        
        // Build max heap
        for (int i = n / 2 - 1; i >= 0; i--)
            Heapify(array, n, i, comparer);
        
        // Extract elements from heap one by one
        for (int i = n - 1; i > 0; i--)
        {
            (array[0], array[i]) = (array[i], array[0]);
            Heapify(array, i, 0, comparer);
        }
    }
    
    private static void Heapify<T>(T[] array, int n, int i, IComparer<T> comparer)
        where T : IComparable<T>
    {
        int largest = i;
        int left = 2 * i + 1;
        int right = 2 * i + 2;
        
        if (left < n && comparer.Compare(array[left], array[largest]) > 0)
            largest = left;
            
        if (right < n && comparer.Compare(array[right], array[largest]) > 0)
            largest = right;
            
        if (largest != i)
        {
            (array[i], array[largest]) = (array[largest], array[i]);
            Heapify(array, n, largest, comparer);
        }
    }
}
```

**Notes**:

- **QuickSort**: Average O(n log n), worst-case O(n²), in-place sorting
- **MergeSort**: Guaranteed O(n log n), stable sorting, requires additional memory
- **HeapSort**: Guaranteed O(n log n), in-place sorting, not stable
- Use QuickSort for general purpose, MergeSort when stability is required, HeapSort when memory is constrained

**Performance Comparison**:

- Small arrays (< 10): Insertion sort often faster
- General purpose: QuickSort with good pivot selection
- Stable sorting required: MergeSort
- Memory constrained: HeapSort

**Related Snippets**:

- [Searching Algorithms](searching-algorithms.md)
- [Data Structures](data-structures.md)
