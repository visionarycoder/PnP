namespace Algorithm.SearchingAlgorithms;

/// <summary>
/// Collection of searching algorithms with generic implementations.
/// </summary>
public static class SearchAlgorithms
{
    /// <summary>
    /// Linear search algorithm that works on any enumerable collection.
    /// Time Complexity: O(n)
    /// </summary>
    public static int LinearSearch<T>(T[] array, T target) where T : IEquatable<T>
    {
        for (var i = 0; i < array.Length; i++)
        {
            if (array[i].Equals(target))
                return i;
        }
        return -1; // Not found
    }
    
    /// <summary>
    /// Binary search algorithm for sorted arrays.
    /// Time Complexity: O(log n)
    /// </summary>
    public static int BinarySearch<T>(T[] array, T target) where T : IComparable<T>
    {
        var left = 0;
        var right = array.Length - 1;
        
        while (left <= right)
        {
            var mid = left + (right - left) / 2;
            var comparison = array[mid].CompareTo(target);
            
            if (comparison == 0)
                return mid;
            
            if (comparison < 0)
                left = mid + 1;
            else
                right = mid - 1;
        }
        
        return -1; // Not found
    }
    
    /// <summary>
    /// Recursive binary search implementation.
    /// Time Complexity: O(log n)
    /// </summary>
    public static int BinarySearchRecursive<T>(T[] array, T target, int left = 0, int right = -1) where T : IComparable<T>
    {
        if (right == -1)
            right = array.Length - 1;
        
        if (left > right)
            return -1;
        
        var mid = left + (right - left) / 2;
        var comparison = array[mid].CompareTo(target);
        
        if (comparison == 0)
            return mid;
        
        if (comparison < 0)
            return BinarySearchRecursive(array, target, mid + 1, right);
        else
            return BinarySearchRecursive(array, target, left, mid - 1);
    }
    
    /// <summary>
    /// Jump search algorithm for sorted arrays.
    /// Time Complexity: O(√n)
    /// </summary>
    public static int JumpSearch<T>(T[] array, T target) where T : IComparable<T>
    {
        var n = array.Length;
        var step = (int)Math.Sqrt(n);
        var prev = 0;
        
        // Finding the block where target may be present
        while (array[Math.Min(step, n) - 1].CompareTo(target) < 0)
        {
            prev = step;
            step += (int)Math.Sqrt(n);
            
            if (prev >= n)
                return -1;
        }
        
        // Linear search in the identified block
        while (array[prev].CompareTo(target) < 0)
        {
            prev++;
            
            // If we reached next block or end of array
            if (prev == Math.Min(step, n))
                return -1;
        }
        
        // If element is found
        if (array[prev].CompareTo(target) == 0)
            return prev;
        
        return -1;
    }
    
    /// <summary>
    /// Interpolation search algorithm for uniformly distributed sorted arrays.
    /// Time Complexity: O(log log n) for uniform distribution, O(n) worst case
    /// </summary>
    public static int InterpolationSearch(int[] array, int target)
    {
        var left = 0;
        var right = array.Length - 1;
        
        while (left <= right && target >= array[left] && target <= array[right])
        {
            // If left == right, we found the target or it doesn't exist
            if (left == right)
            {
                return array[left] == target ? left : -1;
            }
            
            // Calculate position using interpolation formula
            var pos = left + (target - array[left]) * (right - left) / (array[right] - array[left]);
            
            if (array[pos] == target)
                return pos;
            
            if (array[pos] < target)
                left = pos + 1;
            else
                right = pos - 1;
        }
        
        return -1;
    }
    
    /// <summary>
    /// Exponential search algorithm for sorted arrays.
    /// Time Complexity: O(log n)
    /// </summary>
    public static int ExponentialSearch<T>(T[] array, T target) where T : IComparable<T>
    {
        // If target is at first position
        if (array[0].CompareTo(target) == 0)
            return 0;
        
        // Find range for binary search by repeated doubling
        var bound = 1;
        while (bound < array.Length && array[bound].CompareTo(target) <= 0)
            bound *= 2;
        
        // Call binary search for the found range
        var left = bound / 2;
        var right = Math.Min(bound, array.Length - 1);
        
        return BinarySearchInRange(array, target, left, right);
    }
    
    /// <summary>
    /// Helper method for binary search in a specific range.
    /// </summary>
    private static int BinarySearchInRange<T>(T[] array, T target, int left, int right) where T : IComparable<T>
    {
        while (left <= right)
        {
            var mid = left + (right - left) / 2;
            var comparison = array[mid].CompareTo(target);
            
            if (comparison == 0)
                return mid;
            
            if (comparison < 0)
                left = mid + 1;
            else
                right = mid - 1;
        }
        
        return -1;
    }
    
    /// <summary>
    /// Finds the first occurrence of target in a sorted array with duplicates.
    /// </summary>
    public static int FindFirstOccurrence<T>(T[] array, T target) where T : IComparable<T>
    {
        var left = 0;
        var right = array.Length - 1;
        var result = -1;
        
        while (left <= right)
        {
            var mid = left + (right - left) / 2;
            var comparison = array[mid].CompareTo(target);
            
            if (comparison == 0)
            {
                result = mid;
                right = mid - 1; // Continue searching in left half
            }
            else if (comparison < 0)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Finds the last occurrence of target in a sorted array with duplicates.
    /// </summary>
    public static int FindLastOccurrence<T>(T[] array, T target) where T : IComparable<T>
    {
        var left = 0;
        var right = array.Length - 1;
        var result = -1;
        
        while (left <= right)
        {
            var mid = left + (right - left) / 2;
            var comparison = array[mid].CompareTo(target);
            
            if (comparison == 0)
            {
                result = mid;
                left = mid + 1; // Continue searching in right half
            }
            else if (comparison < 0)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }
        
        return result;
    }
}