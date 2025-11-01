namespace Snippets.DesignPatterns.Behavioral.Iterator;

public class ForwardIterator<T>(IList<T> items) : Iterator<T>(items);