using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Python.DataStructures;

/// <summary>
/// Python data structure examples: custom classes, collections, algorithms, and performance optimization.
/// Demonstrates C# equivalents of Python data structures and collection patterns.
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.WriteLine("=== Python Data Structures Examples ===\n");

        ListOperationsExample();
        DictionaryOperationsExample();
        SetOperationsExample();
        TupleAndNamedTupleExample();
        StackAndQueueExample();
        DequeOperationsExample();
        CustomCollectionsExample();
        AlgorithmPatternsExample();
        PerformanceOptimizationExample();
        MemoryEfficientStructuresExample();
    }

    /// <summary>
    /// Python list operations and comprehensions
    /// Python equivalent: list operations, list comprehensions, slicing
    /// </summary>
    private static void ListOperationsExample()
    {
        Console.WriteLine("1. List Operations:");

        // Python: numbers = [1, 2, 3, 4, 5]
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        
        // Python: squares = [x**2 for x in numbers]
        var squares = numbers.Select(x => x * x).ToList();
        
        // Python: evens = [x for x in numbers if x % 2 == 0]
        var evens = numbers.Where(x => x % 2 == 0).ToList();
        
        // Python: numbers[1:4] (slicing)
        var slice = numbers.Skip(1).Take(3).ToList();

        Console.WriteLine($"   Original: [{string.Join(", ", numbers)}]");
        Console.WriteLine($"   Squares: [{string.Join(", ", squares)}]");
        Console.WriteLine($"   Evens: [{string.Join(", ", evens)}]");
        Console.WriteLine($"   Slice [1:4]: [{string.Join(", ", slice)}]");

        // Python: list.extend(), append(), insert()
        numbers.AddRange(new[] { 6, 7, 8 });  // extend
        numbers.Add(9);                        // append
        numbers.Insert(0, 0);                  // insert at index

        Console.WriteLine($"   After modifications: [{string.Join(", ", numbers)}]\n");
    }

    /// <summary>
    /// Python dictionary operations and comprehensions
    /// Python equivalent: dict operations, dict comprehensions, defaultdict
    /// </summary>
    private static void DictionaryOperationsExample()
    {
        Console.WriteLine("2. Dictionary Operations:");

        // Python: person = {'name': 'Alice', 'age': 25, 'city': 'New York'}
        var person = new Dictionary<string, object>
        {
            ["name"] = "Alice",
            ["age"] = 25,
            ["city"] = "New York"
        };

        // Python: {k: v for k, v in person.items() if isinstance(v, str)}
        var stringValues = person
            .Where(kvp => kvp.Value is string)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        Console.WriteLine("   Person dictionary:");
        foreach (var (key, value) in person)
        {
            Console.WriteLine($"     {key}: {value}");
        }

        Console.WriteLine("   String values only:");
        foreach (var (key, value) in stringValues)
        {
            Console.WriteLine($"     {key}: {value}");
        }

        // Python: collections.defaultdict(list)
        var groupedData = new Dictionary<string, List<int>>();
        var items = new[] { ("group1", 1), ("group2", 2), ("group1", 3), ("group2", 4) };
        
        foreach (var (group, value) in items)
        {
            if (!groupedData.ContainsKey(group))
                groupedData[group] = new List<int>();
            groupedData[group].Add(value);
        }

        Console.WriteLine("   Grouped data (defaultdict equivalent):");
        foreach (var (group, values) in groupedData)
        {
            Console.WriteLine($"     {group}: [{string.Join(", ", values)}]");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Python set operations
    /// Python equivalent: set operations, set comprehensions
    /// </summary>
    private static void SetOperationsExample()
    {
        Console.WriteLine("3. Set Operations:");

        // Python: set1 = {1, 2, 3, 4, 5}
        var set1 = new HashSet<int> { 1, 2, 3, 4, 5 };
        var set2 = new HashSet<int> { 4, 5, 6, 7, 8 };

        // Python: set1 & set2 (intersection)
        var intersection = set1.Intersect(set2).ToHashSet();
        
        // Python: set1 | set2 (union)
        var union = set1.Union(set2).ToHashSet();
        
        // Python: set1 - set2 (difference)
        var difference = set1.Except(set2).ToHashSet();
        
        // Python: set1 ^ set2 (symmetric difference)
        var symmetricDiff = set1.Except(set2).Union(set2.Except(set1)).ToHashSet();

        Console.WriteLine($"   Set1: {{{string.Join(", ", set1.OrderBy(x => x))}}}");
        Console.WriteLine($"   Set2: {{{string.Join(", ", set2.OrderBy(x => x))}}}");
        Console.WriteLine($"   Intersection: {{{string.Join(", ", intersection.OrderBy(x => x))}}}");
        Console.WriteLine($"   Union: {{{string.Join(", ", union.OrderBy(x => x))}}}");
        Console.WriteLine($"   Difference (Set1 - Set2): {{{string.Join(", ", difference.OrderBy(x => x))}}}");
        Console.WriteLine($"   Symmetric Difference: {{{string.Join(", ", symmetricDiff.OrderBy(x => x))}}}\n");
    }

    /// <summary>
    /// Python tuple and namedtuple operations
    /// Python equivalent: tuple, namedtuple from collections
    /// </summary>
    private static void TupleAndNamedTupleExample()
    {
        Console.WriteLine("4. Tuple and Named Tuple:");

        // Python: coordinates = (10, 20)
        var coordinates = (X: 10, Y: 20);
        
        // Python: person = ('Alice', 25, 'Engineer')
        var person = ("Alice", 25, "Engineer");

        // Python: namedtuple equivalent using record
        var point = new Point(10, 20);
        var employee = new Employee("Bob", 30, "Manager", 75000);

        Console.WriteLine($"   Coordinates: ({coordinates.X}, {coordinates.Y})");
        Console.WriteLine($"   Person tuple: {person}");
        Console.WriteLine($"   Point record: {point}");
        Console.WriteLine($"   Employee: {employee}");
        
        // Tuple unpacking
        var (name, age, role) = person;
        Console.WriteLine($"   Unpacked: Name={name}, Age={age}, Role={role}");

        // Record with deconstruction
        var (empName, empAge) = employee;
        Console.WriteLine($"   Employee unpacked: Name={empName}, Age={empAge}\n");
    }

    /// <summary>
    /// Python stack and queue operations
    /// Python equivalent: list as stack, collections.deque as queue
    /// </summary>
    private static void StackAndQueueExample()
    {
        Console.WriteLine("5. Stack and Queue Operations:");

        // Python: stack = []; stack.append(x); stack.pop()
        var stack = new Stack<string>();
        foreach (var item in new[] { "first", "second", "third" })
        {
            stack.Push(item);
            Console.WriteLine($"   Pushed: {item}");
        }

        Console.WriteLine($"   Stack contents: [{string.Join(", ", stack)}]");
        while (stack.Count > 0)
        {
            var item = stack.Pop();
            Console.WriteLine($"   Popped: {item}");
        }

        // Python: from collections import deque; queue = deque()
        var queue = new Queue<int>();
        foreach (var item in new[] { 1, 2, 3 })
        {
            queue.Enqueue(item);
            Console.WriteLine($"   Enqueued: {item}");
        }

        Console.WriteLine($"   Queue contents: [{string.Join(", ", queue)}]");
        while (queue.Count > 0)
        {
            var item = queue.Dequeue();
            Console.WriteLine($"   Dequeued: {item}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Python deque operations with specialized methods
    /// Python equivalent: collections.deque with appendleft, popleft
    /// </summary>
    private static void DequeOperationsExample()
    {
        Console.WriteLine("6. Deque Operations:");

        var deque = new CustomDeque<string>();
        
        // Python: deque.append(), deque.appendleft()
        deque.AddLast("middle");
        deque.AddFirst("first");
        deque.AddLast("last");

        Console.WriteLine($"   Deque after additions: [{string.Join(", ", deque)}]");

        // Python: deque.popleft(), deque.pop()
        var first = deque.RemoveFirst();
        var last = deque.RemoveLast();

        Console.WriteLine($"   Removed first: {first}");
        Console.WriteLine($"   Removed last: {last}");
        Console.WriteLine($"   Deque after removals: [{string.Join(", ", deque)}]\n");
    }

    /// <summary>
    /// Custom collection implementations
    /// Python equivalent: implementing __iter__, __len__, __contains__
    /// </summary>
    private static void CustomCollectionsExample()
    {
        Console.WriteLine("7. Custom Collections:");

        var circularBuffer = new CircularBuffer<int>(3);
        
        foreach (var item in new[] { 1, 2, 3, 4, 5 })
        {
            circularBuffer.Add(item);
            Console.WriteLine($"   Added {item}: [{string.Join(", ", circularBuffer)}]");
        }

        var lruCache = new LruCache<string, int>(2);
        lruCache["a"] = 1;
        lruCache["b"] = 2;
        Console.WriteLine($"   LRU after adding a=1, b=2: {lruCache}");
        
        lruCache["c"] = 3; // Should evict 'a'
        Console.WriteLine($"   LRU after adding c=3: {lruCache}");
        
        var _ = lruCache["b"]; // Access 'b' to make it most recent
        lruCache["d"] = 4; // Should evict 'c'
        Console.WriteLine($"   LRU after accessing b and adding d=4: {lruCache}\n");
    }

    /// <summary>
    /// Algorithm patterns commonly used with data structures
    /// Python equivalent: sorting, searching, filtering algorithms
    /// </summary>
    private static void AlgorithmPatternsExample()
    {
        Console.WriteLine("8. Algorithm Patterns:");

        var data = new[] { 64, 34, 25, 12, 22, 11, 90 };
        
        // Python: sorted(data)
        var sorted = data.OrderBy(x => x).ToArray();
        
        // Python: binary search equivalent
        var target = 25;
        var index = Array.BinarySearch(sorted, target);
        
        Console.WriteLine($"   Original: [{string.Join(", ", data)}]");
        Console.WriteLine($"   Sorted: [{string.Join(", ", sorted)}]");
        Console.WriteLine($"   Binary search for {target}: index {index}");

        // Python: filter and map combined
        var processedData = data
            .Where(x => x > 20)
            .Select(x => x * 2)
            .OrderByDescending(x => x)
            .ToArray();
        
        Console.WriteLine($"   Filtered (>20), doubled, desc sorted: [{string.Join(", ", processedData)}]");

        // Python: grouping and aggregation
        var grouped = data
            .GroupBy(x => x % 2 == 0 ? "even" : "odd")
            .ToDictionary(g => g.Key, g => new { Count = g.Count(), Sum = g.Sum(), Avg = g.Average() });

        foreach (var (type, stats) in grouped)
        {
            Console.WriteLine($"   {type} numbers: count={stats.Count}, sum={stats.Sum}, avg={stats.Avg:F1}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Performance optimization techniques
    /// Python equivalent: memory optimization, generator patterns
    /// </summary>
    private static void PerformanceOptimizationExample()
    {
        Console.WriteLine("9. Performance Optimization:");

        const int size = 100_000;
        var stopwatch = Stopwatch.StartNew();

        // Efficient bulk operations
        var largeList = Enumerable.Range(1, size).ToList();
        stopwatch.Stop();
        Console.WriteLine($"   Created list of {size:N0} items in {stopwatch.ElapsedMilliseconds}ms");

        stopwatch.Restart();
        // Python: list comprehension equivalent with lazy evaluation
        var processedData = largeList
            .Where(x => x % 2 == 0)
            .Select(x => x * 2)
            .Take(1000)  // Only take first 1000 to demonstrate lazy evaluation
            .ToArray();
        stopwatch.Stop();

        Console.WriteLine($"   Processed first 1000 even numbers in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"   First 5 results: [{string.Join(", ", processedData.Take(5))}]");

        // Memory-efficient counting without materializing collections
        stopwatch.Restart();
        var evenCount = largeList.Count(x => x % 2 == 0);
        stopwatch.Stop();
        Console.WriteLine($"   Counted {evenCount:N0} even numbers in {stopwatch.ElapsedMilliseconds}ms");

        // Parallel processing
        stopwatch.Restart();
        var parallelSum = largeList.AsParallel().Where(x => x % 2 == 0).Select(x => (long)x).Sum();
        stopwatch.Stop();
        Console.WriteLine($"   Parallel sum of even numbers: {parallelSum:N0} in {stopwatch.ElapsedMilliseconds}ms\n");
    }

    /// <summary>
    /// Memory-efficient data structures
    /// Python equivalent: generators, itertools, memory optimization
    /// </summary>
    private static void MemoryEfficientStructuresExample()
    {
        Console.WriteLine("10. Memory-Efficient Structures:");

        // Python: generator function
        static IEnumerable<int> FibonacciGenerator(int count)
        {
            var (a, b) = (0, 1);
            for (var i = 0; i < count; i++)
            {
                yield return a;
                (a, b) = (b, a + b);
            }
        }

        var fibNumbers = FibonacciGenerator(10).ToArray();
        Console.WriteLine($"   First 10 Fibonacci numbers: [{string.Join(", ", fibNumbers)}]");

        // Memory-efficient streaming processing
        static IEnumerable<string> ProcessLargeDataset()
        {
            for (var i = 1; i <= 5; i++)
            {
                // Simulate processing one item at a time
                var result = $"Processed item {i}";
                Console.WriteLine($"     Generated: {result}");
                yield return result;
            }
        }

        Console.WriteLine("   Streaming data processing:");
        var processedItems = ProcessLargeDataset()
            .Where(item => item.Contains("2") || item.Contains("4"))
            .ToArray();
        Console.WriteLine($"   Filtered results: [{string.Join(", ", processedItems)}]");

        // Concurrent collections for thread-safe operations
        var concurrentDict = new ConcurrentDictionary<string, int>();
        Parallel.For(0, 5, i => 
        {
            var key = $"key{i}";
            concurrentDict.AddOrUpdate(key, 1, (k, v) => v + 1);
        });

        Console.WriteLine("   Concurrent dictionary results:");
        foreach (var (key, value) in concurrentDict.OrderBy(kvp => kvp.Key))
        {
            Console.WriteLine($"     {key}: {value}");
        }
    }
}

// Custom record types (Python namedtuple equivalent)
public record Point(int X, int Y);
public record Employee(string Name, int Age, string Role, decimal Salary)
{
    public void Deconstruct(out string name, out int age)
    {
        name = Name;
        age = Age;
    }
}

// Custom deque implementation
public class CustomDeque<T> : IEnumerable<T>
{
    private readonly LinkedList<T> items = new();

    public void AddFirst(T item) => items.AddFirst(item);
    public void AddLast(T item) => items.AddLast(item);
    public T RemoveFirst() 
    {
        var first = items.First;
        if (first == null) throw new InvalidOperationException("Deque is empty");
        items.RemoveFirst();
        return first.Value;
    }
    
    public T RemoveLast() 
    {
        var last = items.Last;
        if (last == null) throw new InvalidOperationException("Deque is empty");
        items.RemoveLast();
        return last.Value;
    }
    
    public int Count => items.Count;
    public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// Circular buffer implementation
public class CircularBuffer<T> : IEnumerable<T>
{
    private readonly T[] buffer;
    private int head;
    private int count;

    public CircularBuffer(int capacity)
    {
        buffer = new T[capacity];
    }

    public void Add(T item)
    {
        buffer[head] = item;
        head = (head + 1) % buffer.Length;
        if (count < buffer.Length) count++;
    }

    public IEnumerator<T> GetEnumerator()
    {
        var start = count < buffer.Length ? 0 : head;
        for (var i = 0; i < count; i++)
        {
            yield return buffer[(start + i) % buffer.Length];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// LRU Cache implementation
public class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> cache = new();
    private readonly LinkedList<(TKey Key, TValue Value)> order = new();
    private readonly int capacity;

    public LruCache(int capacity)
    {
        this.capacity = capacity;
    }

    public TValue this[TKey key]
    {
        get
        {
            if (!cache.TryGetValue(key, out var node))
                throw new KeyNotFoundException($"Key '{key}' not found");
            
            // Move to front (most recently used)
            order.Remove(node);
            order.AddFirst(node);
            return node.Value.Value;
        }
        set
        {
            if (cache.TryGetValue(key, out var existingNode))
            {
                // Update existing
                existingNode.Value = (key, value);
                order.Remove(existingNode);
                order.AddFirst(existingNode);
            }
            else
            {
                // Add new
                if (cache.Count >= capacity)
                {
                    // Remove least recently used
                    var lru = order.Last!;
                    cache.Remove(lru.Value.Key);
                    order.RemoveLast();
                }
                
                var newNode = order.AddFirst((key, value));
                cache[key] = newNode;
            }
        }
    }

    public override string ToString()
    {
        var items = order.Select(item => $"{item.Key}={item.Value}");
        return $"LRU[{string.Join(", ", items)}]";
    }
}