# Functional LINQ Programming

**Description**: Advanced functional programming patterns using LINQ, including monadic operations, function composition, immutable data transformations, and functional pipeline construction. Emphasizes pure functions, immutability, and compositional design.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Immutable;

// Maybe/Option monad for null-safe operations
public readonly struct Maybe<T>
{
    private readonly T value;
    private readonly bool hasValue;

    private Maybe(T value)
    {
        this.value = value;
        hasValue = value != null;
    }

    public static Maybe<T> Some(T value) => 
        value != null ? new Maybe<T>(value) : None;

    public static Maybe<T> None => default;

    public bool HasValue => hasValue;
    
    public T Value => hasValue ? value : throw new InvalidOperationException("Maybe has no value");

    // Functor: map function over Maybe
    public Maybe<TResult> Map<TResult>(Func<T, TResult> func)
    {
        return hasValue ? Maybe<TResult>.Some(func(value)) : Maybe<TResult>.None;
    }

    // Monad: flatMap for chaining Maybe operations
    public Maybe<TResult> FlatMap<TResult>(Func<T, Maybe<TResult>> func)
    {
        return hasValue ? func(value) : Maybe<TResult>.None;
    }

    // Filter: conditional Maybe
    public Maybe<T> Filter(Func<T, bool> predicate)
    {
        return hasValue && predicate(value) ? this : None;
    }

    // GetOrElse: provide default value
    public T GetOrElse(T defaultValue)
    {
        return hasValue ? value : defaultValue;
    }

    public T GetOrElse(Func<T> defaultFactory)
    {
        return hasValue ? value : defaultFactory();
    }

    // Fold: reduce Maybe to single value
    public TResult Fold<TResult>(TResult noneValue, Func<T, TResult> someFunc)
    {
        return hasValue ? someFunc(value) : noneValue;
    }

    public override string ToString()
    {
        return hasValue ? $"Some({value})" : "None";
    }

    public static implicit operator Maybe<T>(T value) => Some(value);
}

// Either monad for error handling
public abstract class Either<TLeft, TRight>
{
    public abstract bool IsLeft { get; }
    public abstract bool IsRight { get; }

    public abstract TResult Match<TResult>(
        Func<TLeft, TResult> leftFunc,
        Func<TRight, TResult> rightFunc);

    public Either<TLeft, TResult> Map<TResult>(Func<TRight, TResult> func)
    {
        return Match<Either<TLeft, TResult>>(
            left => Either<TLeft, TResult>.Left(left),
            right => Either<TLeft, TResult>.Right(func(right)));
    }

    public Either<TLeft, TResult> FlatMap<TResult>(Func<TRight, Either<TLeft, TResult>> func)
    {
        return Match(
            left => Either<TLeft, TResult>.Left(left),
            func);
    }

    public static Either<TLeft, TRight> Left(TLeft value) => new LeftImpl(value);
    public static Either<TLeft, TRight> Right(TRight value) => new RightImpl(value);

    private class LeftImpl : Either<TLeft, TRight>
    {
        public TLeft Value { get; }
        public LeftImpl(TLeft value) => Value = value;
        public override bool IsLeft => true;
        public override bool IsRight => false;

        public override TResult Match<TResult>(
            Func<TLeft, TResult> leftFunc,
            Func<TRight, TResult> rightFunc) => leftFunc(Value);
    }

    private class RightImpl : Either<TLeft, TRight>
    {
        public TRight Value { get; }
        public RightImpl(TRight value) => Value = value;
        public override bool IsLeft => false;
        public override bool IsRight => true;

        public override TResult Match<TResult>(
            Func<TLeft, TResult> leftFunc,
            Func<TRight, TResult> rightFunc) => rightFunc(Value);
    }
}

// Functional LINQ extensions
public static class FunctionalLinq
{
    // Map (Select with functional naming)
    public static IEnumerable<TResult> Map<T, TResult>(
        this IEnumerable<T> source,
        Func<T, TResult> selector)
    {
        return source.Select(selector);
    }

    // FlatMap (SelectMany with functional naming)
    public static IEnumerable<TResult> FlatMap<T, TResult>(
        this IEnumerable<T> source,
        Func<T, IEnumerable<TResult>> selector)
    {
        return source.SelectMany(selector);
    }

    // Filter (Where with functional naming)
    public static IEnumerable<T> Filter<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate)
    {
        return source.Where(predicate);
    }

    // Reduce (Aggregate with functional naming)
    public static TAccumulate Reduce<T, TAccumulate>(
        this IEnumerable<T> source,
        TAccumulate seed,
        Func<TAccumulate, T, TAccumulate> func)
    {
        return source.Aggregate(seed, func);
    }

    // FoldLeft: left-associative fold
    public static TResult FoldLeft<T, TResult>(
        this IEnumerable<T> source,
        TResult seed,
        Func<TResult, T, TResult> func)
    {
        return source.Aggregate(seed, func);
    }

    // FoldRight: right-associative fold
    public static TResult FoldRight<T, TResult>(
        this IEnumerable<T> source,
        TResult seed,
        Func<T, TResult, TResult> func)
    {
        return source.Reverse().Aggregate(seed, (acc, x) => func(x, acc));
    }

    // Scan: like Aggregate but returns intermediate results
    public static IEnumerable<TResult> Scan<T, TResult>(
        this IEnumerable<T> source,
        TResult seed,
        Func<TResult, T, TResult> func)
    {
        var accumulator = seed;
        yield return accumulator;
        
        foreach (var item in source)
        {
            accumulator = func(accumulator, item);
            yield return accumulator;
        }
    }

    // TakeWhileInclusive: TakeWhile that includes the stopping element
    public static IEnumerable<T> TakeWhileInclusive<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate)
    {
        foreach (var item in source)
        {
            yield return item;
            if (!predicate(item))
                break;
        }
    }

    // Unfold: generate sequence from seed using generator function
    public static IEnumerable<T> Unfold<T, TState>(
        TState seed,
        Func<TState, (T value, TState nextState)?> generator)
    {
        var current = seed;
        
        while (true)
        {
            var result = generator(current);
            if (!result.HasValue)
                break;
                
            yield return result.Value.value;
            current = result.Value.nextState;
        }
    }

    // Memoize: cache function results
    public static Func<T, TResult> Memoize<T, TResult>(this Func<T, TResult> func)
        where T : notnull
    {
        var cache = new();
        return input =>
        {
            if (cache.TryGetValue(input, out var cached))
                return cached;
                
            var result = func(input);
            cache[input] = result;
            return result;
        };
    }

    // Curry: convert multi-parameter function to series of single-parameter functions
    public static Func<T1, Func<T2, TResult>> Curry<T1, T2, TResult>(
        this Func<T1, T2, TResult> func)
    {
        return x => y => func(x, y);
    }

    public static Func<T1, Func<T2, Func<T3, TResult>>> Curry<T1, T2, T3, TResult>(
        this Func<T1, T2, T3, TResult> func)
    {
        return x => y => z => func(x, y, z);
    }

    // Partial application
    public static Func<T2, TResult> Partial<T1, T2, TResult>(
        this Func<T1, T2, TResult> func,
        T1 x)
    {
        return y => func(x, y);
    }

    public static Func<T2, T3, TResult> Partial<T1, T2, T3, TResult>(
        this Func<T1, T2, T3, TResult> func,
        T1 x)
    {
        return (y, z) => func(x, y, z);
    }

    // Function composition
    public static Func<T, TResult2> Compose<T, TResult1, TResult2>(
        this Func<TResult1, TResult2> f,
        Func<T, TResult1> g)
    {
        return x => f(g(x));
    }

    // Pipe operator (like F# |>)
    public static TResult Pipe<T, TResult>(this T input, Func<T, TResult> func)
    {
        return func(input);
    }

    // Tee: apply function for side effects, return original value
    public static T Tee<T>(this T input, Action<T> action)
    {
        action(input);
        return input;
    }

    // Partition: split sequence based on predicate
    public static (IEnumerable<T> trues, IEnumerable<T> falses) Partition<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate)
    {
        var list = source.ToList();
        return (list.Where(predicate), list.Where(x => !predicate(x)));
    }

    // Transpose: transpose matrix-like structure
    public static IEnumerable<IEnumerable<T>> Transpose<T>(
        this IEnumerable<IEnumerable<T>> source)
    {
        var enumerators = source.Select(seq => seq.GetEnumerator()).ToList();
        
        try
        {
            while (enumerators.All(e => e.MoveNext()))
            {
                yield return enumerators.Select(e => e.Current);
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

    // Iterate: apply function n times
    public static IEnumerable<T> Iterate<T>(T seed, Func<T, T> func, int count)
    {
        var current = seed;
        for (int i = 0; i < count; i++)
        {
            yield return current;
            current = func(current);
        }
    }

    // Cycle: repeat sequence infinitely
    public static IEnumerable<T> Cycle<T>(this IEnumerable<T> source)
    {
        var list = source.ToList();
        if (list.Count == 0) yield break;
        
        while (true)
        {
            foreach (var item in list)
                yield return item;
        }
    }

    // Intersperse: insert element between every pair of elements
    public static IEnumerable<T> Intersperse<T>(this IEnumerable<T> source, T separator)
    {
        using var enumerator = source.GetEnumerator();
        
        if (!enumerator.MoveNext())
            yield break;
            
        yield return enumerator.Current;
        
        while (enumerator.MoveNext())
        {
            yield return separator;
            yield return enumerator.Current;
        }
    }

    // Intercalate: intersperse then flatten
    public static IEnumerable<T> Intercalate<T>(
        this IEnumerable<IEnumerable<T>> source,
        IEnumerable<T> separator)
    {
        return source.Intersperse(separator).FlatMap(x => x);
    }

    // Sequence operations for Maybe
    public static Maybe<IEnumerable<T>> Sequence<T>(this IEnumerable<Maybe<T>> source)
    {
        var results = new();
        
        foreach (var maybe in source)
        {
            if (!maybe.HasValue)
                return Maybe<IEnumerable<T>>.None;
            results.Add(maybe.Value);
        }
        
        return Maybe<IEnumerable<T>>.Some(results);
    }

    // Traverse for Maybe
    public static Maybe<IEnumerable<TResult>> Traverse<T, TResult>(
        this IEnumerable<T> source,
        Func<T, Maybe<TResult>> func)
    {
        return source.Select(func).Sequence();
    }

    // Collect successes from Maybe sequence
    public static IEnumerable<T> Successes<T>(this IEnumerable<Maybe<T>> source)
    {
        return source.Where(m => m.HasValue).Select(m => m.Value);
    }

    // Apply function if condition is true
    public static IEnumerable<T> When<T>(
        this IEnumerable<T> source,
        bool condition,
        Func<IEnumerable<T>, IEnumerable<T>> operation)
    {
        return condition ? operation(source) : source;
    }

    // Apply function unless condition is true
    public static IEnumerable<T> Unless<T>(
        this IEnumerable<T> source,
        bool condition,
        Func<IEnumerable<T>, IEnumerable<T>> operation)
    {
        return condition ? source : operation(source);
    }
}

// Immutable functional data structures
public static class ImmutableCollectionExtensions
{
    // Convert to immutable with functional operations
    public static ImmutableList<T> ToImmutableList<T>(this IEnumerable<T> source)
    {
        return System.Collections.Immutable.ImmutableList.CreateRange(source);
    }

    // Functional update operations
    public static ImmutableList<T> Update<T>(
        this ImmutableList<T> list,
        int index,
        Func<T, T> updater)
    {
        if (index < 0 || index >= list.Count)
            return list;
            
        return list.SetItem(index, updater(list[index]));
    }

    // Cons operation (add to front)
    public static ImmutableList<T> Cons<T>(this ImmutableList<T> list, T item)
    {
        return list.Insert(0, item);
    }

    // Head and Tail operations
    public static Maybe<T> Head<T>(this ImmutableList<T> list)
    {
        return list.Count > 0 ? Maybe<T>.Some(list[0]) : Maybe<T>.None;
    }

    public static ImmutableList<T> Tail<T>(this ImmutableList<T> list)
    {
        return list.Count > 0 ? list.RemoveAt(0) : list;
    }

    // Safe indexing
    public static Maybe<T> TryGet<T>(this ImmutableList<T> list, int index)
    {
        return index >= 0 && index < list.Count 
            ? Maybe<T>.Some(list[index]) 
            : Maybe<T>.None;
    }

    // Functional dictionary operations
    public static ImmutableDictionary<TKey, TValue> Update<TKey, TValue>(
        this ImmutableDictionary<TKey, TValue> dict,
        TKey key,
        Func<TValue, TValue> updater)
        where TKey : notnull
    {
        return dict.TryGetValue(key, out var value) 
            ? dict.SetItem(key, updater(value))
            : dict;
    }

    public static Maybe<TValue> TryGetValue<TKey, TValue>(
        this ImmutableDictionary<TKey, TValue> dict,
        TKey key)
        where TKey : notnull
    {
        return dict.TryGetValue(key, out var value) 
            ? Maybe<TValue>.Some(value) 
            : Maybe<TValue>.None;
    }
}

// Function pipeline builder
public class Pipeline<T>
{
    private readonly IEnumerable<T> source;

    public Pipeline(IEnumerable<T> source)
    {
        this.source = source;
    }

    public Pipeline<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        return new Pipeline<TResult>(source.Select(selector));
    }

    public Pipeline<T> Filter(Func<T, bool> predicate)
    {
        return new Pipeline<T>(source.Where(predicate));
    }

    public Pipeline<TResult> FlatMap<TResult>(Func<T, IEnumerable<TResult>> selector)
    {
        return new Pipeline<TResult>(source.SelectMany(selector));
    }

    public Pipeline<T> Take(int count)
    {
        return new Pipeline<T>(source.Take(count));
    }

    public Pipeline<T> Skip(int count)
    {
        return new Pipeline<T>(source.Skip(count));
    }

    public Pipeline<T> Distinct()
    {
        return new Pipeline<T>(source.Distinct());
    }

    public Pipeline<T> OrderBy<TKey>(Func<T, TKey> keySelector)
    {
        return new Pipeline<T>(source.OrderBy(keySelector));
    }

    public Pipeline<T> Tee(Action<T> action)
    {
        return new Pipeline<T>(source.Select(item => item.Tee(action)));
    }

    // Terminal operations
    public List<T> ToList() => source.ToList();
    public T[] ToArray() => source.ToArray();
    public ImmutableList<T> ToImmutableList() => source.ToImmutableList();
    
    public TResult Fold<TResult>(TResult seed, Func<TResult, T, TResult> func)
    {
        return source.Aggregate(seed, func);
    }

    public Maybe<T> FirstMaybe() => source.FirstOrDefault();
    public Maybe<T> SingleMaybe() => source.Count() == 1 ? source.First() : Maybe<T>.None;

    public static implicit operator Pipeline<T>(IEnumerable<T> source) => new(source);
    public static implicit operator Pipeline<T>(T[] source) => new(source);
    public static implicit operator Pipeline<T>(List<T> source) => new(source);
}

// Functional validation using Either
public static class Validation
{
    public static Either<TError, TSuccess> Success<TError, TSuccess>(TSuccess value)
    {
        return Either<TError, TSuccess>.Right(value);
    }

    public static Either<TError, TSuccess> Failure<TError, TSuccess>(TError error)
    {
        return Either<TError, TSuccess>.Left(error);
    }

    public static Either<string, T> ValidateNotNull<T>(T? value, string errorMessage)
        where T : class
    {
        return value != null ? Success<string, T>(value) : Failure<string, T>(errorMessage);
    }

    public static Either<string, T> ValidateNotDefault<T>(T value, string errorMessage)
        where T : struct
    {
        return !EqualityComparer<T>.Default.Equals(value, default(T)) 
            ? Success<string, T>(value) 
            : Failure<string, T>(errorMessage);
    }

    public static Either<string, string> ValidateNotEmpty(string value, string errorMessage)
    {
        return !string.IsNullOrWhiteSpace(value) 
            ? Success<string, string>(value) 
            : Failure<string, string>(errorMessage);
    }

    public static Either<string, int> ValidateRange(int value, int min, int max, string errorMessage)
    {
        return value >= min && value <= max 
            ? Success<string, int>(value) 
            : Failure<string, int>(errorMessage);
    }

    // Combine validations
    public static Either<IEnumerable<TError>, (T1, T2)> Combine<TError, T1, T2>(
        Either<TError, T1> validation1,
        Either<TError, T2> validation2)
    {
        return (validation1.IsRight, validation2.IsRight) switch
        {
            (true, true) => Either<IEnumerable<TError>, (T1, T2)>.Right((
                validation1.Match(_ => default!, r => r),
                validation2.Match(_ => default!, r => r))),
            (false, false) => Either<IEnumerable<TError>, (T1, T2)>.Left(new[]
            {
                validation1.Match(l => l, _ => default!),
                validation2.Match(l => l, _ => default!)
            }),
            (false, true) => Either<IEnumerable<TError>, (T1, T2)>.Left(new[]
            {
                validation1.Match(l => l, _ => default!)
            }),
            (true, false) => Either<IEnumerable<TError>, (T1, T2)>.Left(new[]
            {
                validation2.Match(l => l, _ => default!)
            })
        };
    }
}

// Functional async operations
public static class AsyncFunctional
{
    // Map for Task
    public static async Task<TResult> Map<T, TResult>(
        this Task<T> task,
        Func<T, TResult> selector)
    {
        var result = await task;
        return selector(result);
    }

    // FlatMap for Task
    public static async Task<TResult> FlatMap<T, TResult>(
        this Task<T> task,
        Func<T, Task<TResult>> selector)
    {
        var result = await task;
        return await selector(result);
    }

    // Traverse for async operations
    public static async Task<IEnumerable<TResult>> Traverse<T, TResult>(
        this IEnumerable<T> source,
        Func<T, Task<TResult>> selector)
    {
        var tasks = source.Select(selector);
        return await Task.WhenAll(tasks);
    }

    // Sequential async processing
    public static async Task<IEnumerable<TResult>> TraverseSequential<T, TResult>(
        this IEnumerable<T> source,
        Func<T, Task<TResult>> selector)
    {
        var results = new();
        
        foreach (var item in source)
        {
            var result = await selector(item);
            results.Add(result);
        }
        
        return results;
    }

    // Async filter
    public static async Task<IEnumerable<T>> FilterAsync<T>(
        this IEnumerable<T> source,
        Func<T, Task<bool>> predicate)
    {
        var results = new();
        
        foreach (var item in source)
        {
            if (await predicate(item))
            {
                results.Add(item);
            }
        }
        
        return results;
    }
}

// Lazy evaluation utilities
public static class LazyFunctional
{
    // Lazy sequence generation
    public static IEnumerable<T> Generate<T>(Func<T> generator)
    {
        while (true)
            yield return generator();
    }

    public static IEnumerable<T> GenerateFinite<T>(Func<(T value, bool hasMore)> generator)
    {
        while (true)
        {
            var result = generator();
            if (!result.hasMore)
                break;
            yield return result.value;
        }
    }

    // Thunk for delayed computation
    public class Thunk<T>
    {
        private readonly Lazy<T> lazy;

        public Thunk(Func<T> computation)
        {
            lazy = new Lazy<T>(computation);
        }

        public T Force() => lazy.Value;
        public bool IsForced => lazy.IsValueCreated;

        public static implicit operator Thunk<T>(Func<T> computation) => new(computation);
    }

    // Stream-like processing
    public static IEnumerable<T> Stream<T>(T head, Func<T, T> tailGenerator)
    {
        var current = head;
        while (true)
        {
            yield return current;
            current = tailGenerator(current);
        }
    }
}

// Real-world functional examples
public static class FunctionalExamples
{
    // Functional data processing pipeline
    public record Person(string Name, int Age, string City, decimal Salary);
    public record PersonSummary(string Name, int Age, string AgeGroup, string SalaryBracket);

    public static void DataProcessingPipeline()
    {
        var people = new[]
        {
            new Person("Alice", 25, "New York", 75000),
            new Person("Bob", 35, "San Francisco", 95000),
            new Person("Charlie", 45, "Chicago", 85000),
            new Person("Diana", 28, "New York", 80000),
            new Person("Eve", 52, "San Francisco", 120000)
        };

        var ageGrouper = FunctionalLinq.Curry<int, string, string>((age, prefix) => 
            age < 30 ? $"{prefix}Young" : age < 50 ? $"{prefix}Middle" : $"{prefix}Senior");

        var salaryBracketer = ((decimal salary) => 
            salary < 80000 ? "Low" : salary < 100000 ? "Medium" : "High").Memoize();

        var result = new Pipeline<Person>(people)
            .Filter(p => p.Age >= 25)
            .Map(p => new PersonSummary(
                p.Name,
                p.Age,
                ageGrouper(p.Age)("Age: "),
                salaryBracketer(p.Salary)))
            .OrderBy(p => p.Age)
            .ToImmutableList();

        Console.WriteLine("Functional Data Processing Results:");
        foreach (var summary in result)
        {
            Console.WriteLine($"{summary.Name}: {summary.AgeGroup}, {summary.SalaryBracket} salary");
        }
    }

    // Functional validation example
    public record UserRegistration(string Email, string Password, int Age, string Country);

    public static Either<IEnumerable<string>, UserRegistration> ValidateUserRegistration(
        string email, string password, int age, string country)
    {
        var emailValidation = Validation.ValidateNotEmpty(email, "Email is required")
            .FlatMap(e => e.Contains("@") 
                ? Validation.Success<string, string>(e) 
                : Validation.Failure<string, string>("Invalid email format"));

        var passwordValidation = Validation.ValidateNotEmpty(password, "Password is required")
            .FlatMap(p => p.Length >= 8 
                ? Validation.Success<string, string>(p) 
                : Validation.Failure<string, string>("Password must be at least 8 characters"));

        var ageValidation = Validation.ValidateRange(age, 18, 120, "Age must be between 18 and 120");

        var countryValidation = Validation.ValidateNotEmpty(country, "Country is required");

        // This is a simplified version - in practice, you'd use a proper validation combinator
        var errors = new();

        emailValidation.Match(e => errors.Add(e), _ => { });
        passwordValidation.Match(e => errors.Add(e), _ => { });
        ageValidation.Match(e => errors.Add(e), _ => { });
        countryValidation.Match(e => errors.Add(e), _ => { });

        return errors.Count > 0
            ? Either<IEnumerable<string>, UserRegistration>.Left(errors)
            : Either<IEnumerable<string>, UserRegistration>.Right(
                new UserRegistration(email, password, age, country));
    }

    // Mathematical sequence generation
    public static void MathematicalSequences()
    {
        // Fibonacci using unfold
        var fibonacci = FunctionalLinq.Unfold((0, 1), state =>
        {
            var (a, b) = state;
            return (a, (b, a + b));
        });

        Console.WriteLine("Fibonacci sequence (first 10):");
        Console.WriteLine(string.Join(", ", fibonacci.Take(10)));

        // Prime numbers using functional sieve
        var primes = LazyFunctional.Generate(() => 2)
            .Concat(FunctionalLinq.Unfold(3, n => (n, n + 2)))
            .Filter(IsPrime)
            .Take(20);

        Console.WriteLine("\nFirst 20 prime numbers:");
        Console.WriteLine(string.Join(", ", primes));

        // Factorial using scan
        var factorials = Enumerable.Range(1, 10)
            .Scan(1, (acc, n) => acc * n);

        Console.WriteLine("\nFactorials (0! to 10!):");
        Console.WriteLine(string.Join(", ", factorials));
    }

    private static bool IsPrime(int n)
    {
        if (n < 2) return false;
        if (n == 2) return true;
        if (n % 2 == 0) return false;

        var sqrt = (int)Math.Sqrt(n);
        return Enumerable.Range(3, sqrt - 2)
            .Where(i => i % 2 == 1)
            .All(i => n % i != 0);
    }

    // Async functional processing
    public static async Task AsyncProcessingExample()
    {
        var urls = new[]
        {
            "https://api.github.com/users/octocat",
            "https://jsonplaceholder.typicode.com/posts/1",
            "https://httpbin.org/delay/1"
        };

        // Simulate async HTTP calls
        Func<string, Task<string>> fetchUrl = async url =>
        {
            await Task.Delay(100); // Simulate network delay
            return $"Response from {url}";
        };

        // Parallel processing
        var parallelResults = await urls.Traverse(fetchUrl);
        
        Console.WriteLine("Parallel async results:");
        foreach (var result in parallelResults)
        {
            Console.WriteLine($"  {result}");
        }

        // Sequential processing
        var sequentialResults = await urls.TraverseSequential(fetchUrl);
        
        Console.WriteLine("\nSequential async results:");
        foreach (var result in sequentialResults)
        {
            Console.WriteLine($"  {result}");
        }
    }

    // Functional error handling with Maybe and Either
    public static void ErrorHandlingExample()
    {
        // Chain of operations that might fail
        Func<string, Maybe<int>> parseAge = input =>
            int.TryParse(input, out var age) && age >= 0 ? Maybe<int>.Some(age) : Maybe<int>.None;

        Func<int, Maybe<string>> ageToCategory = age => age switch
        {
            < 13 => Maybe<string>.Some("Child"),
            < 20 => Maybe<string>.Some("Teenager"),
            < 65 => Maybe<string>.Some("Adult"),
            _ => Maybe<string>.Some("Senior")
        };

        Func<string, Maybe<string>> formatCategory = category =>
            Maybe<string>.Some($"Category: {category}");

        var inputs = new[] { "25", "invalid", "150", "8" };

        Console.WriteLine("Functional error handling with Maybe:");
        foreach (var input in inputs)
        {
            var result = parseAge(input)
                .FlatMap(ageToCategory)
                .FlatMap(formatCategory)
                .GetOrElse($"Invalid input: {input}");

            Console.WriteLine($"  {input} -> {result}");
        }

        // Either for detailed error information
        Func<string, Either<string, int>> parseAgeEither = input =>
            int.TryParse(input, out var age) 
                ? age >= 0 
                    ? Either<string, int>.Right(age) 
                    : Either<string, int>.Left("Age cannot be negative")
                : Either<string, int>.Left("Invalid number format");

        Console.WriteLine("\nFunctional error handling with Either:");
        foreach (var input in inputs)
        {
            var result = parseAgeEither(input)
                .Map(age => $"Valid age: {age}")
                .Match(
                    error => $"Error: {error}",
                    success => success);

            Console.WriteLine($"  {input} -> {result}");
        }
    }
}
```

**Usage**:

```csharp
// Example 1: Maybe monad for null-safe operations
Console.WriteLine("Maybe Monad Examples:");

// Safe chain of operations that might return null
Maybe<string> GetUserName(int userId) =>
    userId > 0 ? Maybe<string>.Some($"User{userId}") : Maybe<string>.None;

Maybe<string> GetUserEmail(string userName) =>
    !string.IsNullOrEmpty(userName) ? Maybe<string>.Some($"{userName}@example.com") : Maybe<string>.None;

Maybe<string> FormatEmail(string email) =>
    Maybe<string>.Some($"Email: {email}");

var userIds = new[] { 1, 0, 42, -1 };

foreach (var userId in userIds)
{
    var result = GetUserName(userId)
        .FlatMap(GetUserEmail)
        .FlatMap(FormatEmail)
        .GetOrElse($"No email for user {userId}");
    
    Console.WriteLine($"User {userId}: {result}");
}

// Example 2: Functional data transformation pipeline
Console.WriteLine("\nFunctional Pipeline Examples:");

var numbers = Enumerable.Range(1, 20);

var result = new Pipeline<int>(numbers)
    .Filter(n => n % 2 == 0)           // Even numbers
    .Map(n => n * n)                   // Square them
    .Filter(n => n < 100)              // Less than 100
    .Map(n => $"Square: {n}")          // Format as string
    .Take(5)                           // Take first 5
    .ToList();

Console.WriteLine($"Pipeline result: [{string.Join(", ", result)}]");

// Example 3: Function composition and currying
Console.WriteLine("\nFunction Composition Examples:");

Func<int, int> add5 = x => x + 5;
Func<int, int> multiply3 = x => x * 3;
Func<int, string> format = x => $"Result: {x}";

// Compose functions
var composedFunction = format.Compose(multiply3).Compose(add5);

var inputs = new[] { 1, 2, 3, 4, 5 };
foreach (var input in inputs)
{
    var output = composedFunction(input);
    Console.WriteLine($"{input} -> {output}");
}

// Currying example
Func<int, int, int> add = (x, y) => x + y;
var curriedAdd = add.Curry();
var add10 = curriedAdd(10);

Console.WriteLine($"Curried function: add10(5) = {add10(5)}");

// Example 4: Immutable data transformations
Console.WriteLine("\nImmutable Collections Examples:");

var originalList = ImmutableList.Create(1, 2, 3, 4, 5);

var transformedList = originalList
    .Map(x => x * 2)
    .Filter(x => x > 4)
    .ToImmutableList();

Console.WriteLine($"Original: [{string.Join(", ", originalList)}]");
Console.WriteLine($"Transformed: [{string.Join(", ", transformedList)}]");

// Safe head/tail operations
var head = originalList.Head();
var tail = originalList.Tail();

Console.WriteLine($"Head: {head.GetOrElse(-1)}");
Console.WriteLine($"Tail: [{string.Join(", ", tail)}]");

// Example 5: Functional sequence operations
Console.WriteLine("\nAdvanced Sequence Operations:");

var words = new[] { "functional", "programming", "with", "LINQ" };

// Intersperse
var interspersed = words.Intersperse("and");
Console.WriteLine($"Interspersed: [{string.Join(" ", interspersed)}]");

// Scan (running totals)
var lengths = words.Select(w => w.Length);
var runningSums = lengths.Scan(0, (acc, len) => acc + len);
Console.WriteLine($"Running sums of lengths: [{string.Join(", ", runningSums)}]");

// Transpose
var matrix = new[]
{
    new[] { 1, 2, 3 },
    new[] { 4, 5, 6 },
    new[] { 7, 8, 9 }
};

var transposed = matrix.Transpose().Select(row => row.ToArray()).ToArray();
Console.WriteLine("Matrix transpose:");
foreach (var row in transposed)
{
    Console.WriteLine($"  [{string.Join(", ", row)}]");
}

// Example 6: Lazy evaluation and infinite sequences
Console.WriteLine("\nLazy Evaluation Examples:");

// Infinite sequence with take
var naturalNumbers = FunctionalLinq.Unfold(1, n => (n, n + 1));
var firstTenNaturals = naturalNumbers.Take(10);
Console.WriteLine($"First 10 natural numbers: [{string.Join(", ", firstTenNaturals)}]");

// Fibonacci with lazy evaluation
var fibonacci = FunctionalLinq.Unfold((0, 1), state =>
{
    var (a, b) = state;
    return (a, (b, a + b));
});

var fibonacciNumbers = fibonacci.Take(15).ToList();
Console.WriteLine($"Fibonacci sequence: [{string.Join(", ", fibonacciNumbers)}]");

// Cycle (repeat infinitely)
var colors = new[] { "red", "green", "blue" };
var cycledColors = colors.Cycle().Take(10);
Console.WriteLine($"Cycled colors: [{string.Join(", ", cycledColors)}]");

// Example 7: Memoization for performance
Console.WriteLine("\nMemoization Examples:");

// Expensive recursive function
Func<int, long> fibonacci_slow = null!;
fibonacci_slow = n => n <= 1 ? n : fibonacci_slow(n - 1) + fibonacci_slow(n - 2);

// Memoized version
var fibonacci_fast = fibonacci_slow.Memoize();

var stopwatch = System.Diagnostics.Stopwatch.StartNew();
var result40 = fibonacci_fast(40);
stopwatch.Stop();
Console.WriteLine($"Fibonacci(40) = {result40} (calculated in {stopwatch.ElapsedMilliseconds}ms)");

// Second call should be much faster due to memoization
stopwatch.Restart();
result40 = fibonacci_fast(40);
stopwatch.Stop();
Console.WriteLine($"Fibonacci(40) = {result40} (cached in {stopwatch.ElapsedMilliseconds}ms)");

// Example 8: Functional validation
Console.WriteLine("\nFunctional Validation Examples:");

var userRegistrations = new[]
{
    ("alice@example.com", "password123", 25, "USA"),
    ("", "short", 17, ""),
    ("bob@test.com", "verylongpassword", 30, "Canada"),
    ("invalid-email", "password", 150, "Unknown")
};

foreach (var (email, password, age, country) in userRegistrations)
{
    var validation = FunctionalExamples.ValidateUserRegistration(email, password, age, country);
    
    var result = validation.Match(
        errors => $"Validation failed: {string.Join("; ", errors)}",
        user => $"Valid user: {user.Email}, age {user.Age}");
    
    Console.WriteLine($"Registration validation: {result}");
}

// Example 9: Async functional operations
Console.WriteLine("\nAsync Functional Operations:");

await FunctionalExamples.AsyncProcessingExample();

// Example 10: Real-world data processing
Console.WriteLine("\nReal-world Data Processing:");

FunctionalExamples.DataProcessingPipeline();

// Example 11: Mathematical sequences
Console.WriteLine("\nMathematical Sequences:");

FunctionalExamples.MathematicalSequences();

// Example 12: Error handling patterns
Console.WriteLine("\nError Handling Patterns:");

FunctionalExamples.ErrorHandlingExample();

// Example 13: Partition and grouping
Console.WriteLine("\nPartition and Grouping:");

var mixedNumbers = Enumerable.Range(1, 20);
var (evens, odds) = mixedNumbers.Partition(n => n % 2 == 0);

Console.WriteLine($"Evens: [{string.Join(", ", evens)}]");
Console.WriteLine($"Odds: [{string.Join(", ", odds)}]");

// Example 14: Conditional operations
Console.WriteLine("\nConditional Operations:");

bool includeNegatives = false;
bool sortResults = true;

var conditionalResult = Enumerable.Range(-5, 11)
    .When(!includeNegatives, seq => seq.Where(x => x >= 0))
    .Unless(!sortResults, seq => seq.OrderBy(x => x))
    .ToList();

Console.WriteLine($"Conditional result: [{string.Join(", ", conditionalResult)}]");

// Example 15: Thunk for delayed computation
Console.WriteLine("\nThunk (Delayed Computation):");

var expensiveComputation = new LazyFunctional.Thunk<string>(() =>
{
    Console.WriteLine("  Computing expensive value...");
    Thread.Sleep(100); // Simulate expensive work
    return "Expensive Result";
});

Console.WriteLine($"Thunk created, forced: {expensiveComputation.IsForced}");
Console.WriteLine($"Value: {expensiveComputation.Force()}");
Console.WriteLine($"After force, forced: {expensiveComputation.IsForced}");

// Second access is immediate
Console.WriteLine($"Second access: {expensiveComputation.Force()}");
```

**Notes**:

- Maybe monad provides null-safe operations and eliminates null reference exceptions through type safety
- Either monad enables functional error handling with detailed error information and composable operations
- Function composition allows building complex operations from simple, reusable functions
- Currying and partial application enable flexible function parameterization and reuse
- Immutable collections ensure data integrity and thread safety in functional pipelines
- Lazy evaluation enables working with infinite sequences and expensive computations efficiently
- Memoization provides automatic caching for pure functions, dramatically improving performance
- Pipeline pattern creates fluent, readable data transformation chains
- Functional validation combines multiple validation rules with proper error accumulation
- Thunks enable delayed computation and lazy evaluation for expensive operations

**Prerequisites**:

- .NET Core 2.0+ or .NET Framework 4.7+ for advanced LINQ features
- Understanding of functional programming concepts (immutability, pure functions, higher-order functions)
- Familiarity with monadic patterns and functional composition
- Knowledge of lazy evaluation and deferred execution in C#
- Understanding of generic constraints and type system limitations

**Related Snippets**:

- [LINQ Extensions](linq-extensions.md) - Basic LINQ extension methods and operations
- [Performance LINQ](performance-linq.md) - High-performance LINQ optimizations
- [Query Optimization](query-optimization.md) - Database query optimization techniques
- [Immutable Collections](immutable-collections.md) - Advanced immutable data structure patterns
