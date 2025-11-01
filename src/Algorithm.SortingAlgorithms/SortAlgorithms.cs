namespace Algorithm.SortingAlgorithms;

/// <summary>
/// Collection of sorting algorithms with generic implementations.
/// </summary>
public static class SortAlgorithms
{
    /// <summary>
    /// Bubble Sort - Simple comparison-based sorting algorithm.
    /// Time Complexity: O(n²), Space Complexity: O(1)
    /// </summary>
    public static void BubbleSort<T>(T[] array) where T : IComparable<T>
    {
        var n = array.Length;
        
        for (var i = 0; i < n - 1; i++)
        {
            var swapped = false;
            
            for (var j = 0; j < n - 1 - i; j++)
            {
                if (array[j].CompareTo(array[j + 1]) > 0)
                {
                    (array[j], array[j + 1]) = (array[j + 1], array[j]);
                    swapped = true;
                }
            }
            
            // If no swapping occurred, array is already sorted
            if (!swapped)
                break;
        }
    }
    
    /// <summary>
    /// Selection Sort - Finds minimum element and places it at the beginning.
    /// Time Complexity: O(n²), Space Complexity: O(1)
    /// </summary>
    public static void SelectionSort<T>(T[] array) where T : IComparable<T>
    {
        var n = array.Length;
        
        for (var i = 0; i < n - 1; i++)
        {
            var minIndex = i;
            
            // Find the minimum element in remaining unsorted array
            for (var j = i + 1; j < n; j++)
            {
                if (array[j].CompareTo(array[minIndex]) < 0)
                    minIndex = j;
            }
            
            // Swap the found minimum element with the first element
            if (minIndex != i)
                (array[i], array[minIndex]) = (array[minIndex], array[i]);
        }
    }
    
    /// <summary>
    /// Insertion Sort - Builds the final sorted array one item at a time.
    /// Time Complexity: O(n²), Space Complexity: O(1)
    /// </summary>
    public static void InsertionSort<T>(T[] array) where T : IComparable<T>
    {
        for (var i = 1; i < array.Length; i++)
        {
            var key = array[i];
            var j = i - 1;
            
            // Move elements that are greater than key one position ahead
            while (j >= 0 && array[j].CompareTo(key) > 0)
            {
                array[j + 1] = array[j];
                j--;
            }
            
            array[j + 1] = key;
        }
    }
    
    /// <summary>
    /// Merge Sort - Divide and conquer algorithm.
    /// Time Complexity: O(n log n), Space Complexity: O(n)
    /// </summary>
    public static void MergeSort<T>(T[] array) where T : IComparable<T>
    {
        if (array.Length <= 1)
            return;
            
        MergeSortHelper(array, 0, array.Length - 1);
    }
    
    private static void MergeSortHelper<T>(T[] array, int left, int right) where T : IComparable<T>
    {
        if (left < right)
        {
            var mid = left + (right - left) / 2;
            
            MergeSortHelper(array, left, mid);
            MergeSortHelper(array, mid + 1, right);
            Merge(array, left, mid, right);
        }
    }
    
    private static void Merge<T>(T[] array, int left, int mid, int right) where T : IComparable<T>
    {
        var leftSize = mid - left + 1;
        var rightSize = right - mid;
        
        var leftArray = new T[leftSize];
        var rightArray = new T[rightSize];
        
        Array.Copy(array, left, leftArray, 0, leftSize);
        Array.Copy(array, mid + 1, rightArray, 0, rightSize);
        
        int i = 0, j = 0, k = left;
        
        while (i < leftSize && j < rightSize)
        {
            if (leftArray[i].CompareTo(rightArray[j]) <= 0)
                array[k++] = leftArray[i++];
            else
                array[k++] = rightArray[j++];
        }
        
        while (i < leftSize)
            array[k++] = leftArray[i++];
            
        while (j < rightSize)
            array[k++] = rightArray[j++];
    }
    
    /// <summary>
    /// Quick Sort - Efficient divide and conquer algorithm.
    /// Time Complexity: O(n log n) average, O(n²) worst, Space Complexity: O(log n)
    /// </summary>
    public static void QuickSort<T>(T[] array) where T : IComparable<T>
    {
        QuickSortHelper(array, 0, array.Length - 1);
    }
    
    private static void QuickSortHelper<T>(T[] array, int low, int high) where T : IComparable<T>
    {
        if (low < high)
        {
            var partitionIndex = Partition(array, low, high);
            
            QuickSortHelper(array, low, partitionIndex - 1);
            QuickSortHelper(array, partitionIndex + 1, high);
        }
    }
    
    private static int Partition<T>(T[] array, int low, int high) where T : IComparable<T>
    {
        var pivot = array[high];
        var i = low - 1;
        
        for (var j = low; j < high; j++)
        {
            if (array[j].CompareTo(pivot) < 0)
            {
                i++;
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
        
        (array[i + 1], array[high]) = (array[high], array[i + 1]);
        return i + 1;
    }
    
    /// <summary>
    /// Heap Sort - Uses heap data structure to sort.
    /// Time Complexity: O(n log n), Space Complexity: O(1)
    /// </summary>
    public static void HeapSort<T>(T[] array) where T : IComparable<T>
    {
        var n = array.Length;
        
        // Build max heap
        for (var i = n / 2 - 1; i >= 0; i--)
            Heapify(array, n, i);
        
        // Extract elements from heap one by one
        for (var i = n - 1; i > 0; i--)
        {
            (array[0], array[i]) = (array[i], array[0]);
            Heapify(array, i, 0);
        }
    }
    
    private static void Heapify<T>(T[] array, int n, int i) where T : IComparable<T>
    {
        var largest = i;
        var left = 2 * i + 1;
        var right = 2 * i + 2;
        
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
    
    /// <summary>
    /// Counting Sort - Non-comparison based sorting for integers.
    /// Time Complexity: O(n + k), Space Complexity: O(k)
    /// where k is the range of input
    /// </summary>
    public static int[] CountingSort(int[] array, int maxValue)
    {
        var count = new int[maxValue + 1];
        var output = new int[array.Length];
        
        // Count occurrences
        foreach (var num in array)
            count[num]++;
        
        // Change count[i] to actual position
        for (var i = 1; i <= maxValue; i++)
            count[i] += count[i - 1];
        
        // Build output array
        for (var i = array.Length - 1; i >= 0; i--)
        {
            output[count[array[i]] - 1] = array[i];
            count[array[i]]--;
        }
        
        return output;
    }
    
    /// <summary>
    /// Radix Sort - Non-comparison based sorting for integers.
    /// Time Complexity: O(d * (n + k)), Space Complexity: O(n + k)
    /// where d is the number of digits
    /// </summary>
    public static void RadixSort(int[] array)
    {
        if (array.Length == 0)
            return;
            
        var max = array.Max();
        
        // Do counting sort for every digit
        for (var exp = 1; max / exp > 0; exp *= 10)
            CountingSortByDigit(array, exp);
    }
    
    private static void CountingSortByDigit(int[] array, int exp)
    {
        var n = array.Length;
        var output = new int[n];
        var count = new int[10];
        
        // Count occurrences of each digit
        for (var i = 0; i < n; i++)
            count[(array[i] / exp) % 10]++;
        
        // Change count[i] to actual position
        for (var i = 1; i < 10; i++)
            count[i] += count[i - 1];
        
        // Build output array
        for (var i = n - 1; i >= 0; i--)
        {
            var digit = (array[i] / exp) % 10;
            output[count[digit] - 1] = array[i];
            count[digit]--;
        }
        
        // Copy output array back to original array
        for (var i = 0; i < n; i++)
            array[i] = output[i];
    }
    
    /// <summary>
    /// Shell Sort - Improved insertion sort with gap sequences.
    /// Time Complexity: depends on gap sequence, Space Complexity: O(1)
    /// </summary>
    public static void ShellSort<T>(T[] array) where T : IComparable<T>
    {
        var n = array.Length;
        
        // Start with a big gap, then reduce the gap
        for (var gap = n / 2; gap > 0; gap /= 2)
        {
            for (var i = gap; i < n; i++)
            {
                var temp = array[i];
                var j = i;
                
                while (j >= gap && array[j - gap].CompareTo(temp) > 0)
                {
                    array[j] = array[j - gap];
                    j -= gap;
                }
                
                array[j] = temp;
            }
        }
    }
}