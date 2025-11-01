using System.Collections;

namespace Snippets.DesignPatterns.Behavioral.Iterator;

public class CustomCollection<T> : IIterable<T>, IEnumerable<T>
{
    private readonly List<T> items = [];

    public int Count => items.Count;
    public bool IsEmpty => items.Count == 0;

    public void Add(T item)
    {
        items.Add(item);
    }

    public void AddRange(IEnumerable<T> items)
    {
        this.items.AddRange(items);
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
        get => items[index];
        set => items[index] = value;
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
            yield return items[i];
        }
    }

    public IEnumerable<T> Reverse()
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            yield return items[i];
        }
    }

    public IEnumerable<T> Skip(int count = 2)
    {
        for (int i = 0; i < items.Count; i += count)
        {
            yield return items[i];
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