using System.Collections;

namespace Algorithm.DataStructures;

/// <summary>
/// A custom stack implementation with generic type support and modern C# patterns.
/// </summary>
/// <typeparam name="T">The type of elements in the stack</typeparam>
public class CustomStack<T> : IEnumerable<T>
{
    private T[] items;
    private int count;
    private const int DefaultCapacity = 4;
    
    public CustomStack()
    {
        items = new T[DefaultCapacity];
        count = 0;
    }
    
    public CustomStack(int capacity)
    {
        items = new T[capacity];
        count = 0;
    }
    
    public int Count => count;
    public bool IsEmpty => count == 0;
    
    public void Push(T item)
    {
        if (count == items.Length)
            Resize();
            
        items[count++] = item;
    }
    
    public T Pop()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Stack is empty");
            
        var item = items[--count];
        items[count] = default; // Clear reference
        return item;
    }
    
    public T Peek()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Stack is empty");
            
        return items[count - 1];
    }
    
    public bool TryPop(out T? item)
    {
        if (IsEmpty)
        {
            item = default;
            return false;
        }
        
        item = Pop();
        return true;
    }
    
    private void Resize()
    {
        var newItems = new T[items.Length * 2];
        Array.Copy(items, newItems, count);
        items = newItems;
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        for (var i = count - 1; i >= 0; i--)
            yield return items[i];
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}