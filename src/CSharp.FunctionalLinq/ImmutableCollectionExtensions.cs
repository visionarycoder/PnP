using System.Collections.Immutable;

namespace CSharp.FunctionalLinq;

/// <summary>
/// Functional extensions for immutable collections
/// </summary>
public static class ImmutableCollectionExtensions
{
    // ImmutableList extensions
    public static ImmutableList<TResult> Map<T, TResult>(
        this ImmutableList<T> list,
        Func<T, TResult> selector)
    {
        return list.Select(selector).ToImmutableList();
    }

    public static ImmutableList<T> Filter<T>(
        this ImmutableList<T> list,
        Func<T, bool> predicate)
    {
        return list.Where(predicate).ToImmutableList();
    }

    public static TResult FoldLeft<T, TResult>(
        this ImmutableList<T> list,
        TResult seed,
        Func<TResult, T, TResult> func)
    {
        return list.Aggregate(seed, func);
    }

    // ImmutableArray extensions
    public static ImmutableArray<TResult> Map<T, TResult>(
        this ImmutableArray<T> array,
        Func<T, TResult> selector)
    {
        return array.Select(selector).ToImmutableArray();
    }

    public static ImmutableArray<T> Filter<T>(
        this ImmutableArray<T> array,
        Func<T, bool> predicate)
    {
        return array.Where(predicate).ToImmutableArray();
    }

    public static TResult FoldLeft<T, TResult>(
        this ImmutableArray<T> array,
        TResult seed,
        Func<TResult, T, TResult> func)
    {
        return array.Aggregate(seed, func);
    }

    // ImmutableHashSet extensions
    public static ImmutableHashSet<TResult> Map<T, TResult>(
        this ImmutableHashSet<T> set,
        Func<T, TResult> selector)
    {
        return set.Select(selector).ToImmutableHashSet();
    }

    public static ImmutableHashSet<T> Filter<T>(
        this ImmutableHashSet<T> set,
        Func<T, bool> predicate)
    {
        return set.Where(predicate).ToImmutableHashSet();
    }

    // ImmutableDictionary extensions
    public static ImmutableDictionary<TKey, TResult> MapValues<TKey, TValue, TResult>(
        this ImmutableDictionary<TKey, TValue> dictionary,
        Func<TValue, TResult> selector)
        where TKey : notnull
    {
        return dictionary.ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => selector(kvp.Value));
    }

    public static ImmutableDictionary<TKey, TValue> FilterByValue<TKey, TValue>(
        this ImmutableDictionary<TKey, TValue> dictionary,
        Func<TValue, bool> predicate)
        where TKey : notnull
    {
        return dictionary
            .Where(kvp => predicate(kvp.Value))
            .ToImmutableDictionary();
    }

    public static ImmutableDictionary<TKey, TValue> FilterByKey<TKey, TValue>(
        this ImmutableDictionary<TKey, TValue> dictionary,
        Func<TKey, bool> predicate)
        where TKey : notnull
    {
        return dictionary
            .Where(kvp => predicate(kvp.Key))
            .ToImmutableDictionary();
    }

    // Safe operations that return Maybe
    public static Maybe<T> TryGetAt<T>(this ImmutableList<T> list, int index)
    {
        return index >= 0 && index < list.Count
            ? Maybe<T>.Some(list[index])
            : Maybe<T>.None;
    }

    public static Maybe<T> TryGetAt<T>(this ImmutableArray<T> array, int index)
    {
        return index >= 0 && index < array.Length
            ? Maybe<T>.Some(array[index])
            : Maybe<T>.None;
    }

    public static Maybe<TValue> TryGetValue<TKey, TValue>(
        this ImmutableDictionary<TKey, TValue> dictionary,
        TKey key)
        where TKey : notnull
    {
        return dictionary.TryGetValue(key, out var value)
            ? Maybe<TValue>.Some(value)
            : Maybe<TValue>.None;
    }

    // Functional update operations
    public static ImmutableList<T> UpdateAt<T>(
        this ImmutableList<T> list,
        int index,
        Func<T, T> updater)
    {
        if (index < 0 || index >= list.Count)
            return list;

        return list.SetItem(index, updater(list[index]));
    }

    public static ImmutableArray<T> UpdateAt<T>(
        this ImmutableArray<T> array,
        int index,
        Func<T, T> updater)
    {
        if (index < 0 || index >= array.Length)
            return array;

        return array.SetItem(index, updater(array[index]));
    }

    public static ImmutableDictionary<TKey, TValue> UpdateValue<TKey, TValue>(
        this ImmutableDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TValue, TValue> updater)
        where TKey : notnull
    {
        return dictionary.TryGetValue(key, out var value)
            ? dictionary.SetItem(key, updater(value))
            : dictionary;
    }

    // Grouping operations
    public static ImmutableDictionary<TKey, ImmutableList<TValue>> GroupByToImmutable<TSource, TKey, TValue>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector)
        where TKey : notnull
    {
        return source
            .GroupBy(keySelector, valueSelector)
            .ToImmutableDictionary(
                g => g.Key,
                g => g.ToImmutableList());
    }

    // Zip operations
    public static ImmutableList<TResult> ZipWith<T1, T2, TResult>(
        this ImmutableList<T1> first,
        ImmutableList<T2> second,
        Func<T1, T2, TResult> resultSelector)
    {
        return first.Zip(second, resultSelector).ToImmutableList();
    }

    public static ImmutableArray<TResult> ZipWith<T1, T2, TResult>(
        this ImmutableArray<T1> first,
        ImmutableArray<T2> second,
        Func<T1, T2, TResult> resultSelector)
    {
        return first.Zip(second, resultSelector).ToImmutableArray();
    }
}