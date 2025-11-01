namespace Snippets.DesignPatterns.Behavioral.Iterator;

public class SkipIterator<T>(IList<T> items, int skipCount) : Iterator<T>(items)
{
    private readonly int skipCount = skipCount > 0 ? skipCount : 1;

    public override T Next()
    {
        if (!HasNext())
        {
            throw new InvalidOperationException("No more elements");
        }

        Position += skipCount;
        return Items[Position];
    }

    public override bool HasNext()
    {
        return Position + skipCount < Items.Count;
    }
}