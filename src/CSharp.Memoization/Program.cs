using CSharp.Memoization;
using System.Diagnostics;

namespace CSharp.Memoization;

/// <summary>
/// Demonstrates comprehensive memoization patterns including function caching,
/// weak reference memoization, method decorators, and performance optimization.
/// </summary>
class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== Memoization Patterns Demonstration ===\n");

        DemonstrateBasicMemoization();
        await DemonstrateAsyncMemoization();
        DemonstrateFibonacciMemoization();
        DemonstrateWeakReferenceMemoization();
        DemonstrateMemoizationDecorator();
        DemonstratePerformanceComparison();
    }

    static void DemonstrateBasicMemoization()
    {
        Console.WriteLine("--- Basic Memoization ---");

        var options = new MemoizationOptions
        {
            MaxCacheSize = 100,
            DefaultExpiration = TimeSpan.FromMinutes(5)
        };

        var memoizer = new Memoizer<string, string>(options);

        // Simulate expensive string operations
        Func<string, string> expensiveOperation = input =>
        {
            Console.WriteLine($"  Computing for: {input}");
            Thread.Sleep(100); // Simulate work
            return input.ToUpper().Reverse().ToArray().Aggregate("", (acc, c) => acc + c);
        };

        // First calls - cache misses
        Console.WriteLine("First calls (cache misses):");
        var result1 = memoizer.GetOrCompute("hello", expensiveOperation);
        var result2 = memoizer.GetOrCompute("world", expensiveOperation);
        
        Console.WriteLine($"Results: {result1}, {result2}");

        // Second calls - cache hits
        Console.WriteLine("\nSecond calls (cache hits):");
        var result3 = memoizer.GetOrCompute("hello", expensiveOperation);
        var result4 = memoizer.GetOrCompute("world", expensiveOperation);
        
        Console.WriteLine($"Results: {result3}, {result4}");

        var stats = memoizer.GetStatistics();
        Console.WriteLine($"Cache Statistics: Hits={stats.HitCount}, Misses={stats.MissCount}, Hit Ratio={stats.HitRatio:P1}");
        
        memoizer.Dispose();
        Console.WriteLine();
    }

    static async Task DemonstrateAsyncMemoization()
    {
        Console.WriteLine("--- Async Memoization ---");

        var memoizer = new Memoizer<int, string>();

        Func<int, Task<string>> asyncOperation = async id =>
        {
            Console.WriteLine($"  Fetching data for ID: {id}");
            await Task.Delay(200); // Simulate async work
            return $"Data for ID {id} (timestamp: {DateTime.Now:HH:mm:ss.fff})";
        };

        // Demonstrate async memoization
        var tasks = new[]
        {
            memoizer.GetOrComputeAsync(1, asyncOperation),
            memoizer.GetOrComputeAsync(2, asyncOperation),
            memoizer.GetOrComputeAsync(1, asyncOperation), // This should be cached
            memoizer.GetOrComputeAsync(3, asyncOperation),
            memoizer.GetOrComputeAsync(2, asyncOperation)  // This should be cached
        };

        var results = await Task.WhenAll(tasks);
        
        Console.WriteLine("Results:");
        for (int i = 0; i < results.Length; i++)
        {
            Console.WriteLine($"  Task {i + 1}: {results[i]}");
        }

        var stats = memoizer.GetStatistics();
        Console.WriteLine($"Cache Statistics: Hits={stats.HitCount}, Misses={stats.MissCount}");
        
        memoizer.Dispose();
        Console.WriteLine();
    }

    static void DemonstrateFibonacciMemoization()
    {
        Console.WriteLine("--- Fibonacci Memoization ---");

        var stopwatch = Stopwatch.StartNew();
        
        // Compute several Fibonacci numbers
        var numbers = new[] { 10, 20, 30, 15, 25, 10, 20 }; // Note: 10 and 20 repeated
        
        Console.WriteLine("Computing Fibonacci numbers:");
        foreach (var n in numbers)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            var result = FibonacciMemoizer.Fibonacci(n);
            var duration = stopwatch.ElapsedMilliseconds - startTime;
            
            Console.WriteLine($"  F({n}) = {result} (computed in {duration}ms)");
        }

        Console.WriteLine($"Cache size: {FibonacciMemoizer.CacheSize} entries");
        
        FibonacciMemoizer.ClearCache();
        Console.WriteLine();
    }

    static void DemonstrateWeakReferenceMemoization()
    {
        Console.WriteLine("--- Weak Reference Memoization ---");

        using var weakMemoizer = new WeakMemoizer<string, ExpensiveObject>();

        Func<string, ExpensiveObject> factory = name =>
        {
            Console.WriteLine($"  Creating expensive object: {name}");
            return new ExpensiveObject(name);
        };

        // Create some objects
        var obj1 = weakMemoizer.GetOrCompute("Object1", factory);
        var obj2 = weakMemoizer.GetOrCompute("Object2", factory);
        var obj1Again = weakMemoizer.GetOrCompute("Object1", factory); // Should be cached

        Console.WriteLine($"obj1 == obj1Again: {ReferenceEquals(obj1, obj1Again)}");

        var (alive, dead) = weakMemoizer.GetStatistics();
        Console.WriteLine($"Weak references - Alive: {alive}, Dead: {dead}");

        // Clear strong references and force garbage collection
        obj1 = null;
        obj2 = null;
        obj1Again = null;
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Thread.Sleep(100); // Allow cleanup timer to run

        (alive, dead) = weakMemoizer.GetStatistics();
        Console.WriteLine($"After GC - Alive: {alive}, Dead: {dead}");
        
        Console.WriteLine();
    }

    static void DemonstrateMemoizationDecorator()
    {
        Console.WriteLine("--- Memoization Decorator ---");

        var calculator = new Calculator();
        using var memoizedCalculator = new MemoizingDecorator<Calculator>(calculator);

        // Test method memoization
        Console.WriteLine("Computing with memoization:");
        
        var result1 = memoizedCalculator.ExecuteWithMemoization(
            c => c.ComplexCalculation(5, 3), 
            nameof(Calculator.ComplexCalculation), 
            5, 3);
        Console.WriteLine($"Result 1: {result1}");

        var result2 = memoizedCalculator.ExecuteWithMemoization(
            c => c.ComplexCalculation(5, 3), 
            nameof(Calculator.ComplexCalculation), 
            5, 3);
        Console.WriteLine($"Result 2 (cached): {result2}");

        var result3 = memoizedCalculator.ExecuteWithMemoization(
            c => c.ComplexCalculation(7, 2), 
            nameof(Calculator.ComplexCalculation), 
            7, 2);
        Console.WriteLine($"Result 3: {result3}");

        Console.WriteLine();
    }

    static void DemonstratePerformanceComparison()
    {
        Console.WriteLine("--- Performance Comparison ---");

        const int iterations = 1000;
        var random = new Random(42);

        // Test expensive function without memoization
        Func<int, double> expensiveFunction = x =>
        {
            // Simulate expensive computation
            double result = 0;
            for (int i = 0; i < 10000; i++)
            {
                result += Math.Sin(x + i) * Math.Cos(x - i);
            }
            return result;
        };

        // Create memoized version
        var memoizedFunction = expensiveFunction.Memoize();

        var inputs = Enumerable.Range(0, iterations)
            .Select(_ => random.Next(1, 50)) // Random inputs with high probability of duplicates
            .ToList();

        // Benchmark without memoization
        var stopwatch = Stopwatch.StartNew();
        var results1 = inputs.Select(expensiveFunction).ToList();
        var timeWithoutMemo = stopwatch.ElapsedMilliseconds;

        // Benchmark with memoization
        stopwatch.Restart();
        var results2 = inputs.Select(memoizedFunction).ToList();
        var timeWithMemo = stopwatch.ElapsedMilliseconds;

        var uniqueInputs = inputs.Distinct().Count();
        var speedup = (double)timeWithoutMemo / timeWithMemo;

        Console.WriteLine($"Performance Results ({iterations} iterations):");
        Console.WriteLine($"  Unique inputs: {uniqueInputs} out of {iterations}");
        Console.WriteLine($"  Without memoization: {timeWithoutMemo}ms");
        Console.WriteLine($"  With memoization: {timeWithMemo}ms");
        Console.WriteLine($"  Speedup: {speedup:F1}x faster");
        Console.WriteLine($"  Results match: {results1.SequenceEqual(results2)}");

        Console.WriteLine();
    }
}

/// <summary>
/// Example class for demonstrating object creation caching.
/// </summary>
public class ExpensiveObject
{
    public string Name { get; }
    public DateTime CreatedAt { get; }

    public ExpensiveObject(string name)
    {
        Name = name;
        CreatedAt = DateTime.Now;
        // Simulate expensive initialization
        Thread.Sleep(50);
    }

    public override string ToString() => $"{Name} (created at {CreatedAt:HH:mm:ss.fff})";
}

/// <summary>
/// Example class for demonstrating method memoization.
/// </summary>
public class Calculator
{
    /// <summary>
    /// Simulates a complex calculation that would benefit from memoization.
    /// </summary>
    public double ComplexCalculation(int a, int b)
    {
        Console.WriteLine($"  Performing complex calculation: {a}, {b}");
        Thread.Sleep(100); // Simulate expensive work
        
        double result = 0;
        for (int i = 0; i < 1000; i++)
        {
            result += Math.Pow(a, 2) * Math.Sin(b + i) + Math.Sqrt(a * b + i);
        }
        
        return Math.Round(result, 4);
    }
}