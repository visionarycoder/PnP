using CSharp.FunctionalLinq;
using System.Collections.Immutable;

namespace CSharp.FunctionalLinq;

/// <summary>
/// Demonstrates functional programming patterns with LINQ in C#.
/// 
/// Functional LINQ combines traditional LINQ with functional programming concepts
/// like monads, immutability, higher-order functions, and compositional patterns
/// to create more expressive and safer code.
/// 
/// Key Features Demonstrated:
/// - Functional-style LINQ extensions (Map, Filter, FlatMap)
/// - Maybe monad for null-safe operations
/// - Either monad for error handling without exceptions
/// - Pipeline pattern for operation chaining
/// - Lazy evaluation patterns
/// - Immutable collection operations
/// - Function composition patterns
/// </summary>
public class Program
{
    public static Task Main(string[] args)
    {
        Console.WriteLine("=== Functional LINQ Patterns Demo ===\n");

        DemoFunctionalLinqExtensions();
        DemoMaybeMonad();
        DemoEitherMonad();
        DemoPipelinePattern();
        DemoLazyEvaluation();
        DemoImmutableCollections();
        DemoAdvancedPatterns();

        Console.WriteLine("\n=== Demo Complete ===");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Demonstrates functional-style LINQ extensions
    /// </summary>
    private static void DemoFunctionalLinqExtensions()
    {
        Console.WriteLine("1. Functional LINQ Extensions");
        Console.WriteLine("----------------------------");

        var numbers = Enumerable.Range(1, 10);

        // Traditional LINQ
        var traditionalResult = numbers
            .Where(x => x % 2 == 0)
            .Select(x => x * x)
            .ToList();

        Console.WriteLine($"Traditional LINQ: [{string.Join(", ", traditionalResult)}]");

        // Functional LINQ with Map/Filter
        var functionalResult = numbers
            .Filter(x => x % 2 == 0)
            .Map(x => x * x)
            .ToList();

        Console.WriteLine($"Functional LINQ: [{string.Join(", ", functionalResult)}]");

        // FlatMap demonstration
        var words = new[] { "hello", "world", "functional", "programming" };
        var vowels = new[] { 'a', 'e', 'i', 'o', 'u' };
        var characters = words
            .FlatMap(word => word.ToCharArray())
            .Filter(c => vowels.Contains(char.ToLower(c)))
            .Map(c => char.ToUpper(c))
            .Distinct()
            .ToList();

        Console.WriteLine($"Vowels extracted: [{string.Join(", ", characters)}]");

        // FoldLeft operations (using actual implementation)
        var sum = numbers.FoldLeft(0, (acc, x) => acc + x);
        var product = numbers.Where(x => x <= 5).FoldLeft(1, (acc, x) => acc * x);

        Console.WriteLine($"Sum: {sum}, Product (1-5): {product}");

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates Maybe monad for null-safe operations
    /// </summary>
    private static void DemoMaybeMonad()
    {
        Console.WriteLine("2. Maybe Monad - Null Safety");
        Console.WriteLine("---------------------------");

        // Working with Maybe values
        var validNumber = Maybe<int>.Some(42);
        var invalidNumber = Maybe<int>.None;

        Console.WriteLine($"Valid number has value: {validNumber.HasValue}");
        Console.WriteLine($"Invalid number has value: {invalidNumber.HasValue}");

        // Map operations
        var doubled = validNumber.Map(x => x * 2);
        var doubledInvalid = invalidNumber.Map(x => x * 2);
        
        Console.WriteLine($"Doubled valid: {doubled.GetOrElse(0)}");
        Console.WriteLine($"Doubled invalid: {doubledInvalid.GetOrElse(0)}");

        // Chain multiple Maybe operations with strings
        var texts = new[] { "42", "invalid", "123", null };
        
        foreach (var text in texts)
        {
            var result = (text != null ? Maybe<string>.Some(text) : Maybe<string>.None)
                .Filter(s => !string.IsNullOrEmpty(s))
                .FlatMap(s => int.TryParse(s, out var n) ? Maybe<int>.Some(n) : Maybe<int>.None)
                .Map(n => n * 2)
                .Map(n => $"Result: {n}");

            var display = result.HasValue ? result.Value : "No valid number";
            Console.WriteLine($"'{text ?? "null"}' -> {display}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates Either monad for error handling
    /// </summary>
    private static void DemoEitherMonad()
    {
        Console.WriteLine("3. Either Monad - Error Handling");
        Console.WriteLine("--------------------------------");

        // Operations that can fail
        var inputs = new[] { "42", "abc", "123", "-5", "999" };

        foreach (var input in inputs)
        {
            var result = ParseNumber(input)
                .Map(n => n * 2)
                .Map(n => $"Double: {n}")
                .Match(
                    leftFunc: error => $"❌ {error}",
                    rightFunc: value => $"✅ {value}");

            Console.WriteLine($"Input '{input}': {result}");
        }

        // Chain multiple Either operations
        Console.WriteLine("\nEither chain with division:");

        var divisions = new[] { ("10", "2"), ("15", "3"), ("10", "0"), ("abc", "2") };

        foreach (var (dividend, divisor) in divisions)
        {
            var result = ParseNumber(dividend)
                .FlatMap(a => ParseNumber(divisor).Map(b => (a, b)))
                .FlatMap(tuple => tuple.b == 0 
                    ? Either<string, int>.Left("Division by zero")
                    : Either<string, int>.Right(tuple.a / tuple.b))
                .Match(
                    leftFunc: error => $"❌ Error: {error}",
                    rightFunc: value => $"✅ Result: {value}");

            Console.WriteLine($"{dividend} ÷ {divisor} = {result}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates pipeline pattern for composable operations
    /// </summary>
    private static void DemoPipelinePattern()
    {
        Console.WriteLine("4. Pipeline Pattern");
        Console.WriteLine("------------------");

        var textData = new[]
        {
            "Hello World",
            "Functional Programming", 
            "Pipeline Pattern Demo",
            "Immutable Collections"
        };

        // Functional pipeline using method chaining
        Console.WriteLine("Processing text data through functional pipeline:");
        
        var result = textData
            .Map(s => { Console.WriteLine($"  🔄 Converting to lowercase: {s}"); return s.ToLower(); })
            .FlatMap(s => { 
                var words = s.Split(' ');
                Console.WriteLine($"  🔄 Splitting into words: [{string.Join(", ", words)}]"); 
                return words; 
            })
            .Filter(w => { 
                var keep = w.Length > 5;
                if (keep) Console.WriteLine($"  ✅ Keeping long word: {w}");
                return keep;
            })
            .Distinct()
            .ToArray();
        
        Console.WriteLine($"\nFinal result: [{string.Join(", ", result)}]");
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates lazy evaluation patterns
    /// </summary>
    private static void DemoLazyEvaluation()
    {
        Console.WriteLine("5. Lazy Evaluation Patterns");
        Console.WriteLine("---------------------------");

        // Built-in lazy evaluation with .NET
        var expensiveComputation = new Lazy<int>(() =>
        {
            Console.WriteLine("  🔄 Computing expensive operation...");
            return Enumerable.Range(1, 1000).Sum();
        });

        Console.WriteLine("Lazy computation created but not yet evaluated");

        // Conditional evaluation
        bool shouldCompute = true;
        Console.WriteLine($"Should compute: {shouldCompute}");

        if (shouldCompute)
        {
            var result = expensiveComputation.Value; // Evaluation happens here
            Console.WriteLine($"✅ Result: {result}");
            
            // Second access uses cached value
            var cachedResult = expensiveComputation.Value;
            Console.WriteLine($"   Cached access: {cachedResult}");
        }
        else
        {
            Console.WriteLine("⏭️ Computation skipped (not needed)");
        }

        // Lazy LINQ evaluation
        Console.WriteLine("\nLazy sequence processing:");
        var lazySequence = Enumerable.Range(1, 10)
            .Map(x => { Console.WriteLine($"    Processing: {x}"); return x * x; })
            .Filter(x => x > 25);

        Console.WriteLine("Lazy sequence created, now consuming first 3:");
        var first3 = lazySequence.Take(3).ToArray();
        Console.WriteLine($"Results: [{string.Join(", ", first3)}]");

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates immutable collection operations
    /// </summary>
    private static void DemoImmutableCollections()
    {
        Console.WriteLine("6. Immutable Collection Operations");
        Console.WriteLine("---------------------------------");

        var originalList = ImmutableList.Create(1, 2, 3, 4, 5);
        Console.WriteLine($"Original: [{string.Join(", ", originalList)}]");

        // Functional operations return new immutable collections
        var doubled = originalList.Select(x => x * 2).ToImmutableList();
        var filtered = doubled.Where(x => x > 5).ToImmutableList();
        var withAddition = filtered.Add(100);

        Console.WriteLine($"After Map(x2): [{string.Join(", ", doubled)}]");
        Console.WriteLine($"After Filter(>5): [{string.Join(", ", filtered)}]");
        Console.WriteLine($"After Add(100): [{string.Join(", ", withAddition)}]");
        Console.WriteLine($"Original unchanged: [{string.Join(", ", originalList)}]");

        // Immutable dictionary operations
        var dict = ImmutableDictionary<string, int>.Empty
            .SetItem("apple", 5)
            .SetItem("banana", 3)
            .SetItem("cherry", 8);

        var transformed = dict
            .Where(kvp => kvp.Value > 4)
            .ToImmutableDictionary(kvp => kvp.Key.ToUpper(), kvp => kvp.Value * 10);

        Console.WriteLine("\nImmutable dictionary transformation:");
        foreach (var kvp in transformed)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates advanced functional patterns
    /// </summary>
    private static void DemoAdvancedPatterns()
    {
        Console.WriteLine("7. Advanced Functional Patterns");
        Console.WriteLine("------------------------------");

        // Function composition through method chaining
        Func<int, int> addTwo = x => x + 2;
        Func<int, int> multiplyByThree = x => x * 3;
        Func<int, string> toString = x => $"Result: {x}";

        // Manual composition
        var composed = new Func<int, string>(x => toString(multiplyByThree(addTwo(x))));
        
        Console.WriteLine($"Composed function f(5): {composed(5)}");
        Console.WriteLine("  Pipeline: 5 → (+2) → (*3) → (toString) = 'Result: 21'");

        // Simple currying simulation
        Func<int, Func<int, int>> curriedAdd = x => y => x + y;
        var addFive = curriedAdd(5);

        Console.WriteLine($"\nCurried addition:");
        Console.WriteLine($"  addFive(3) = {addFive(3)}");
        Console.WriteLine($"  addFive(7) = {addFive(7)}");

        // Function pipeline with LINQ
        var numbers = new[] { 1, 2, 3, 4, 5 };
        var pipeline = numbers
            .Map(addTwo)
            .Map(multiplyByThree)
            .Filter(x => x > 10)
            .ToArray();

        Console.WriteLine($"Function pipeline result: [{string.Join(", ", pipeline)}]");

        Console.WriteLine();
    }

    private static Either<string, int> ParseNumber(string input)
    {
        return int.TryParse(input, out var number)
            ? Either<string, int>.Right(number)
            : Either<string, int>.Left($"'{input}' is not a valid number");
    }
}