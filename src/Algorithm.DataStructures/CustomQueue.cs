using System.Collections;

namespace Algorithm.DataStructures;

/// <summary>
/// A custom queue implementation with circular buffer and modern C# patterns.
/// </summary>
/// <typeparam name="T">The type of elements in the queue</typeparam>
public class CustomQueue<T> : IEnumerable<T>
{
    private T[] items;
    private int head;
    private int tail;
    private int count;
    private const int DefaultCapacity = 4;
    
    public CustomQueue()
    {
        items = new T[DefaultCapacity];
    }
    
    public int Count => count;
    public bool IsEmpty => count == 0;
    
    public void Enqueue(T item)
    {
        if (count == items.Length)
            Resize();
            
        items[tail] = item;
        tail = (tail + 1) % items.Length;
        count++;
    }
    
    public T Dequeue()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Queue is empty");
            
        var item = items[head];
        items[head] = default;
        head = (head + 1) % items.Length;
        count--;
        
        return item;
    }
    
    public T Peek()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Queue is empty");
            
        return items[head];
    }
    
    public bool TryDequeue(out T? item)
    {
        if (IsEmpty)
        {
            item = default;
            return false;
        }
        
        item = Dequeue();
        return true;
    }
    
    private void Resize()
    {
        var newItems = new T[items.Length * 2];
        
        for (var i = 0; i < count; i++)
        {
            newItems[i] = items[(head + i) % items.Length];
        }
        
        items = newItems;
        head = 0;
        tail = count;
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < count; i++)
            yield return items[(head + i) % items.Length];
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}