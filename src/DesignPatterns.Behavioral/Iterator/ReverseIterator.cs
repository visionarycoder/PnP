namespace Snippets.DesignPatterns.Behavioral.Iterator;

public class ReverseIterator<T> : Iterator<T>
{
    public ReverseIterator(IList<T> items) : base(items)
    {
        Position = items.Count;
    }

    public override bool HasNext()
    {
        return Position - 1 >= 0;
    }

    public override T Next()
    {
        if (!HasNext())
        {
            throw new InvalidOperationException("No more elements");
        }

        Position--;
        return Items[Position];
    }

    public override void Reset()
    {
        Position = Items.Count;
    }
}