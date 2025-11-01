# Searching Algorithms

**Description**: Implementation of common searching algorithms with performance analysis and use cases.
**Language/Technology**: C#, Algorithms
**Performance Complexity**: Various O(1) to O(n) depending on algorithm and data structure

## Binary Search (Divide and Conquer Search)

**Description**: Binary Search is a divide-and-conquer algorithm that finds the position of a target value within a sorted array. It compares the target value to the middle element and eliminates half of the search space in each iteration.

**Code**:

```csharp
using System;
using System.Collections.Generic;

public static class BinarySearch
{
    public static int Search<T>(T[] sortedArray, T target, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        int left = 0;
        int right = sortedArray.Length - 1;
        
        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            int comparison = comparer.Compare(sortedArray[mid], target);
            
            if (comparison == 0)
                return mid;
            else if (comparison < 0)
                left = mid + 1;
            else
                right = mid - 1;
        }
        
        return -1; // Not found
    }
    
    public static int SearchRecursive<T>(T[] sortedArray, T target, int left, int right, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        
        if (left > right)
            return -1;
            
        int mid = left + (right - left) / 2;
        int comparison = comparer.Compare(sortedArray[mid], target);
        
        if (comparison == 0)
            return mid;
        else if (comparison < 0)
            return SearchRecursive(sortedArray, target, mid + 1, right, comparer);
        else
            return SearchRecursive(sortedArray, target, left, mid - 1, comparer);
    }
}
```

**Usage**:

```csharp
int[] numbers = { 2, 5, 8, 12, 16, 23, 38, 56, 67, 78 };
int index = BinarySearch.Search(numbers, 23);
Console.WriteLine($"Found at index: {index}"); // Output: Found at index: 5

string[] words = { "apple", "banana", "grape", "orange", "peach" };
int wordIndex = BinarySearch.Search(words, "grape");
Console.WriteLine($"Found at index: {wordIndex}"); // Output: Found at index: 2
```

## Linear Search (Sequential Search)

**Description**: Linear Search is the simplest searching algorithm that sequentially checks each element in the array until a match is found or the entire array has been searched. It works on both sorted and unsorted arrays.

**Code**:

```csharp
public static class LinearSearch
{
    public static int Search<T>(T[] array, T target, IEqualityComparer<T> comparer = null)
        where T : IEquatable<T>
    {
        comparer ??= EqualityComparer<T>.Default;
        
        for (int i = 0; i < array.Length; i++)
        {
            if (comparer.Equals(array[i], target))
                return i;
        }
        
        return -1; // Not found
    }
    
    public static IEnumerable<int> SearchAll<T>(T[] array, T target, IEqualityComparer<T> comparer = null)
        where T : IEquatable<T>
    {
        comparer ??= EqualityComparer<T>.Default;
        
        for (int i = 0; i < array.Length; i++)
        {
            if (comparer.Equals(array[i], target))
                yield return i;
        }
    }
}
```

**Usage**:

```csharp
int[] numbers = { 4, 2, 7, 2, 9, 2, 1 };
int firstIndex = LinearSearch.Search(numbers, 2);
Console.WriteLine($"First occurrence at: {firstIndex}"); // Output: First occurrence at: 1

var allIndices = LinearSearch.SearchAll(numbers, 2).ToArray();
Console.WriteLine($"All occurrences: [{string.Join(", ", allIndices)}]"); // Output: All occurrences: [1, 3, 5]
```

## Interpolation Search (Uniform Distribution Search)

**Description**: Interpolation Search is an improvement over Binary Search for uniformly distributed sorted arrays. Instead of always checking the middle element, it calculates a probable position based on the value being searched, similar to how humans search in a phone book.

**Code**:

```csharp
public static class InterpolationSearch
{
    public static int Search(int[] sortedArray, int target)
    {
        int left = 0;
        int right = sortedArray.Length - 1;
        
        while (left <= right && target >= sortedArray[left] && target <= sortedArray[right])
        {
            if (left == right)
            {
                return sortedArray[left] == target ? left : -1;
            }
            
            // Calculate probable position using interpolation formula
            int pos = left + ((target - sortedArray[left]) * (right - left)) / (sortedArray[right] - sortedArray[left]);
            
            if (sortedArray[pos] == target)
                return pos;
            else if (sortedArray[pos] < target)
                left = pos + 1;
            else
                right = pos - 1;
        }
        
        return -1; // Not found
    }
}
```

**Usage**:

```csharp
int[] uniformNumbers = { 10, 20, 30, 40, 50, 60, 70, 80, 90 };
int index = InterpolationSearch.Search(uniformNumbers, 50);
Console.WriteLine($"Found at index: {index}"); // Output: Found at index: 4
```

## Exponential Search (Doubling Search)

**Description**: Exponential Search finds the range where the element is present and then uses Binary Search in that range. It's useful when the size of the array is unknown or when the target is expected to be near the beginning.

**Code**:

```csharp
public static class ExponentialSearch
{
    public static int Search<T>(T[] sortedArray, T target, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        
        if (sortedArray.Length == 0)
            return -1;
            
        // If target is at first position
        if (comparer.Compare(sortedArray[0], target) == 0)
            return 0;
        
        // Find range for binary search by repeatedly doubling
        int i = 1;
        while (i < sortedArray.Length && comparer.Compare(sortedArray[i], target) <= 0)
            i *= 2;
        
        // Call binary search for the found range
        return BinarySearch.SearchRecursive(sortedArray, target, i / 2, Math.Min(i, sortedArray.Length - 1), comparer);
    }
}
```

**Usage**:

```csharp
int[] numbers = { 2, 3, 4, 10, 40, 50, 80, 100, 120, 140 };
int index = ExponentialSearch.Search(numbers, 10);
Console.WriteLine($"Found at index: {index}"); // Output: Found at index: 3
```

## Jump Search (Block Search)

**Description**: Jump Search works on sorted arrays by jumping ahead by fixed steps and then performing a linear search in the identified block. The optimal jump size is √n.

**Code**:

```csharp
public static class JumpSearch
{
    public static int Search<T>(T[] sortedArray, T target, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        int n = sortedArray.Length;
        int step = (int)Math.Sqrt(n);
        int prev = 0;
        
        // Finding the block where element is present (if it is present)
        while (comparer.Compare(sortedArray[Math.Min(step, n) - 1], target) < 0)
        {
            prev = step;
            step += (int)Math.Sqrt(n);
            if (prev >= n)
                return -1;
        }
        
        // Doing a linear search in the identified block
        while (comparer.Compare(sortedArray[prev], target) < 0)
        {
            prev++;
            if (prev == Math.Min(step, n))
                return -1;
        }
        
        // If element is found
        if (comparer.Compare(sortedArray[prev], target) == 0)
            return prev;
            
        return -1;
    }
}
```

**Usage**:

```csharp
int[] numbers = { 0, 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610 };
int index = JumpSearch.Search(numbers, 55);
Console.WriteLine($"Found at index: {index}"); // Output: Found at index: 10
```

## Ternary Search (Three-Way Search)

**Description**: Ternary Search is a divide-and-conquer algorithm that divides the array into three parts and determines which part the target element lies in. It can be used for both discrete and continuous search spaces.

**Code**:

```csharp
public static class TernarySearch
{
    public static int SearchDiscrete<T>(T[] sortedArray, T target, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        return TernarySearchRecursive(sortedArray, target, 0, sortedArray.Length - 1, comparer);
    }
    
    private static int TernarySearchRecursive<T>(T[] sortedArray, T target, int left, int right, IComparer<T> comparer)
        where T : IComparable<T>
    {
        if (right >= left)
        {
            // Find the mid1 and mid2 points
            int mid1 = left + (right - left) / 3;
            int mid2 = right - (right - left) / 3;
            
            // Check if target is present at any mid
            if (comparer.Compare(sortedArray[mid1], target) == 0)
                return mid1;
            if (comparer.Compare(sortedArray[mid2], target) == 0)
                return mid2;
            
            // Since target is not present at mid, check in which region it is present
            if (comparer.Compare(target, sortedArray[mid1]) < 0)
            {
                // The target lies in between left and mid1
                return TernarySearchRecursive(sortedArray, target, left, mid1 - 1, comparer);
            }
            else if (comparer.Compare(target, sortedArray[mid2]) > 0)
            {
                // The target lies in between mid2 and right
                return TernarySearchRecursive(sortedArray, target, mid2 + 1, right, comparer);
            }
            else
            {
                // The target lies in between mid1 and mid2
                return TernarySearchRecursive(sortedArray, target, mid1 + 1, mid2 - 1, comparer);
            }
        }
        
        // Target not found
        return -1;
    }
    
    // Ternary search for finding maximum/minimum in unimodal function
    public static double FindMaximum(Func<double, double> function, double left, double right, double epsilon = 1e-9)
    {
        while (right - left > epsilon)
        {
            double mid1 = left + (right - left) / 3.0;
            double mid2 = right - (right - left) / 3.0;
            
            if (function(mid1) > function(mid2))
                right = mid2;
            else
                left = mid1;
        }
        
        return (left + right) / 2.0;
    }
}
```

## Fibonacci Search

**Description**: Fibonacci Search uses Fibonacci numbers to divide the array into unequal parts. It's advantageous when the cost of comparison is high and random access to memory is expensive.

**Code**:

```csharp
public static class FibonacciSearch
{
    public static int Search<T>(T[] sortedArray, T target, IComparer<T> comparer = null)
        where T : IComparable<T>
    {
        comparer ??= Comparer<T>.Default;
        int n = sortedArray.Length;
        
        // Initialize Fibonacci numbers
        int fibMMm2 = 0; // (m-2)'th Fibonacci number
        int fibMMm1 = 1; // (m-1)'th Fibonacci number
        int fibM = fibMMm2 + fibMMm1; // m'th Fibonacci number
        
        // fibM is going to store the smallest Fibonacci number >= n
        while (fibM < n)
        {
            fibMMm2 = fibMMm1;
            fibMMm1 = fibM;
            fibM = fibMMm2 + fibMMm1;
        }
        
        // Marks the eliminated range from front
        int offset = -1;
        
        // while there are elements to be inspected
        while (fibM > 1)
        {
            // Check if fibMMm2 is a valid location
            int i = Math.Min(offset + fibMMm2, n - 1);
            
            // If target is greater than the value at index fibMMm2, cut the subarray from offset to i
            if (comparer.Compare(sortedArray[i], target) < 0)
            {
                fibM = fibMMm1;
                fibMMm1 = fibMMm2;
                fibMMm2 = fibM - fibMMm1;
                offset = i;
            }
            // If target is less than the value at index fibMMm2, cut the subarray after i+1
            else if (comparer.Compare(sortedArray[i], target) > 0)
            {
                fibM = fibMMm2;
                fibMMm1 = fibMMm1 - fibMMm2;
                fibMMm2 = fibM - fibMMm1;
            }
            // Element found
            else
                return i;
        }
        
        // Comparing the last element with target
        if (fibMMm1 == 1 && offset + 1 < n && comparer.Compare(sortedArray[offset + 1], target) == 0)
            return offset + 1;
        
        // Element not found
        return -1;
    }
}
```

## Hash-Based Search (Dictionary/Map Search)

**Description**: Hash-based search provides average O(1) time complexity for search operations by using hash tables. It's the fastest search method for unsorted data when memory allows.

**Code**:

```csharp
public static class HashSearch
{
    public class HashSearchStructure<TKey, TValue>
    {
        private readonly Dictionary<TKey, List<int>> _hashMap;
        private readonly TValue[] _originalArray;
        private readonly Func<TValue, TKey> _keySelector;
        
        public HashSearchStructure(TValue[] array, Func<TValue, TKey> keySelector)
        {
            _originalArray = array;
            _keySelector = keySelector;
            _hashMap = new Dictionary<TKey, List<int>>();
            
            BuildHashMap();
        }
        
        private void BuildHashMap()
        {
            for (int i = 0; i < _originalArray.Length; i++)
            {
                TKey key = _keySelector(_originalArray[i]);
                
                if (!_hashMap.ContainsKey(key))
                    _hashMap[key] = new List<int>();
                    
                _hashMap[key].Add(i);
            }
        }
        
        public int Search(TKey key)
        {
            if (_hashMap.TryGetValue(key, out List<int> indices) && indices.Count > 0)
                return indices[0];
            return -1;
        }
        
        public IEnumerable<int> SearchAll(TKey key)
        {
            if (_hashMap.TryGetValue(key, out List<int> indices))
                return indices;
            return Enumerable.Empty<int>();
        }
        
        public bool Contains(TKey key)
        {
            return _hashMap.ContainsKey(key);
        }
    }
    
    // Simple hash search for primitive types
    public static HashSearchStructure<T, T> CreateSearchStructure<T>(T[] array)
    {
        return new HashSearchStructure<T, T>(array, x => x);
    }
}
```

**Extended Usage Examples**:

```csharp
// Exponential Search
int[] exponentialArray = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
int expIndex = ExponentialSearch.Search(exponentialArray, 13);
Console.WriteLine($"Exponential Search found 13 at index: {expIndex}");

// Jump Search
int[] jumpArray = { 0, 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610 };
int jumpIndex = JumpSearch.Search(jumpArray, 55);
Console.WriteLine($"Jump Search found 55 at index: {jumpIndex}");

// Ternary Search
int[] ternaryArray = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
int ternaryIndex = TernarySearch.SearchDiscrete(ternaryArray, 5);
Console.WriteLine($"Ternary Search found 5 at index: {ternaryIndex}");

// Ternary Search for continuous optimization
double maxPoint = TernarySearch.FindMaximum(x => -(x - 3) * (x - 3) + 10, 0, 6);
Console.WriteLine($"Maximum point found at x = {maxPoint:F6}");

// Fibonacci Search
int[] fibArray = { 10, 22, 35, 40, 45, 50, 80, 82, 85, 90, 100 };
int fibIndex = FibonacciSearch.Search(fibArray, 85);
Console.WriteLine($"Fibonacci Search found 85 at index: {fibIndex}");

// Hash Search
string[] names = { "Alice", "Bob", "Charlie", "Diana", "Alice", "Eve" };
var hashSearch = HashSearch.CreateSearchStructure(names);
int hashIndex = hashSearch.Search("Alice");
var allAliceIndices = hashSearch.SearchAll("Alice").ToArray();
Console.WriteLine($"Hash Search found first 'Alice' at index: {hashIndex}");
Console.WriteLine($"All 'Alice' indices: [{string.Join(", ", allAliceIndices)}]");
```

**Algorithm Comparison and Selection Guide**:

| Algorithm | Time Complexity | Space Complexity | Best Use Case |
|-----------|-----------------|------------------|---------------|
| Linear Search | O(n) | O(1) | Small arrays, unsorted data |
| Binary Search | O(log n) | O(1) | Sorted arrays, general purpose |
| Interpolation Search | O(log log n) avg, O(n) worst | O(1) | Uniformly distributed sorted data |
| Jump Search | O(√n) | O(1) | When binary search is costly |
| Exponential Search | O(log n) | O(1) | Unbounded/infinite arrays |
| Ternary Search | O(log₃ n) | O(1) | Unimodal functions, optimization |
| Fibonacci Search | O(log n) | O(1) | When division is costly |
| Hash Search | O(1) avg, O(n) worst | O(n) | Frequent searches, memory available |

**Performance Considerations**:

- **Binary Search vs Ternary Search**: While ternary search has fewer iterations, it requires more comparisons per iteration. Binary search is generally preferred.
- **Cache Performance**: Linear search can be faster for small arrays due to better cache locality.
- **Memory Access Patterns**: Jump and exponential search can be beneficial when memory access is expensive.
- **Function Evaluation Cost**: Use ternary search when the comparison/evaluation function is expensive.

**Notes**:

- **Binary Search (Divide and Conquer Search)**: O(log n) time, requires sorted array, most commonly used for sorted data
- **Linear Search (Sequential Search)**: O(n) time, works on unsorted arrays, simple implementation, good cache locality for small arrays
- **Interpolation Search (Uniform Distribution Search)**: O(log log n) average case on uniformly distributed data, O(n) worst case
- **Exponential Search (Doubling Search)**: O(log n) time, useful for unbounded arrays or when target is near the beginning
- **Jump Search (Block Search)**: O(√n) time, good balance between linear and binary search, optimal jump size is √n
- **Ternary Search (Three-Way Search)**: O(log₃ n) time, useful for unimodal function optimization, more comparisons per iteration than binary search
- **Fibonacci Search**: O(log n) time, uses Fibonacci numbers for division, good when division operation is expensive
- **Hash Search**: O(1) average case, O(n) worst case, requires additional O(n) space, fastest for frequent lookups

**Memory and Performance Guidelines**:

- **Small Arrays (< 100 elements)**: Linear search often fastest due to cache effects
- **Medium Arrays (100-10,000 elements)**: Binary search is optimal for sorted data
- **Large Arrays (> 10,000 elements)**: Consider hash tables for multiple searches
- **Frequent Searches**: Build hash table or other index structure
- **Memory Constrained**: Use in-place algorithms like binary search
- **Uniformly Distributed Data**: Interpolation search can outperform binary search
- **Unknown Array Size**: Exponential search followed by binary search

**Specialized Search Scenarios**:

- **Nearly Sorted Data**: Linear search with early termination
- **Searching for Range**: Modified binary search for first/last occurrence
- **Approximate Search**: Interpolation search or ternary search for continuous domains
- **Multiple Patterns**: Use advanced string searching algorithms (see string-algorithms.md)
- **Geometric Search**: Spatial data structures (k-d trees, quad trees)

**Implementation Best Practices**:

- Always handle edge cases (empty arrays, single elements)
- Use generic implementations with custom comparers for flexibility
- Consider iterative vs recursive approaches based on stack limitations  
- Implement bounds checking to prevent array overflow
- Use appropriate integer overflow protection in index calculations
- Profile different algorithms with your specific data characteristics

**Security Considerations**:

- Hash tables can be vulnerable to collision attacks
- Always validate array bounds to prevent buffer overflows
- Consider timing attacks when searching sensitive data
- Use secure comparison functions for cryptographic applications

## Related Snippets

- [Sorting Algorithms](sorting-algorithms.md) - Many search algorithms require pre-sorted data
- [Data Structures](data-structures.md) - Hash tables, trees, and other structures used in advanced searching
- [String Algorithms](string-algorithms.md) - Specialized search algorithms for text and pattern matching
- [Dynamic Programming Algorithms](dynamic-programming.md) - Optimization problems often involve search components
