namespace Snippets.DesignPatterns.Behavioral.Iterator;

public interface IIterable<T>
{
    IIterator<T> CreateIterator();
}