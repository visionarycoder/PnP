# Iterator Pattern

**Description**: Provides a way to access elements of a collection sequentially without exposing the underlying representation. The pattern defines a common interface for traversing different data structures while keeping the traversal logic separate from the collection implementation.

**Language/Technology**: C#

**Code**:

## 1. Basic Iterator Interface

```csharp
// Generic iterator interface
public interface IIterator<T>
{
    bool HasNext();
    T Next();
    void Reset();
    T Current { get; }
}

// Iterable interface
public interface IIterable<T>
{
    IIterator<T> CreateIterator();
}

// Basic iterator implementation
public abstract class Iterator<T> : IIterator<T>
{
    protected int position = -1;
    protected readonly IList<T> items;
    
    protected Iterator(IList<T> items)
    {
        items = items ?? throw new ArgumentNullException(nameof(items));
    }
    
    public virtual bool HasNext()
    {
        return position + 1 < items.Count;
    }
    
    public virtual T Next()
    {
        if (!HasNext())
        {
            throw new InvalidOperationException("No more elements");
        }
        
        _position++;
        return _items[_position];
    }
    
    public virtual void Reset()
    {
        position = -1;
    }
    
    public virtual T Current
    {
        get
        {
            if (position < 0 || position >= items.Count)
            {
                throw new InvalidOperationException("Iterator is not positioned on a valid element");
            }
            return _items[_position];
        }
    }
}

// Forward iterator
public class ForwardIterator<T> : Iterator<T>
{
    public ForwardIterator(IList<T> items) : base(items) { }
}

// Reverse iterator
public class ReverseIterator<T> : Iterator<T>
{
    public ReverseIterator(IList<T> items) : base(items)
    {
        position = items.Count;
    }
    
    public override bool HasNext()
    {
        return position - 1 >= 0;
    }
    
    public override T Next()
    {
        if (!HasNext())
        {
            throw new InvalidOperationException("No more elements");
        }
        
        _position--;
        return _items[_position];
    }
    
    public override void Reset()
    {
        position = items.Count;
    }
}

// Skip iterator (every nth element)
public class SkipIterator<T> : Iterator<T>
{
    private readonly int skipCount;
    
    public SkipIterator(IList<T> items, int skipCount) : base(items)
    {
        skipCount = skipCount > 0 ? skipCount : 1;
    }
    
    public override T Next()
    {
        if (!HasNext())
        {
            throw new InvalidOperationException("No more elements");
        }
        
        position += skipCount;
        return _items[_position];
    }
    
    public override bool HasNext()
    {
        return position + skipCount < items.Count;
    }
}
```

## 2. Custom Collection with Multiple Iterators

```csharp
// Custom collection class
public class CustomCollection<T> : IIterable<T>, IEnumerable<T>
{
    private readonly List<T> items = new();
    
    public int Count => items.Count;
    public bool IsEmpty => items.Count == 0;
    
    public void Add(T item)
    {
        items.Add(item);
    }
    
    public void AddRange(IEnumerable<T> items)
    {
        items.AddRange(items);
    }
    
    public bool Remove(T item)
    {
        return items.Remove(item);
    }
    
    public void Clear()
    {
        items.Clear();
    }
    
    public T this[int index]
    {
        get => _items[index];
        set => _items[index] = value;
    }
    
    // Multiple iterator creation methods
    public IIterator<T> CreateIterator()
    {
        return new ForwardIterator<T>(items);
    }
    
    public IIterator<T> CreateReverseIterator()
    {
        return new ReverseIterator<T>(items);
    }
    
    public IIterator<T> CreateSkipIterator(int skipCount = 2)
    {
        return new SkipIterator<T>(items, skipCount);
    }
    
    public IIterator<T> CreateFilterIterator(Func<T, bool> predicate)
    {
        return new FilterIterator<T>(items, predicate);
    }
    
    public IIterator<TResult> CreateTransformIterator<TResult>(Func<T, TResult> transform)
    {
        return new TransformIterator<T, TResult>(items, transform);
    }
    
    // IEnumerable implementation for C# foreach support
    public IEnumerator<T> GetEnumerator()
    {
        return items.GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    // Yield-based iterator methods
    public IEnumerable<T> Forward()
    {
        for (int i = 0; i < items.Count; i++)
        {
            yield return _items[i];
        }
    }
    
    public IEnumerable<T> Reverse()
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            yield return _items[i];
        }
    }
    
    public IEnumerable<T> Skip(int count = 2)
    {
        for (int i = 0; i < items.Count; i += count)
        {
            yield return _items[i];
        }
    }
    
    public IEnumerable<T> Where(Func<T, bool> predicate)
    {
        foreach (var item in items)
        {
            if (predicate(item))
            {
                yield return item;
            }
        }
    }
    
    public IEnumerable<TResult> Select<TResult>(Func<T, TResult> selector)
    {
        foreach (var item in items)
        {
            yield return selector(item);
        }
    }
    
    public override string ToString()
    {
        return $"CustomCollection<{typeof(T).Name}>({Count} items)";
    }
}

// Advanced iterators
public class FilterIterator<T> : IIterator<T>
{
    private readonly IList<T> items;
    private readonly Func<T, bool> predicate;
    private int position = -1;
    private T current = default(T)!;
    
    public FilterIterator(IList<T> items, Func<T, bool> predicate)
    {
        items = items ?? throw new ArgumentNullException(nameof(items));
        predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }
    
    public bool HasNext()
    {
        for (int i = position + 1; i < items.Count; i++)
        {
            if (predicate(_items[i]))
            {
                return true;
            }
        }
        return false;
    }
    
    public T Next()
    {
        for (int i = position + 1; i < items.Count; i++)
        {
            if (predicate(_items[i]))
            {
                position = i;
                current = _items[i];
                return current;
            }
        }
        throw new InvalidOperationException("No more elements matching the filter");
    }
    
    public void Reset()
    {
        position = -1;
        current = default(T)!;
    }
    
    public T Current => current;
}

public class TransformIterator<TSource, TResult> : IIterator<TResult>
{
    private readonly IList<TSource> items;
    private readonly Func<TSource, TResult> transform;
    private int position = -1;
    
    public TransformIterator(IList<TSource> items, Func<TSource, TResult> transform)
    {
        items = items ?? throw new ArgumentNullException(nameof(items));
        transform = transform ?? throw new ArgumentNullException(nameof(transform));
    }
    
    public bool HasNext()
    {
        return position + 1 < items.Count;
    }
    
    public TResult Next()
    {
        if (!HasNext())
        {
            throw new InvalidOperationException("No more elements");
        }
        
        _position++;
        return transform(_items[_position]);
    }
    
    public void Reset()
    {
        position = -1;
    }
    
    public TResult Current
    {
        get
        {
            if (position < 0 || position >= items.Count)
            {
                throw new InvalidOperationException("Iterator is not positioned on a valid element");
            }
            return transform(_items[_position]);
        }
    }
}
```

## 3. Tree Traversal Iterators

```csharp
// Tree node for demonstration
public class TreeNode<T>
{
    public T Value { get; set; }
    public List<TreeNode<T>> Children { get; set; } = new();
    public TreeNode<T>? Parent { get; set; }
    
    public TreeNode(T value)
    {
        Value = value;
    }
    
    public void AddChild(TreeNode<T> child)
    {
        child.Parent = this;
        Children.Add(child);
    }
    
    public void AddChild(T value)
    {
        AddChild(new TreeNode<T>(value));
    }
    
    public bool IsLeaf => Children.Count == 0;
    public bool IsRoot => Parent == null;
    
    public override string ToString() => $"TreeNode({Value})";
}

// Tree collection with multiple traversal strategies
public class Tree<T> : IEnumerable<T>
{
    public TreeNode<T>? Root { get; set; }
    
    public Tree(T rootValue)
    {
        Root = new TreeNode<T>(rootValue);
    }
    
    public Tree(TreeNode<T> root)
    {
        Root = root;
    }
    
    // Default enumeration (breadth-first)
    public IEnumerator<T> GetEnumerator()
    {
        return BreadthFirstTraversal().GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    // Breadth-First (Level Order) Traversal
    public IEnumerable<T> BreadthFirstTraversal()
    {
        if (Root == null) yield break;
        
        var queue = new Queue<TreeNode<T>>();
        queue.Enqueue(Root);
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current.Value;
            
            foreach (var child in current.Children)
            {
                queue.Enqueue(child);
            }
        }
    }
    
    // Depth-First (Pre-order) Traversal
    public IEnumerable<T> DepthFirstPreOrder()
    {
        return DepthFirstPreOrderRecursive(Root);
    }
    
    private IEnumerable<T> DepthFirstPreOrderRecursive(TreeNode<T>? node)
    {
        if (node == null) yield break;
        
        yield return node.Value;
        
        foreach (var child in node.Children)
        {
            foreach (var value in DepthFirstPreOrderRecursive(child))
            {
                yield return value;
            }
        }
    }
    
    // Depth-First (Post-order) Traversal
    public IEnumerable<T> DepthFirstPostOrder()
    {
        return DepthFirstPostOrderRecursive(Root);
    }
    
    private IEnumerable<T> DepthFirstPostOrderRecursive(TreeNode<T>? node)
    {
        if (node == null) yield break;
        
        foreach (var child in node.Children)
        {
            foreach (var value in DepthFirstPostOrderRecursive(child))
            {
                yield return value;
            }
        }
        
        yield return node.Value;
    }
    
    // Iterative Depth-First using Stack
    public IEnumerable<T> DepthFirstIterative()
    {
        if (Root == null) yield break;
        
        var stack = new Stack<TreeNode<T>>();
        stack.Push(Root);
        
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current.Value;
            
            // Push children in reverse order to maintain left-to-right traversal
            for (int i = current.Children.Count - 1; i >= 0; i--)
            {
                stack.Push(current.Children[i]);
            }
        }
    }
    
    // Leaves-only traversal
    public IEnumerable<T> LeavesOnly()
    {
        foreach (var node in GetAllNodes())
        {
            if (node.IsLeaf)
            {
                yield return node.Value;
            }
        }
    }
    
    // Level-by-level traversal
    public IEnumerable<IEnumerable<T>> TraverseByLevels()
    {
        if (Root == null) yield break;
        
        var currentLevel = new List<TreeNode<T>> { Root };
        
        while (currentLevel.Any())
        {
            yield return currentLevel.Select(node => node.Value);
            
            var nextLevel = new List<TreeNode<T>>();
            foreach (var node in currentLevel)
            {
                nextLevel.AddRange(node.Children);
            }
            currentLevel = nextLevel;
        }
    }
    
    private IEnumerable<TreeNode<T>> GetAllNodes()
    {
        if (Root == null) yield break;
        
        var queue = new Queue<TreeNode<T>>();
        queue.Enqueue(Root);
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;
            
            foreach (var child in current.Children)
            {
                queue.Enqueue(child);
            }
        }
    }
}
```

## 4. Async Iterator Support

```csharp
// Async iterator for data streaming
public class AsyncDataStream<T> : IAsyncEnumerable<T>
{
    private readonly IEnumerable<T> data;
    private readonly TimeSpan delay;
    private readonly int batchSize;
    
    public AsyncDataStream(IEnumerable<T> data, TimeSpan? delay = null, int batchSize = 1)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));
        delay = delay ?? TimeSpan.FromMilliseconds(100);
        batchSize = Math.Max(1, batchSize);
    }
    
    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        int count = 0;
        
        foreach (var item in data)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            
            yield return item;
            count++;
            
            // Add delay every batch
            if (count % batchSize == 0)
            {
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
    
    // Async LINQ-like operations
    public async IAsyncEnumerable<TResult> SelectAsync<TResult>(Func<T, Task<TResult>> selector,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in this.WithCancellation(cancellationToken))
        {
            yield return await selector(item);
        }
    }
    
    public async IAsyncEnumerable<T> WhereAsync(Func<T, Task<bool>> predicate,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in this.WithCancellation(cancellationToken))
        {
            if (await predicate(item))
            {
                yield return item;
            }
        }
    }
    
    public async Task<List<T>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<T>();
        
        await foreach (var item in this.WithCancellation(cancellationToken))
        {
            result.Add(item);
        }
        
        return result;
    }
    
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        int count = 0;
        
        await foreach (var _ in this.WithCancellation(cancellationToken))
        {
            count++;
        }
        
        return count;
    }
}

// Async file line reader
public static class AsyncFileIterator
{
    public static async IAsyncEnumerable<string> ReadLinesAsync(string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(filePath);
        
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var line = await reader.ReadLineAsync();
            if (line != null)
            {
                yield return line;
            }
        }
    }
    
    public static async IAsyncEnumerable<string> ReadLinesWithNumbersAsync(string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int lineNumber = 1;
        
        await foreach (var line in ReadLinesAsync(filePath, cancellationToken))
        {
            yield return $"{lineNumber:D4}: {line}";
            lineNumber++;
        }
    }
}

// Async web data iterator
public class AsyncWebDataIterator
{
    private readonly HttpClient httpClient;
    
    public AsyncWebDataIterator(HttpClient? httpClient = null)
    {
        httpClient = httpClient ?? new HttpClient();
    }
    
    public async IAsyncEnumerable<string> FetchUrlsAsync(IEnumerable<string> urls,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var url in urls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var response = await httpClient.GetStringAsync(url, cancellationToken);
                yield return response;
            }
            catch (Exception ex)
            {
                yield return $"Error fetching {url}: {ex.Message}";
            }
        }
    }
    
    public async IAsyncEnumerable<TResult> FetchAndProcessAsync<TResult>(
        IEnumerable<string> urls,
        Func<string, string, TResult> processor,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var url in urls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var content = await httpClient.GetStringAsync(url, cancellationToken);
                yield return processor(url, content);
            }
            catch (Exception ex)
            {
                yield return processor(url, $"Error: {ex.Message}");
            }
        }
    }
}
```

## 5. LINQ-Style Iterator Extensions

```csharp
// Extension methods for enhanced iteration
public static class IteratorExtensions
{
    // Batch processing
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)
    {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        
        var batch = new List<T>(size);
        
        foreach (var item in source)
        {
            batch.Add(item);
            
            if (batch.Count == size)
            {
                yield return batch;
                batch = new List<T>(size);
            }
        }
        
        if (batch.Count > 0)
        {
            yield return batch;
        }
    }
    
    // Window sliding
    public static IEnumerable<IEnumerable<T>> Window<T>(this IEnumerable<T> source, int size)
    {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        
        var window = new Queue<T>(size);
        
        foreach (var item in source)
        {
            window.Enqueue(item);
            
            if (window.Count > size)
            {
                window.Dequeue();
            }
            
            if (window.Count == size)
            {
                yield return window.ToArray();
            }
        }
    }
    
    // Circular iteration
    public static IEnumerable<T> Cycle<T>(this IEnumerable<T> source)
    {
        var items = source.ToList();
        if (!items.Any()) yield break;
        
        while (true)
        {
            foreach (var item in items)
            {
                yield return item;
            }
        }
    }
    
    // Take while with index
    public static IEnumerable<T> TakeWhile<T>(this IEnumerable<T> source, Func<T, int, bool> predicate)
    {
        int index = 0;
        foreach (var item in source)
        {
            if (!predicate(item, index))
                yield break;
            
            yield return item;
            index++;
        }
    }
    
    // Scan (like Aggregate but returns intermediate results)
    public static IEnumerable<TResult> Scan<T, TResult>(this IEnumerable<T> source, TResult seed, Func<TResult, T, TResult> accumulator)
    {
        var current = seed;
        yield return current;
        
        foreach (var item in source)
        {
            current = accumulator(current, item);
            yield return current;
        }
    }
    
    // Interleave multiple sequences
    public static IEnumerable<T> Interleave<T>(params IEnumerable<T>[] sources)
    {
        var enumerators = sources.Select(s => s.GetEnumerator()).ToArray();
        
        try
        {
            bool hasMore = true;
            
            while (hasMore)
            {
                hasMore = false;
                
                foreach (var enumerator in enumerators)
                {
                    if (enumerator.MoveNext())
                    {
                        yield return enumerator.Current;
                        hasMore = true;
                    }
                }
            }
        }
        finally
        {
            foreach (var enumerator in enumerators)
            {
                enumerator.Dispose();
            }
        }
    }
    
    // Pairwise iteration
    public static IEnumerable<(T Previous, T Current)> Pairwise<T>(this IEnumerable<T> source)
    {
        using var enumerator = source.GetEnumerator();
        
        if (!enumerator.MoveNext()) yield break;
        
        var previous = enumerator.Current;
        
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            yield return (previous, current);
            previous = current;
        }
    }
    
    // Index with item
    public static IEnumerable<(int Index, T Item)> WithIndex<T>(this IEnumerable<T> source)
    {
        int index = 0;
        foreach (var item in source)
        {
            yield return (index++, item);
        }
    }
}

// Performance monitoring iterator
public class MonitoredIterator<T> : IEnumerable<T>
{
    private readonly IEnumerable<T> source;
    private readonly Action<IterationStats>? onStats;
    
    public MonitoredIterator(IEnumerable<T> source, Action<IterationStats>? onStats = null)
    {
        source = source ?? throw new ArgumentNullException(nameof(source));
        onStats = onStats;
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        var stats = new IterationStats();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            foreach (var item in source)
            {
                stats.ItemsProcessed++;
                yield return item;
            }
        }
        finally
        {
            stopwatch.Stop();
            stats.TotalTime = stopwatch.Elapsed;
            onStats?.Invoke(stats);
        }
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class IterationStats
{
    public long ItemsProcessed { get; set; }
    public TimeSpan TotalTime { get; set; }
    public double ItemsPerSecond => TotalTime.TotalSeconds > 0 ? ItemsProcessed / TotalTime.TotalSeconds : 0;
    
    public override string ToString()
    {
        return $"Processed {ItemsProcessed} items in {TotalTime.TotalMilliseconds:F2}ms ({ItemsPerSecond:F2} items/sec)";
    }
}
```

**Usage**:

```csharp
// 1. Basic Iterator Usage
var collection = new CustomCollection<int>();
collection.AddRange(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

Console.WriteLine("Forward iteration:");
var forwardIterator = collection.CreateIterator();
while (forwardIterator.HasNext())
{
    Console.Write($"{forwardIterator.Next()} ");
}
Console.WriteLine();

Console.WriteLine("Reverse iteration:");
var reverseIterator = collection.CreateReverseIterator();
while (reverseIterator.HasNext())
{
    Console.Write($"{reverseIterator.Next()} ");
}
Console.WriteLine();

Console.WriteLine("Skip iteration (every 3rd):");
var skipIterator = collection.CreateSkipIterator(3);
while (skipIterator.HasNext())
{
    Console.Write($"{skipIterator.Next()} ");
}
Console.WriteLine();

Console.WriteLine("Filter iteration (even numbers):");
var filterIterator = collection.CreateFilterIterator(x => x % 2 == 0);
while (filterIterator.HasNext())
{
    Console.Write($"{filterIterator.Next()} ");
}
Console.WriteLine();

Console.WriteLine("Transform iteration (square numbers):");
var transformIterator = collection.CreateTransformIterator(x => x * x);
while (transformIterator.HasNext())
{
    Console.Write($"{transformIterator.Next()} ");
}
Console.WriteLine();

// 2. Yield-based iteration
Console.WriteLine("\nYield-based iterations:");
Console.WriteLine($"Forward: [{string.Join(", ", collection.Forward())}]");
Console.WriteLine($"Reverse: [{string.Join(", ", collection.Reverse())}]");
Console.WriteLine($"Skip(2): [{string.Join(", ", collection.Skip(2))}]");
Console.WriteLine($"Where(>5): [{string.Join(", ", collection.Where(x => x > 5))}]");
Console.WriteLine($"Select(x²): [{string.Join(", ", collection.Select(x => x * x))}]");

// 3. Tree traversal
var tree = new Tree<string>("Root");
tree.Root!.AddChild("Child1");
tree.Root.AddChild("Child2");
tree.Root.AddChild("Child3");
tree.Root.Children[0].AddChild("Grandchild1");
tree.Root.Children[0].AddChild("Grandchild2");
tree.Root.Children[1].AddChild("Grandchild3");

Console.WriteLine("\nTree traversals:");
Console.WriteLine($"Breadth-First: [{string.Join(", ", tree.BreadthFirstTraversal())}]");
Console.WriteLine($"Depth-First Pre: [{string.Join(", ", tree.DepthFirstPreOrder())}]");
Console.WriteLine($"Depth-First Post: [{string.Join(", ", tree.DepthFirstPostOrder())}]");
Console.WriteLine($"Iterative DFS: [{string.Join(", ", tree.DepthFirstIterative())}]");
Console.WriteLine($"Leaves Only: [{string.Join(", ", tree.LeavesOnly())}]");

Console.WriteLine("Level-by-level:");
foreach (var level in tree.TraverseByLevels())
{
    Console.WriteLine($"  Level: [{string.Join(", ", level)}]");
}

// 4. Async iteration
async Task DemonstrateAsyncIteration()
{
    var data = Enumerable.Range(1, 10);
    var asyncStream = new AsyncDataStream<int>(data, TimeSpan.FromMilliseconds(50));
    
    Console.WriteLine("\nAsync iteration:");
    await foreach (var item in asyncStream)
    {
        Console.Write($"{item} ");
    }
    Console.WriteLine();
    
    // Async transformations
    var squares = asyncStream.SelectAsync(async x => 
    {
        await Task.Delay(10); // Simulate async work
        return x * x;
    });
    
    Console.WriteLine("Async squares:");
    await foreach (var square in squares)
    {
        Console.Write($"{square} ");
    }
    Console.WriteLine();
}

// 5. LINQ-style extensions
var numbers = Enumerable.Range(1, 20);

Console.WriteLine("\nLINQ-style extensions:");
Console.WriteLine("Batches of 5:");
foreach (var batch in numbers.Batch(5))
{
    Console.WriteLine($"  [{string.Join(", ", batch)}]");
}

Console.WriteLine("Sliding window of 3:");
foreach (var window in numbers.Take(8).Window(3))
{
    Console.WriteLine($"  [{string.Join(", ", window)}]");
}

Console.WriteLine("Scan (running sum):");
var runningSums = numbers.Take(5).Scan(0, (acc, x) => acc + x);
Console.WriteLine($"  [{string.Join(", ", runningSums)}]");

Console.WriteLine("Pairwise:");
var pairs = numbers.Take(5).Pairwise();
Console.WriteLine($"  [{string.Join(", ", pairs.Select(p => $"({p.Previous},{p.Current})"))}]");

Console.WriteLine("With Index:");
var indexed = new[] { "apple", "banana", "cherry" }.WithIndex();
Console.WriteLine($"  [{string.Join(", ", indexed.Select(i => $"{i.Index}:{i.Item}"))}]");

// 6. Performance monitoring
var monitoredData = Enumerable.Range(1, 1000000);
var monitored = new MonitoredIterator<int>(monitoredData, stats => 
{
    Console.WriteLine($"\nIteration completed: {stats}");
});

var processedCount = monitored.Where(x => x % 1000 == 0).Count();
Console.WriteLine($"Processed {processedCount} items");

await DemonstrateAsyncIteration();

// Expected output demonstrates:
// - Multiple iteration strategies over same data structure
// - Tree traversal with different algorithms
// - Async iteration with proper cancellation support
// - LINQ-style operations using yield statements
// - Performance monitoring and statistics collection
// - Type-safe iteration without exposing internal structure
```

**Notes**:

- **Encapsulation**: Iterator pattern hides the internal structure of collections while providing uniform access
- **Multiple Traversals**: Same collection can support different iteration strategies (forward, reverse, filtered, etc.)
- **Lazy Evaluation**: Yield statements enable lazy evaluation and memory-efficient iteration
- **Async Support**: Modern C# async iterators support asynchronous data processing with proper cancellation
- **LINQ Integration**: Custom iterators integrate seamlessly with LINQ and foreach loops
- **Performance**: Iterator pattern can provide better memory usage for large datasets through lazy evaluation
- **Flexibility**: Easy to add new iteration strategies without modifying existing collection classes
- **Thread Safety**: Consider thread safety requirements when implementing iterators for concurrent access

**Prerequisites**:

- .NET 5.0 or later for async enumerable support and modern C# features
- Understanding of yield statements and IEnumerable/IEnumerator interfaces
- Knowledge of LINQ and extension methods
- Familiarity with async/await patterns for async iterators

**Related Patterns**:

- **Composite**: Iterator often used to traverse Composite structures like trees
- **Factory Method**: Iterator creation can use factory methods for different traversal strategies  
- **Command**: Iterator operations can be encapsulated as commands
- **Memento**: Iterator state can be captured and restored using Memento pattern
