using System.Collections.Concurrent;

namespace CSharp.FunctionalLinq;

/// <summary>
/// Functional LINQ extensions for enhanced functional programming support
/// </summary>
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
        var cache = new ConcurrentDictionary<T, TResult>();
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
        var results = new List<T>();
        
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