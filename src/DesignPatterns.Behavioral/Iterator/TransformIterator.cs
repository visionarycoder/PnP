namespace Snippets.DesignPatterns.Behavioral.Iterator;

public class TransformIterator<TSource, TResult>(IList<TSource> items, Func<TSource, TResult> transform)
    : IIterator<TResult>
{
    private readonly IList<TSource> items = items ?? throw new ArgumentNullException(nameof(items));
    private readonly Func<TSource, TResult> transform = transform ?? throw new ArgumentNullException(nameof(transform));
    private int position = -1;

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

        position++;
        Current = transform(items[position]);
        return Current;
    }

    public void Reset()
    {
        position = -1;
        Current = default(TResult)!;
    }

    public TResult Current { get; private set; } = default(TResult)!;
}