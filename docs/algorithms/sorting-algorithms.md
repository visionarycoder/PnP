# Sorting Algorithms

**Description**: Implementation of common sorting algorithms with performance analysis and use cases.
**Language/Technology**: C#, Algorithms
**Performance Complexity**: Various O(n) to O(n²) depending on algorithm

## Quick Sort (Divide and Conquer Partitioning Sort)

**Description**: Quick Sort is a divide-and-conquer algorithm that works by selecting a 'pivot' element and partitioning the array around the pivot such that elements smaller than the pivot are on the left and larger elements are on the right.

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

## Merge Sort (Divide and Conquer Merge Sort)

**Description**: Merge Sort is a stable, divide-and-conquer algorithm that divides the array into halves, recursively sorts them, and then merges the sorted halves. It guarantees O(n log n) performance in all cases.

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

## Heap Sort (Binary Heap Sort)

**Description**: Heap Sort is a comparison-based sorting algorithm that uses a binary heap data structure. It first builds a max heap from the input data, then repeatedly extracts the maximum element and places it at the end of the sorted array.

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

## Insertion Sort (Incremental Sort)

**Description**: Insertion Sort builds the final sorted array one item at a time. It's efficient for small datasets and nearly sorted arrays. The algorithm works by taking elements from the unsorted portion and inserting them into their correct position in the sorted portion.

**Code**:

```csharp
public static class InsertionSort
{
    public static void Sort<T>(T[] array, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        
        for (int i = 1; i < array.Length; i++)
        {
            T key = array[i];
            int j = i - 1;
            
            // Move elements greater than key one position ahead
            while (j >= 0 && comparer.Compare(array[j], key) > 0)
            {
                array[j + 1] = array[j];
                j--;
            }
            
            array[j + 1] = key;
        }
    }
    
    // Binary insertion sort - uses binary search to find insertion position
    public static void BinaryInsertionSort<T>(T[] array, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        
        for (int i = 1; i < array.Length; i++)
        {
            T key = array[i];
            int left = 0;
            int right = i - 1;
            
            // Binary search for insertion position
            while (left <= right)
            {
                int mid = (left + right) / 2;
                if (comparer.Compare(key, array[mid]) < 0)
                    right = mid - 1;
                else
                    left = mid + 1;
            }
            
            // Shift elements to make space
            for (int j = i - 1; j >= left; j--)
                array[j + 1] = array[j];
            
            array[left] = key;
        }
    }
}
```

## Selection Sort (Minimum Selection Sort)

**Description**: Selection Sort divides the input into sorted and unsorted regions. It repeatedly selects the smallest element from the unsorted region and moves it to the end of the sorted region.

**Code**:

```csharp
public static class SelectionSort
{
    public static void Sort<T>(T[] array, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        
        for (int i = 0; i < array.Length - 1; i++)
        {
            int minIndex = i;
            
            // Find minimum element in remaining unsorted array
            for (int j = i + 1; j < array.Length; j++)
            {
                if (comparer.Compare(array[j], array[minIndex]) < 0)
                    minIndex = j;
            }
            
            // Swap minimum element with first element
            if (minIndex != i)
                (array[i], array[minIndex]) = (array[minIndex], array[i]);
        }
    }
}
```

## Bubble Sort (Exchange Sort)

**Description**: Bubble Sort repeatedly steps through the list, compares adjacent elements, and swaps them if they're in the wrong order. The pass through the list is repeated until the list is sorted.

**Code**:

```csharp
public static class BubbleSort
{
    public static void Sort<T>(T[] array, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        int n = array.Length;
        
        for (int i = 0; i < n - 1; i++)
        {
            bool swapped = false;
            
            // Last i elements are already sorted
            for (int j = 0; j < n - i - 1; j++)
            {
                if (comparer.Compare(array[j], array[j + 1]) > 0)
                {
                    (array[j], array[j + 1]) = (array[j + 1], array[j]);
                    swapped = true;
                }
            }
            
            // If no swapping occurred, array is sorted
            if (!swapped)
                break;
        }
    }
    
    // Cocktail shaker sort - bidirectional bubble sort
    public static void CocktailSort<T>(T[] array, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        bool swapped = true;
        int start = 0;
        int end = array.Length - 1;
        
        while (swapped)
        {
            swapped = false;
            
            // Forward pass
            for (int i = start; i < end; i++)
            {
                if (comparer.Compare(array[i], array[i + 1]) > 0)
                {
                    (array[i], array[i + 1]) = (array[i + 1], array[i]);
                    swapped = true;
                }
            }
            
            if (!swapped) break;
            
            end--;
            swapped = false;
            
            // Backward pass
            for (int i = end - 1; i >= start; i--)
            {
                if (comparer.Compare(array[i], array[i + 1]) > 0)
                {
                    (array[i], array[i + 1]) = (array[i + 1], array[i]);
                    swapped = true;
                }
            }
            
            start++;
        }
    }
}
```

## Shell Sort (Diminishing Increment Sort)

**Description**: Shell Sort is a generalization of insertion sort that allows the exchange of items that are far apart. It starts by sorting pairs of elements far apart from each other, then progressively reducing the gap.

**Code**:

```csharp
public static class ShellSort
{
    public static void Sort<T>(T[] array, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        int n = array.Length;
        
        // Start with a big gap, then reduce the gap
        for (int gap = n / 2; gap > 0; gap /= 2)
        {
            // Do a gapped insertion sort for this gap size
            for (int i = gap; i < n; i++)
            {
                T temp = array[i];
                int j;
                
                // Shift earlier gap-sorted elements up until the correct location for array[i] is found
                for (j = i; j >= gap && comparer.Compare(array[j - gap], temp) > 0; j -= gap)
                {
                    array[j] = array[j - gap];
                }
                
                array[j] = temp;
            }
        }
    }
    
    // Shell sort with Knuth's sequence (3k + 1)
    public static void SortWithKnuthSequence<T>(T[] array, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        int n = array.Length;
        
        // Generate Knuth sequence
        int gap = 1;
        while (gap < n / 3)
            gap = gap * 3 + 1;
        
        while (gap >= 1)
        {
            // Gap insertion sort
            for (int i = gap; i < n; i++)
            {
                T temp = array[i];
                int j = i;
                
                while (j >= gap && comparer.Compare(array[j - gap], temp) > 0)
                {
                    array[j] = array[j - gap];
                    j -= gap;
                }
                
                array[j] = temp;
            }
            
            gap /= 3;
        }
    }
}
```

## Counting Sort (Non-Comparison Integer Sort)

**Description**: Counting Sort is an integer sorting algorithm that operates by counting the number of objects that have each distinct key value. It only works for integers within a specific range.

**Code**:

```csharp
public static class CountingSort
{
    public static void Sort(int[] array, int maxValue = -1)
    {
        if (array.Length == 0) return;
        
        if (maxValue == -1)
            maxValue = array.Max();
        
        int[] count = new int[maxValue + 1];
        int[] output = new int[array.Length];
        
        // Count occurrences of each value
        for (int i = 0; i < array.Length; i++)
            count[array[i]]++;
        
        // Modify count array to store actual position
        for (int i = 1; i <= maxValue; i++)
            count[i] += count[i - 1];
        
        // Build output array in reverse order to maintain stability
        for (int i = array.Length - 1; i >= 0; i--)
        {
            output[count[array[i]] - 1] = array[i];
            count[array[i]]--;
        }
        
        // Copy output array to original array
        Array.Copy(output, array, array.Length);
    }
    
    // Counting sort for strings based on character at specific position
    public static void CountingSortStrings(string[] array, int position)
    {
        if (array.Length == 0) return;
        
        int[] count = new int[256]; // ASCII characters
        string[] output = new string[array.Length];
        
        // Count occurrences
        foreach (string str in array)
        {
            int charIndex = position < str.Length ? str[position] : 0;
            count[charIndex]++;
        }
        
        // Modify count array
        for (int i = 1; i < 256; i++)
            count[i] += count[i - 1];
        
        // Build output array
        for (int i = array.Length - 1; i >= 0; i--)
        {
            int charIndex = position < array[i].Length ? array[i][position] : 0;
            output[count[charIndex] - 1] = array[i];
            count[charIndex]--;
        }
        
        Array.Copy(output, array, array.Length);
    }
}
```

## Radix Sort (Digit-by-Digit Sort)

**Description**: Radix Sort is a non-comparison sorting algorithm that sorts integers by processing individual digits. It uses counting sort as a subroutine to sort the array according to each digit.

**Code**:

```csharp
public static class RadixSort
{
    public static void Sort(int[] array)
    {
        if (array.Length == 0) return;
        
        int max = array.Max();
        
        // Do counting sort for every digit
        for (int exp = 1; max / exp > 0; exp *= 10)
            CountingSortByDigit(array, exp);
    }
    
    private static void CountingSortByDigit(int[] array, int exp)
    {
        int[] count = new int[10];
        int[] output = new int[array.Length];
        
        // Count occurrences of digits
        for (int i = 0; i < array.Length; i++)
            count[(array[i] / exp) % 10]++;
        
        // Modify count array
        for (int i = 1; i < 10; i++)
            count[i] += count[i - 1];
        
        // Build output array
        for (int i = array.Length - 1; i >= 0; i--)
        {
            int digit = (array[i] / exp) % 10;
            output[count[digit] - 1] = array[i];
            count[digit]--;
        }
        
        Array.Copy(output, array, array.Length);
    }
    
    // Radix sort for strings (MSD - Most Significant Digit)
    public static void SortStrings(string[] array)
    {
        if (array.Length <= 1) return;
        
        int maxLength = array.Max(s => s.Length);
        RadixSortStringsMSD(array, 0, array.Length - 1, 0, maxLength);
    }
    
    private static void RadixSortStringsMSD(string[] array, int low, int high, int digit, int maxLength)
    {
        if (low >= high || digit >= maxLength) return;
        
        int[] count = new int[257]; // 256 ASCII + 1 for strings shorter than digit position
        string[] aux = new string[high - low + 1];
        
        // Count frequency of each character at digit position
        for (int i = low; i <= high; i++)
        {
            int charIndex = digit < array[i].Length ? array[i][digit] + 1 : 0;
            count[charIndex]++;
        }
        
        // Convert counts to indices
        for (int i = 1; i < count.Length; i++)
            count[i] += count[i - 1];
        
        // Distribute
        for (int i = high; i >= low; i--)
        {
            int charIndex = digit < array[i].Length ? array[i][digit] + 1 : 0;
            aux[--count[charIndex]] = array[i];
        }
        
        // Copy back
        for (int i = 0; i < aux.Length; i++)
            array[low + i] = aux[i];
        
        // Recursively sort for each character
        for (int i = 0; i < 256; i++)
        {
            int start = low + (i > 0 ? count[i] : 0);
            int end = low + count[i + 1] - 1;
            if (start < end)
                RadixSortStringsMSD(array, start, end, digit + 1, maxLength);
        }
    }
}
```

## Bucket Sort (Uniform Distribution Sort)

**Description**: Bucket Sort distributes elements into a number of buckets, sorts individual buckets (often using insertion sort), and then concatenates the sorted buckets.

**Code**:

```csharp
public static class BucketSort
{
    public static void Sort(float[] array, int bucketCount = -1)
    {
        if (array.Length <= 1) return;
        
        if (bucketCount == -1)
            bucketCount = array.Length;
        
        // Create buckets
        var buckets = new List<float>[bucketCount];
        for (int i = 0; i < bucketCount; i++)
            buckets[i] = new List<float>();
        
        // Distribute elements into buckets
        foreach (float value in array)
        {
            int bucketIndex = Math.Min((int)(value * bucketCount), bucketCount - 1);
            buckets[bucketIndex].Add(value);
        }
        
        // Sort individual buckets and concatenate
        int index = 0;
        for (int i = 0; i < bucketCount; i++)
        {
            if (buckets[i].Count > 0)
            {
                buckets[i].Sort(); // Use built-in sort for individual buckets
                foreach (float value in buckets[i])
                    array[index++] = value;
            }
        }
    }
    
    // Generic bucket sort for any comparable type with custom distribution function
    public static void Sort<T>(T[] array, Func<T, int> getBucketIndex, int bucketCount, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        
        if (array.Length <= 1) return;
        
        var buckets = new List<T>[bucketCount];
        for (int i = 0; i < bucketCount; i++)
            buckets[i] = new List<T>();
        
        // Distribute elements
        foreach (T item in array)
        {
            int bucketIndex = getBucketIndex(item);
            buckets[bucketIndex].Add(item);
        }
        
        // Sort and concatenate
        int index = 0;
        for (int i = 0; i < bucketCount; i++)
        {
            if (buckets[i].Count > 0)
            {
                buckets[i].Sort(comparer);
                foreach (T item in buckets[i])
                    array[index++] = item;
            }
        }
    }
}
```

**Extended Usage Examples**:

```csharp
// Insertion Sort
int[] smallArray = { 5, 2, 4, 6, 1, 3 };
InsertionSort.Sort(smallArray);
Console.WriteLine($"Insertion Sort: {string.Join(", ", smallArray)}");

// Selection Sort
int[] selectionArray = { 64, 25, 12, 22, 11, 90 };
SelectionSort.Sort(selectionArray);
Console.WriteLine($"Selection Sort: {string.Join(", ", selectionArray)}");

// Shell Sort with Knuth sequence
int[] shellArray = { 9, 5, 1, 4, 3, 6, 8, 2, 7 };
ShellSort.SortWithKnuthSequence(shellArray);
Console.WriteLine($"Shell Sort: {string.Join(", ", shellArray)}");

// Counting Sort
int[] countingArray = { 4, 2, 2, 8, 3, 3, 1 };
CountingSort.Sort(countingArray);
Console.WriteLine($"Counting Sort: {string.Join(", ", countingArray)}");

// Radix Sort
int[] radixArray = { 170, 45, 75, 90, 2, 802, 24, 66 };
RadixSort.Sort(radixArray);
Console.WriteLine($"Radix Sort: {string.Join(", ", radixArray)}");

// Bucket Sort
float[] bucketArray = { 0.42f, 0.32f, 0.23f, 0.52f, 0.25f, 0.47f, 0.51f };
BucketSort.Sort(bucketArray);
Console.WriteLine($"Bucket Sort: {string.Join(", ", bucketArray)}");

// Cocktail Shaker Sort
int[] cocktailArray = { 5, 1, 4, 2, 8, 0, 2 };
BubbleSort.CocktailSort(cocktailArray);
Console.WriteLine($"Cocktail Sort: {string.Join(", ", cocktailArray)}");
```

**Algorithm Classification and Comparison**:

| Algorithm | Time Complexity (Best/Avg/Worst) | Space | Stable | In-Place | Best Use Case |
|-----------|----------------------------------|-------|---------|-----------|---------------|
| Quick Sort | O(n log n) / O(n log n) / O(n²) | O(log n) | No | Yes | General purpose, large arrays |
| Merge Sort | O(n log n) / O(n log n) / O(n log n) | O(n) | Yes | No | When stability required |
| Heap Sort | O(n log n) / O(n log n) / O(n log n) | O(1) | No | Yes | Guaranteed O(n log n) |
| Insertion Sort | O(n) / O(n²) / O(n²) | O(1) | Yes | Yes | Small or nearly sorted arrays |
| Selection Sort | O(n²) / O(n²) / O(n²) | O(1) | No | Yes | When memory writes are expensive |
| Bubble Sort | O(n) / O(n²) / O(n²) | O(1) | Yes | Yes | Educational, very small arrays |
| Shell Sort | O(n) / O(n^1.25) / O(n²) | O(1) | No | Yes | Medium-sized arrays |
| Counting Sort | O(n + k) / O(n + k) / O(n + k) | O(k) | Yes | No | Integers in small range |
| Radix Sort | O(d(n + k)) / O(d(n + k)) / O(d(n + k)) | O(n + k) | Yes | No | Integers with fixed digits |
| Bucket Sort | O(n + k) / O(n + k) / O(n²) | O(n) | Yes | No | Uniformly distributed data |

**Performance Characteristics and Selection Guide**:

- **Comparison-based vs Non-comparison**:
  - **Comparison-based**: Quick, Merge, Heap, Insertion, Selection, Bubble, Shell
  - **Non-comparison**: Counting, Radix, Bucket (can achieve O(n) for specific data types)

- **Stability** (maintains relative order of equal elements):
  - **Stable**: Merge Sort, Insertion Sort, Bubble Sort, Counting Sort, Radix Sort, Bucket Sort
  - **Unstable**: Quick Sort, Heap Sort, Selection Sort, Shell Sort

- **Memory Usage**:
  - **In-place (O(1) extra space)**: Quick Sort, Heap Sort, Insertion Sort, Selection Sort, Bubble Sort, Shell Sort
  - **Requires extra space**: Merge Sort (O(n)), Counting Sort (O(k)), Radix Sort (O(n)), Bucket Sort (O(n))

**Hybrid and Optimized Approaches**:

- **Introsort**: Combines Quick Sort, Heap Sort, and Insertion Sort (used in C++ STL)
- **Timsort**: Hybrid merge sort used in Python and Java (optimized for real-world data)
- **Dual-Pivot Quick Sort**: Java's Arrays.sort() implementation
- **Three-way partitioning**: Optimized Quick Sort for arrays with many duplicate values

**Notes**:

- **Quick Sort (Divide and Conquer Partitioning Sort)**: Average O(n log n), worst-case O(n²), in-place sorting, excellent general-purpose algorithm with good cache performance
- **Merge Sort (Divide and Conquer Merge Sort)**: Guaranteed O(n log n), stable sorting, requires additional O(n) memory, predictable performance
- **Heap Sort (Binary Heap Sort)**: Guaranteed O(n log n), in-place sorting, not stable, good worst-case guarantee but slower than Quick Sort on average
- **Insertion Sort (Incremental Sort)**: O(n) best case for nearly sorted data, O(n²) average/worst case, excellent for small arrays (< 50 elements)
- **Selection Sort (Minimum Selection Sort)**: O(n²) all cases, minimizes number of writes, simple implementation
- **Bubble Sort (Exchange Sort)**: O(n²) average/worst case, O(n) best case, stable, mainly educational value
- **Shell Sort (Diminishing Increment Sort)**: Better than O(n²) with good gap sequences, in-place, good for medium-sized arrays
- **Counting Sort (Non-Comparison Integer Sort)**: O(n + k) where k is range, stable, only works for integers in known range
- **Radix Sort (Digit-by-Digit Sort)**: O(d(n + k)) where d is number of digits, stable, works for integers and strings
- **Bucket Sort (Uniform Distribution Sort)**: O(n + k) average case, works well for uniformly distributed floating-point numbers

**Advanced Optimization Techniques**:

- **Pivot Selection**: Median-of-three, random pivot, or Tukey's ninther for Quick Sort
- **Cutoff to Insertion Sort**: Switch to insertion sort for small subarrays (typically < 10-20 elements)
- **Tail Recursion Elimination**: Convert recursive calls to iterative for better stack usage
- **Three-way Partitioning**: Handle duplicate values efficiently in Quick Sort
- **External Sorting**: Use merge sort variants for data larger than memory
- **Parallel Sorting**: Multi-threaded implementations of merge sort and quick sort

**Implementation Best Practices**:

- Use generic implementations with custom comparers
- Handle edge cases (empty arrays, single elements)
- Consider cache performance for large datasets
- Implement iterative versions to avoid stack overflow
- Use appropriate algorithms based on data characteristics
- Profile with real data to validate performance assumptions

**Security and Robustness Considerations**:

- Quick Sort can be vulnerable to worst-case O(n²) attacks with adversarial input
- Use randomized or median-of-three pivot selection
- Consider Introsort for production systems needing guaranteed O(n log n)
- Validate array bounds to prevent buffer overflows
- Handle integer overflow in index calculations
- Use stable sorts when order preservation is important

## Related Snippets

- [Searching Algorithms](searching-algorithms.md) - Sorted data enables efficient searching
- [Data Structures](data-structures.md) - Heaps, trees, and other structures used in advanced sorting
- [Dynamic Programming Algorithms](dynamic-programming.md) - Some optimization problems involve sorting components
- [String Algorithms](string-algorithms.md) - String sorting using radix sort and suffix arrays
