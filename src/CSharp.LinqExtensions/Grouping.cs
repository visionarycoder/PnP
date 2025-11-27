using System.Collections;

namespace CSharp.LinqExtensions;

/// <summary>
/// Implementation of IGrouping interface for LINQ extensions.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TElement">The type of the elements.</typeparam>
public class Grouping<TKey, TElement>(TKey key, IEnumerable<TElement> elements) : IGrouping<TKey, TElement>
{
    public TKey Key { get; } = key;

    public IEnumerator<TElement> GetEnumerator() => elements.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}