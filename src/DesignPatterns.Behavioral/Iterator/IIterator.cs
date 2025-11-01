namespace Snippets.DesignPatterns.Behavioral.Iterator;

public interface IIterator<T>
{
    bool HasNext();
    T Next();
    void Reset();
    T Current { get; }
}