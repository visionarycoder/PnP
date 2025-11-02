using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace CSharp.ConcurrentCollections;

/// <summary>
/// Demonstrates various concurrent collection patterns and performance characteristics.
/// </summary>
public class ConcurrentCollectionsDemo
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Concurrent Collections Demonstrations ===\n");

        await DemoLockFreeStack();
        Console.WriteLine();

        await DemoLockFreeQueue();
        Console.WriteLine();

        await DemoBoundedBuffer();
        Console.WriteLine();

        DemoAtomicCounter();
        Console.WriteLine();

        await DemoConcurrentObjectPool();
        Console.WriteLine();

        DemoSPSCRingBuffer();
        Console.WriteLine();

        DemoConcurrentHashMap();
        Console.WriteLine();

        DemoConcurrentPriorityQueue();
        Console.WriteLine();

        DemoConcurrentLRUCache();
        Console.WriteLine();

        await DemoBuiltInConcurrentCollections();
        Console.WriteLine();

        await DemoPerformanceComparison();
    }

    private static async Task DemoLockFreeStack()
    {
        Console.WriteLine("--- Lock-Free Stack Demo ---");

        var stack = new LockFreeStack<string>();
        var tasks = new List<Task>();

        // Concurrent push operations
        for (int i = 0; i < 5; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    stack.Push($"Task{taskId}-Item{j}");
                    Console.WriteLine($"Task {taskId} pushed: Task{taskId}-Item{j}");
                }
            }));
        }

        await Task.WhenAll(tasks);
        tasks.Clear();

        Console.WriteLine($"Stack count after pushes: {stack.Count}");
        Console.WriteLine($"Is empty: {stack.IsEmpty}");

        // Concurrent pop operations
        var poppedItems = new ConcurrentBag<string>();
        
        for (int i = 0; i < 3; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (stack.TryPop(out var item))
                {
                    if (item != null)
                    {
                        poppedItems.Add(item);
                        Console.WriteLine($"Popped: {item}");
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        Console.WriteLine($"Total items popped: {poppedItems.Count}");
        Console.WriteLine($"Final stack count: {stack.Count}");
        Console.WriteLine($"Is empty: {stack.IsEmpty}");
    }

    private static void DemoConcurrentPriorityQueue()
    {
        Console.WriteLine("--- Concurrent Priority Queue Demo ---");

        var priorityQueue = new ConcurrentPriorityQueue<int>();

        // Add items with different priorities
        var random = new Random();
        var items = new List<int>();

        for (int i = 0; i < 15; i++)
        {
            int value = random.Next(1, 100);
            items.Add(value);
            priorityQueue.Enqueue(value);
            Console.WriteLine($"Enqueued: {value}");
        }

        Console.WriteLine($"\nQueue count: {priorityQueue.Count}");
        Console.WriteLine($"Items added: [{string.Join(", ", items)}]");

        // Dequeue items (should come out in priority order - highest first)
        Console.WriteLine("\nDequeuing items (highest priority first):");
        var dequeuedItems = new List<int>();

        while (priorityQueue.TryDequeue(out var item))
        {
            dequeuedItems.Add(item);
            Console.WriteLine($"Dequeued: {item}");
        }

        Console.WriteLine($"Dequeued items: [{string.Join(", ", dequeuedItems)}]");
        Console.WriteLine($"Items are in descending order: {IsDescendingOrder(dequeuedItems)}");
        Console.WriteLine($"Final queue count: {priorityQueue.Count}");
    }

    private static void DemoConcurrentLRUCache()
    {
        Console.WriteLine("--- Concurrent LRU Cache Demo ---");

        var cache = new ConcurrentLRUCache<string, string>(5);

        // Add items to cache
        var items = new[] { "A", "B", "C", "D", "E", "F", "G" };

        Console.WriteLine("Adding items to cache (capacity: 5):");
        foreach (var item in items)
        {
            cache.AddOrUpdate(item, $"Value-{item}");
            Console.WriteLine($"Added: {item} -> Value-{item}, Count: {cache.Count}");
        }

        Console.WriteLine("\nAccessing items (will affect LRU order):");
        
        // Access some items to change LRU order
        if (cache.TryGetValue("C", out var valueC))
            Console.WriteLine($"Accessed C: {valueC}");

        if (cache.TryGetValue("E", out var valueE))
            Console.WriteLine($"Accessed E: {valueE}");

        // Try to access evicted items
        if (!cache.TryGetValue("A", out var valueA))
            Console.WriteLine("A was evicted from cache");

        if (!cache.TryGetValue("B", out var valueB))
            Console.WriteLine("B was evicted from cache");

        Console.WriteLine($"\nFinal cache count: {cache.Count}");

        // Add one more item to trigger another eviction
        cache.AddOrUpdate("H", "Value-H");
        Console.WriteLine("Added H -> Value-H");

        // Check which items remain
        Console.WriteLine("\nRemaining items in cache:");
        foreach (var key in new[] { "C", "D", "E", "F", "G", "H" })
        {
            if (cache.TryGetValue(key, out var value))
                Console.WriteLine($"{key}: {value}");
            else
                Console.WriteLine($"{key}: (evicted)");
        }

        cache.Dispose();
    }

    private static async Task DemoBuiltInConcurrentCollections()
    {
        Console.WriteLine("--- Built-in Concurrent Collections Demo ---");

        // ConcurrentDictionary
        var concurrentDict = new ConcurrentDictionary<int, string>();
        var tasks = new List<Task>();

        Console.WriteLine("ConcurrentDictionary operations:");

        // Concurrent additions
        for (int i = 0; i < 5; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    var key = taskId * 10 + j;
                    var value = $"Value-{key}";
                    concurrentDict.TryAdd(key, value);
                }
            }));
        }

        await Task.WhenAll(tasks);
        Console.WriteLine($"ConcurrentDictionary count: {concurrentDict.Count}");

        // ConcurrentQueue
        var concurrentQueue = new ConcurrentQueue<int>();
        
        Console.WriteLine("\nConcurrentQueue operations:");
        
        // Enqueue items
        for (int i = 0; i < 20; i++)
        {
            concurrentQueue.Enqueue(i);
        }

        Console.WriteLine($"Enqueued 20 items, count: {concurrentQueue.Count}");

        // Dequeue half
        var dequeued = 0;
        while (dequeued < 10 && concurrentQueue.TryDequeue(out var item))
        {
            Console.WriteLine($"Dequeued: {item}");
            dequeued++;
        }

        Console.WriteLine($"Remaining in queue: {concurrentQueue.Count}");

        // ConcurrentBag
        var concurrentBag = new ConcurrentBag<string>();
        tasks.Clear();

        Console.WriteLine("\nConcurrentBag operations:");

        for (int i = 0; i < 3; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 5; j++)
                {
                    concurrentBag.Add($"Task{taskId}-Item{j}");
                }
            }));
        }

        await Task.WhenAll(tasks);
        Console.WriteLine($"ConcurrentBag count: {concurrentBag.Count}");
        Console.WriteLine($"Items: [{string.Join(", ", concurrentBag)}]");
    }

    private static async Task DemoPerformanceComparison()
    {
        Console.WriteLine("--- Performance Comparison ---");

        const int iterations = 100000;
        var threadCount = Environment.ProcessorCount;

        // Test lock-free stack vs ConcurrentStack
        Console.WriteLine($"Testing with {iterations} operations across {threadCount} threads:");

        var lockFreeStack = new LockFreeStack<int>();
        var concurrentStack = new ConcurrentStack<int>();

        // Lock-free stack performance
        var sw = Stopwatch.StartNew();
        var tasks = new List<Task>();

        for (int t = 0; t < threadCount; t++)
        {
            int threadId = t;
            tasks.Add(Task.Run(() =>
            {
                var startRange = threadId * (iterations / threadCount);
                var endRange = (threadId + 1) * (iterations / threadCount);

                for (int i = startRange; i < endRange; i++)
                {
                    lockFreeStack.Push(i);
                }

                // Pop half of what we pushed
                var popsNeeded = (endRange - startRange) / 2;
                for (int i = 0; i < popsNeeded; i++)
                {
                    lockFreeStack.TryPop(out var _);
                }
            }));
        }

        await Task.WhenAll(tasks);
        sw.Stop();
        var lockFreeTime = sw.ElapsedMilliseconds;

        Console.WriteLine($"Lock-free stack: {lockFreeTime}ms, Final count: {lockFreeStack.Count}");

        // ConcurrentStack performance
        sw.Restart();
        tasks.Clear();

        for (int t = 0; t < threadCount; t++)
        {
            int threadId = t;
            tasks.Add(Task.Run(() =>
            {
                var startRange = threadId * (iterations / threadCount);
                var endRange = (threadId + 1) * (iterations / threadCount);

                for (int i = startRange; i < endRange; i++)
                {
                    concurrentStack.Push(i);
                }

                // Pop half of what we pushed
                var popsNeeded = (endRange - startRange) / 2;
                for (int i = 0; i < popsNeeded; i++)
                {
                    concurrentStack.TryPop(out var _);
                }
            }));
        }

        await Task.WhenAll(tasks);
        sw.Stop();
        var concurrentStackTime = sw.ElapsedMilliseconds;

        Console.WriteLine($"ConcurrentStack: {concurrentStackTime}ms, Final count: {concurrentStack.Count}");

        var performanceRatio = (double)concurrentStackTime / lockFreeTime;
        Console.WriteLine($"Performance ratio (ConcurrentStack/LockFree): {performanceRatio:F2}x");

        if (performanceRatio > 1)
            Console.WriteLine("Lock-free stack is faster");
        else if (performanceRatio < 1)
            Console.WriteLine("ConcurrentStack is faster");
        else
            Console.WriteLine("Performance is similar");
    }

    private static async Task DemoLockFreeQueue()
    {
        Console.WriteLine("--- Lock-Free Queue Demo ---");

        var queue = new LockFreeQueue<string>();
        var tasks = new List<Task>();

        // Producer tasks
        for (int i = 0; i < 3; i++)
        {
            int producerId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 5; j++)
                {
                    var item = $"Producer{producerId}-Item{j}";
                    queue.Enqueue(item);
                    Console.WriteLine($"Enqueued: {item}");
                }
            }));
        }

        // Consumer task
        tasks.Add(Task.Run(async () =>
        {
            var consumedItems = new List<string>();
            
            while (consumedItems.Count < 15)
            {
                if (queue.TryDequeue(out var item) && item != null)
                {
                    consumedItems.Add(item);
                    Console.WriteLine($"Dequeued: {item}");
                }
                await Task.Delay(10);
            }
        }));

        await Task.WhenAll(tasks);
        Console.WriteLine($"Final queue count: {queue.Count}");
    }

    private static async Task DemoBoundedBuffer()
    {
        Console.WriteLine("--- Bounded Buffer Demo ---");

        using var buffer = new BoundedBuffer<int>(5);
        var tasks = new List<Task>();

        // Producer task
        tasks.Add(Task.Run(async () =>
        {
            for (int i = 0; i < 10; i++)
            {
                bool added = await buffer.TryAddAsync(i, TimeSpan.FromSeconds(1));
                Console.WriteLine(added ? $"Added: {i}" : $"Failed to add: {i}");
            }
        }));

        // Consumer task
        tasks.Add(Task.Run(async () =>
        {
            await Task.Delay(100); // Let producer get ahead
            
            for (int i = 0; i < 8; i++)
            {
                if (buffer.TryTake(out var item))
                {
                    Console.WriteLine($"Consumed: {item}");
                }
                await Task.Delay(50);
            }
        }));

        await Task.WhenAll(tasks);
        Console.WriteLine($"Final buffer count: {buffer.Count}");
    }

    private static void DemoAtomicCounter()
    {
        Console.WriteLine("--- Atomic Counter Demo ---");

        var counter = new AtomicCounter(10);
        var tasks = new List<Task>();

        Console.WriteLine($"Initial value: {counter.Value}");

        // Multiple threads modifying counter
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    var value = counter.GetAndIncrement();
                    Console.WriteLine($"Thread got: {value}, incremented to: {value + 1}");
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        Console.WriteLine($"Final value: {counter.Value}");
        Console.WriteLine($"Counter operations: Increment: {counter.Increment()}, Decrement: {counter.Decrement()}");
        Console.WriteLine($"Add 5: {counter.Add(5)}, Exchange with 100: {counter.Exchange(100)}");
        Console.WriteLine($"Final value after operations: {counter.Value}");
    }

    private static async Task DemoConcurrentObjectPool()
    {
        Console.WriteLine("--- Concurrent Object Pool Demo ---");

        var pool = new ConcurrentObjectPool<StringBuilder>(
            factory: () => new StringBuilder(),
            reset: sb => sb.Clear(),
            maxSize: 3);

        var tasks = new List<Task>();

        // Multiple tasks using pool
        for (int i = 0; i < 5; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                using var pooled = pool.RentDisposable();
                var sb = pooled.Value;
                
                sb.Append($"Task {taskId} used this StringBuilder");
                Console.WriteLine($"Task {taskId}: {sb}");
                
                // StringBuilder will be automatically returned to pool when disposed
            }));
        }

        await Task.WhenAll(tasks);
        Console.WriteLine($"Pool count after use: {pool.Count}");

        // Demonstrate pool reuse
        var sb1 = pool.Rent();
        sb1.Append("First use");
        Console.WriteLine($"First rental: {sb1}");
        pool.Return(sb1);

        var sb2 = pool.Rent(); // Should get the same instance
        Console.WriteLine($"Second rental (should be empty after reset): '{sb2}'");
        pool.Return(sb2);

        pool.Dispose();
    }

    private static void DemoSPSCRingBuffer()
    {
        Console.WriteLine("--- SPSC Ring Buffer Demo ---");

        var buffer = new SPSCRingBuffer<int>(8); // Must be power of 2
        var producedItems = new List<int>();
        var consumedItems = new List<int>();

        // Producer task
        var producer = Task.Run(() =>
        {
            for (int i = 0; i < 15; i++)
            {
                while (!buffer.TryWrite(i))
                {
                    Thread.Sleep(1); // Buffer full, wait a bit
                }
                producedItems.Add(i);
                Console.WriteLine($"Produced: {i}");
            }
        });

        // Consumer task
        var consumer = Task.Run(() =>
        {
            while (consumedItems.Count < 15)
            {
                if (buffer.TryRead(out var item))
                {
                    consumedItems.Add(item);
                    Console.WriteLine($"Consumed: {item}");
                }
                Thread.Sleep(5); // Simulate processing time
            }
        });

        Task.WaitAll(producer, consumer);

        Console.WriteLine($"Produced: [{string.Join(", ", producedItems)}]");
        Console.WriteLine($"Consumed: [{string.Join(", ", consumedItems)}]");
        Console.WriteLine($"Items match: {producedItems.SequenceEqual(consumedItems)}");
        Console.WriteLine($"Final buffer count: {buffer.Count}");
    }

    private static void DemoConcurrentHashMap()
    {
        Console.WriteLine("--- Concurrent Hash Map Demo ---");

        var hashMap = new ConcurrentHashMap<string, int>();
        var tasks = new List<Task>();

        // Multiple threads adding/updating values
        for (int i = 0; i < 4; i++)
        {
            int threadId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 5; j++)
                {
                    var key = $"Key{j}";
                    var added = hashMap.TryAdd(key, threadId * 10 + j);
                    if (added)
                    {
                        Console.WriteLine($"Thread {threadId} added {key}: {threadId * 10 + j}");
                    }
                    else
                    {
                        // Key exists, try to update
                        var newValue = threadId * 100 + j;
                        hashMap.AddOrUpdate(key, newValue, (k, oldValue) => oldValue + newValue);
                        Console.WriteLine($"Thread {threadId} updated {key}");
                    }
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        Console.WriteLine($"\nFinal hash map contents ({hashMap.Count} items):");
        foreach (var kvp in hashMap)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }

        // Test removal
        if (hashMap.TryRemove("Key0", out var removedValue))
        {
            Console.WriteLine($"Removed Key0 with value: {removedValue}");
        }

        Console.WriteLine($"Final count: {hashMap.Count}");
        hashMap.Dispose();
    }

    private static bool IsDescendingOrder(List<int> items)
    {
        for (int i = 1; i < items.Count; i++)
        {
            if (items[i] > items[i - 1])
                return false;
        }
        return true;
    }
}