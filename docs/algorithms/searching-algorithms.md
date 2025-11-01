# Searching Algorithms

**Description**: Implementation of common searching algorithms with performance analysis and use cases.
**Language/Technology**: C#, Algorithms
**Performance Complexity**: Various O(1) to O(n) depending on algorithm and data structure

## Binary Search

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

## Linear Search

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

## Interpolation Search

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

**Notes**:

- **Binary Search**: O(log n) time, requires sorted array, most commonly used
- **Linear Search**: O(n) time, works on unsorted arrays, simple implementation
- **Interpolation Search**: O(log log n) average case on uniformly distributed data, O(n) worst case

**Performance Guidelines**:

- Use Binary Search for sorted arrays when you need O(log n) performance
- Use Linear Search for small arrays or when data is not sorted
- Use Interpolation Search for uniformly distributed sorted data
- Consider hash tables for O(1) average-case lookup when frequent searches are needed

**Related Snippets**:

- [Sorting Algorithms](sorting-algorithms.md)
- [Data Structures](data-structures.md)
