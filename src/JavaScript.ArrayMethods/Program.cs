using System.Text;

namespace JavaScript.ArrayMethods;

/// <summary>
/// Modern JavaScript array manipulation patterns using ES6+ methods with TypeScript type safety.
/// Demonstrates C# equivalents of JavaScript array methods and patterns.
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.WriteLine("=== JavaScript Array Methods Examples ===\n");

        FilterAndTransformExample();
        ReduceAndGroupByExample();
        FindOperationsExample();
        BooleanChecksExample();
        UniqueValuesExample();
        ChainedOperationsExample();
        PerformanceOptimizedExample();
    }

    /// <summary>
    /// Filter and Map - Transform and filter arrays
    /// JavaScript equivalent: items.filter(predicate).map(transformer)
    /// </summary>
    private static void FilterAndTransformExample()
    {
        Console.WriteLine("1. Filter and Transform Operations:");

        var numbers = new[] { 1, 2, 3, 4, 5, 6 };
        
        // C# LINQ equivalent of JavaScript filterAndTransform
        var evenSquares = numbers
            .Where(n => n % 2 == 0)  // filter: keep even numbers
            .Select(n => n * n)      // map: square them
            .ToArray();

        Console.WriteLine($"   Numbers: [{string.Join(", ", numbers)}]");
        Console.WriteLine($"   Even squares: [{string.Join(", ", evenSquares)}]");

        // Generic helper method
        static TResult[] FilterAndTransform<TSource, TResult>(
            IEnumerable<TSource> items,
            Func<TSource, bool> predicate,
            Func<TSource, TResult> transformer)
        {
            return items.Where(predicate).Select(transformer).ToArray();
        }

        var result = FilterAndTransform(numbers, n => n > 3, n => $"num_{n}");
        Console.WriteLine($"   Numbers > 3 as strings: [{string.Join(", ", result)}]\n");
    }

    /// <summary>
    /// Reduce - Sum, group, or aggregate data
    /// JavaScript equivalent: arr.reduce((acc, val) => acc + val, 0)
    /// </summary>
    private static void ReduceAndGroupByExample()
    {
        Console.WriteLine("2. Reduce and GroupBy Operations:");

        var numbers = new[] { 1, 2, 3, 4, 5 };
        var sum = numbers.Aggregate(0, (acc, val) => acc + val); // or Sum()
        
        Console.WriteLine($"   Numbers: [{string.Join(", ", numbers)}]");
        Console.WriteLine($"   Sum: {sum}");

        var people = new[]
        {
            new { Name = "Alice", Age = 25 },
            new { Name = "Bob", Age = 30 },
            new { Name = "Charlie", Age = 25 }
        };

        // JavaScript: groupBy(people, p => p.age)
        var byAge = people.GroupBy(p => p.Age).ToLookup(g => g.Key, g => g.ToArray());
        
        Console.WriteLine("   People grouped by age:");
        foreach (var group in byAge)
        {
            var names = string.Join(", ", group.First().Select(p => p.Name));
            Console.WriteLine($"     Age {group.Key}: {names}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Find - Locate items using optional patterns
    /// JavaScript equivalent: arr.find(predicate), arr.filter(predicate), arr.findIndex(predicate)
    /// </summary>
    private static void FindOperationsExample()
    {
        Console.WriteLine("3. Find Operations:");

        var users = new[]
        {
            new { Id = 1, Name = "Alice", Active = true },
            new { Id = 2, Name = "Bob", Active = false },
            new { Id = 3, Name = "Charlie", Active = true }
        };

        // JavaScript: findFirst(users, u => u.active)
        var firstActive = users.FirstOrDefault(u => u.Active);
        
        // JavaScript: findAll(users, u => u.active)
        var allActive = users.Where(u => u.Active).ToArray();
        
        // JavaScript: findIndex(users, u => u.active)
        var firstActiveIndex = Array.FindIndex(users.ToArray(), u => u.Active);

        Console.WriteLine($"   First active user: {firstActive?.Name}");
        Console.WriteLine($"   All active users: [{string.Join(", ", allActive.Select(u => u.Name))}]");
        Console.WriteLine($"   Index of first active: {firstActiveIndex}\n");
    }

    /// <summary>
    /// Every and Some - Boolean checks
    /// JavaScript equivalent: arr.every(predicate), arr.some(predicate)
    /// </summary>
    private static void BooleanChecksExample()
    {
        Console.WriteLine("4. Boolean Check Operations:");

        var positiveNumbers = new[] { 1, 2, 3 };
        var mixedNumbers = new[] { 1, -2, 3 };

        // JavaScript: arr.every(n => n > 0)
        var allPositiveInFirst = positiveNumbers.All(n => n > 0);
        var allPositiveInSecond = mixedNumbers.All(n => n > 0);
        
        // JavaScript: arr.some(n => n < 0)
        var hasNegativeInFirst = positiveNumbers.Any(n => n < 0);
        var hasNegativeInSecond = mixedNumbers.Any(n => n < 0);

        Console.WriteLine($"   Positive numbers [{string.Join(", ", positiveNumbers)}]:");
        Console.WriteLine($"     All positive: {allPositiveInFirst}");
        Console.WriteLine($"     Has negative: {hasNegativeInFirst}");
        
        Console.WriteLine($"   Mixed numbers [{string.Join(", ", mixedNumbers)}]:");
        Console.WriteLine($"     All positive: {allPositiveInSecond}");
        Console.WriteLine($"     Has negative: {hasNegativeInSecond}\n");
    }

    /// <summary>
    /// Unique values - Remove duplicates
    /// JavaScript equivalent: [...new Set(arr)], custom uniqueBy function
    /// </summary>
    private static void UniqueValuesExample()
    {
        Console.WriteLine("5. Unique Values Operations:");

        var numbersWithDuplicates = new[] { 1, 2, 2, 3, 3, 3, 4 };
        
        // JavaScript: [...new Set(arr)]
        var uniqueNumbers = numbersWithDuplicates.Distinct().ToArray();

        Console.WriteLine($"   Original: [{string.Join(", ", numbersWithDuplicates)}]");
        Console.WriteLine($"   Unique: [{string.Join(", ", uniqueNumbers)}]");

        var usersWithDuplicates = new[]
        {
            new { Id = 1, Name = "Alice" },
            new { Id = 2, Name = "Bob" },
            new { Id = 1, Name = "Alice" },
            new { Id = 3, Name = "Charlie" }
        };

        // JavaScript: uniqueBy(users, u => u.id)
        var uniqueUsers = usersWithDuplicates.DistinctBy(u => u.Id).ToArray();
        
        Console.WriteLine("   Unique users by ID:");
        foreach (var user in uniqueUsers)
        {
            Console.WriteLine($"     {user.Id}: {user.Name}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Chained operations - Combining multiple array methods
    /// JavaScript equivalent: arr.filter().map().reduce()
    /// </summary>
    private static void ChainedOperationsExample()
    {
        Console.WriteLine("6. Chained Operations:");

        var products = new[]
        {
            new { Name = "Laptop", Price = 1000, Category = "Electronics" },
            new { Name = "Book", Price = 20, Category = "Education" },
            new { Name = "Phone", Price = 800, Category = "Electronics" },
            new { Name = "Pen", Price = 5, Category = "Education" }
        };

        // JavaScript equivalent:
        // products
        //   .filter(p => p.category === 'Electronics')
        //   .map(p => p.price)
        //   .reduce((sum, price) => sum + price, 0)
        
        var electronicsTotal = products
            .Where(p => p.Category == "Electronics")
            .Select(p => p.Price)
            .Sum();

        var expensiveElectronics = products
            .Where(p => p.Category == "Electronics" && p.Price > 500)
            .Select(p => new { p.Name, FormattedPrice = $"${p.Price:N0}" })
            .ToArray();

        Console.WriteLine($"   Total electronics price: ${electronicsTotal:N0}");
        Console.WriteLine("   Expensive electronics:");
        foreach (var item in expensiveElectronics)
        {
            Console.WriteLine($"     {item.Name}: {item.FormattedPrice}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Performance optimized operations using Span and modern C# features
    /// Demonstrates high-performance alternatives to LINQ for hot paths
    /// </summary>
    private static void PerformanceOptimizedExample()
    {
        Console.WriteLine("7. Performance Optimized Operations:");

        var largeArray = Enumerable.Range(1, 1000).ToArray();
        var span = largeArray.AsSpan();

        // High-performance filtering using Span
        var evenCount = 0;
        var evenSum = 0;
        
        foreach (var value in span)
        {
            if (value % 2 == 0)
            {
                evenCount++;
                evenSum += value;
            }
        }

        Console.WriteLine($"   Large array size: {largeArray.Length:N0}");
        Console.WriteLine($"   Even numbers count: {evenCount:N0}");
        Console.WriteLine($"   Even numbers sum: {evenSum:N0}");
        
        // Memory-efficient batch processing
        var batchSize = 100;
        var batchSums = new List<int>();
        
        for (var i = 0; i < largeArray.Length; i += batchSize)
        {
            var batch = span.Slice(i, Math.Min(batchSize, largeArray.Length - i));
            var batchSum = 0;
            foreach (var value in batch)
            {
                batchSum += value;
            }
            batchSums.Add(batchSum);
        }

        Console.WriteLine($"   Processed in {batchSums.Count} batches of {batchSize}");
        Console.WriteLine($"   First 3 batch sums: [{string.Join(", ", batchSums.Take(3))}]");
    }
}
