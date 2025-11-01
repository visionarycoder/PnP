using Algorithm.SortingAlgorithms;

namespace Algorithm.SortingAlgorithms;

/// <summary>
/// Demonstrates various sorting algorithms with performance comparisons.
/// </summary>
internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("=== Sorting Algorithms Demo ===\n");
        
        DemonstrateBasicSorts();
        Console.WriteLine();
        
        DemonstrateAdvancedSorts();
        Console.WriteLine();
        
        DemonstrateSpecializedSorts();
        Console.WriteLine();
        
        DemonstratePerformanceComparison();
    }
    
    private static void DemonstrateBasicSorts()
    {
        Console.WriteLine("--- Basic Sorting Algorithms ---");
        
        var originalArray = new[] { 64, 34, 25, 12, 22, 11, 90, 5 };
        
        Console.WriteLine($"Original array: [{string.Join(", ", originalArray)}]\n");
        
        // Bubble Sort
        var bubbleArray = (int[])originalArray.Clone();
        SortAlgorithms.BubbleSort(bubbleArray);
        Console.WriteLine($"Bubble Sort:    [{string.Join(", ", bubbleArray)}]");
        
        // Selection Sort
        var selectionArray = (int[])originalArray.Clone();
        SortAlgorithms.SelectionSort(selectionArray);
        Console.WriteLine($"Selection Sort: [{string.Join(", ", selectionArray)}]");
        
        // Insertion Sort
        var insertionArray = (int[])originalArray.Clone();
        SortAlgorithms.InsertionSort(insertionArray);
        Console.WriteLine($"Insertion Sort: [{string.Join(", ", insertionArray)}]");
    }
    
    private static void DemonstrateAdvancedSorts()
    {
        Console.WriteLine("--- Advanced Sorting Algorithms ---");
        
        var originalArray = new[] { 38, 27, 43, 3, 9, 82, 10 };
        
        Console.WriteLine($"Original array: [{string.Join(", ", originalArray)}]\n");
        
        // Merge Sort
        var mergeArray = (int[])originalArray.Clone();
        SortAlgorithms.MergeSort(mergeArray);
        Console.WriteLine($"Merge Sort: [{string.Join(", ", mergeArray)}]");
        
        // Quick Sort
        var quickArray = (int[])originalArray.Clone();
        SortAlgorithms.QuickSort(quickArray);
        Console.WriteLine($"Quick Sort: [{string.Join(", ", quickArray)}]");
        
        // Heap Sort
        var heapArray = (int[])originalArray.Clone();
        SortAlgorithms.HeapSort(heapArray);
        Console.WriteLine($"Heap Sort:  [{string.Join(", ", heapArray)}]");
        
        // Shell Sort
        var shellArray = (int[])originalArray.Clone();
        SortAlgorithms.ShellSort(shellArray);
        Console.WriteLine($"Shell Sort: [{string.Join(", ", shellArray)}]");
    }
    
    private static void DemonstrateSpecializedSorts()
    {
        Console.WriteLine("--- Specialized Sorting Algorithms ---");
        
        var integerArray = new[] { 4, 2, 2, 8, 3, 3, 1 };
        Console.WriteLine($"Original array: [{string.Join(", ", integerArray)}]");
        
        // Counting Sort (for integers with known range)
        var countingSorted = SortAlgorithms.CountingSort(integerArray, integerArray.Max());
        Console.WriteLine($"Counting Sort: [{string.Join(", ", countingSorted)}]");
        
        // Radix Sort (for integers)
        var radixArray = new[] { 170, 45, 75, 90, 2, 802, 24, 66 };
        Console.WriteLine($"\nRadix Sort input: [{string.Join(", ", radixArray)}]");
        SortAlgorithms.RadixSort(radixArray);
        Console.WriteLine($"Radix Sort:       [{string.Join(", ", radixArray)}]");
        
        // Demonstrate with strings
        var stringArray = new[] { "banana", "apple", "cherry", "date", "elderberry" };
        Console.WriteLine($"\nString array: [{string.Join(", ", stringArray)}]");
        SortAlgorithms.QuickSort(stringArray);
        Console.WriteLine($"Quick Sort:   [{string.Join(", ", stringArray)}]");
    }
    
    private static void DemonstratePerformanceComparison()
    {
        Console.WriteLine("--- Performance Comparison ---");
        
        const int size = 5000;
        Console.WriteLine($"Testing with array of size {size:N0}");
        
        // Generate random array for testing
        var random = new Random();
        var originalArray = new int[size];
        for (var i = 0; i < size; i++)
        {
            originalArray[i] = random.Next(1, 1000);
        }
        
        var algorithms = new Dictionary<string, Action<int[]>>
        {
            { "Bubble Sort", SortAlgorithms.BubbleSort },
            { "Selection Sort", SortAlgorithms.SelectionSort },
            { "Insertion Sort", SortAlgorithms.InsertionSort },
            { "Merge Sort", SortAlgorithms.MergeSort },
            { "Quick Sort", SortAlgorithms.QuickSort },
            { "Heap Sort", SortAlgorithms.HeapSort },
            { "Shell Sort", SortAlgorithms.ShellSort }
        };
        
        var results = new Dictionary<string, double>();
        
        foreach (var algorithm in algorithms)
        {
            var testArray = (int[])originalArray.Clone();
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            algorithm.Value(testArray);
            stopwatch.Stop();
            
            results[algorithm.Key] = stopwatch.Elapsed.TotalMilliseconds;
            
            // Verify the array is sorted
            var isSorted = IsSorted(testArray);
            Console.WriteLine($"{algorithm.Key}: {stopwatch.Elapsed.TotalMilliseconds:F2} ms {(isSorted ? "✓" : "✗")}");
        }
        
        // Test specialized sorts with appropriate data
        var integerArray = originalArray.Where(x => x <= 100).ToArray();
        
        if (integerArray.Length > 0)
        {
            var countingStopwatch = System.Diagnostics.Stopwatch.StartNew();
            SortAlgorithms.CountingSort(integerArray, 100);
            countingStopwatch.Stop();
            Console.WriteLine($"Counting Sort: {countingStopwatch.Elapsed.TotalMilliseconds:F2} ms ✓");
            
            var radixTestArray = (int[])integerArray.Clone();
            var radixStopwatch = System.Diagnostics.Stopwatch.StartNew();
            SortAlgorithms.RadixSort(radixTestArray);
            radixStopwatch.Stop();
            Console.WriteLine($"Radix Sort: {radixStopwatch.Elapsed.TotalMilliseconds:F2} ms {(IsSorted(radixTestArray) ? "✓" : "✗")}");
        }
        
        // Show fastest algorithms
        Console.WriteLine("\nFastest algorithms:");
        var sortedResults = results.OrderBy(r => r.Value).Take(3);
        
        var rank = 1;
        foreach (var result in sortedResults)
        {
            Console.WriteLine($"{rank++}. {result.Key}: {result.Value:F2} ms");
        }
    }
    
    private static bool IsSorted<T>(T[] array) where T : IComparable<T>
    {
        for (var i = 0; i < array.Length - 1; i++)
        {
            if (array[i].CompareTo(array[i + 1]) > 0)
                return false;
        }
        return true;
    }
}