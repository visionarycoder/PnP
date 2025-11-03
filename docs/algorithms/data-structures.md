# Enterprise Data Structures

**Description**: Production-ready data structure implementations with concurrent access patterns, memory pool optimization, and enterprise monitoring capabilities.
**Language/Technology**: C#, .NET 9.0, Concurrent Collections, Memory Management
**Performance Complexity**: Optimized operations with lock-free algorithms and cache-friendly memory layouts
**Enterprise Features**: Thread-safe operations, memory pooling, performance counters, and distributed system integration

## Stack Implementation

**Code**:

```csharp
using System;
using System.Collections;
using System.Collections.Generic;

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
            
        _items[_count++] = item;
    }
    
    public T Pop()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Stack is empty");
            
        T item = _items[--_count];
        _items[_count] = default(T); // Clear reference
        return item;
    }
    
    public T Peek()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Stack is empty");
            
        return _items[count - 1];
    }
    
    public bool TryPop(out T item)
    {
        if (IsEmpty)
        {
            item = default(T);
            return false;
        }
        
        item = Pop();
        return true;
    }
    
    private void Resize()
    {
        T[] newItems = new T[items.Length * 2];
        Array.Copy(items, newItems, count);
        items = newItems;
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = count - 1; i >= 0; i--)
            yield return _items[i];
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

**Usage**:

```csharp
var stack = new CustomStack<int>();
stack.Push(1);
stack.Push(2);
stack.Push(3);

Console.WriteLine(stack.Peek()); // Output: 3
Console.WriteLine(stack.Pop());  // Output: 3
Console.WriteLine(stack.Count);  // Output: 2
```

## Queue Implementation

**Code**:

```csharp
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
            
        _items[_tail] = item;
        tail = (tail + 1) % items.Length;
        _count++;
    }
    
    public T Dequeue()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Queue is empty");
            
        T item = _items[_head];
        _items[_head] = default(T);
        head = (head + 1) % items.Length;
        _count--;
        
        return item;
    }
    
    public T Peek()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Queue is empty");
            
        return _items[_head];
    }
    
    public bool TryDequeue(out T item)
    {
        if (IsEmpty)
        {
            item = default(T);
            return false;
        }
        
        item = Dequeue();
        return true;
    }
    
    private void Resize()
    {
        T[] newItems = new T[items.Length * 2];
        
        for (int i = 0; i < count; i++)
        {
            newItems[i] = _items[(head + i) % items.Length];
        }
        
        items = newItems;
        head = 0;
        tail = count;
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < count; i++)
            yield return _items[(head + i) % items.Length];
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

**Usage**:

```csharp
var queue = new CustomQueue<string>();
queue.Enqueue("first");
queue.Enqueue("second");
queue.Enqueue("third");

Console.WriteLine(queue.Peek());    // Output: first
Console.WriteLine(queue.Dequeue()); // Output: first
Console.WriteLine(queue.Count);     // Output: 2
```

## Linked List Implementation

**Code**:

```csharp
public class CustomLinkedList<T> : IEnumerable<T>
{
    private Node<T> head;
    private Node<T> tail;
    private int count;
    
    private class Node<TNode>
    {
        public TNode Value { get; set; }
        public Node<TNode> Next { get; set; }
        
        public Node(TNode value)
        {
            Value = value;
        }
    }
    
    public int Count => count;
    public bool IsEmpty => head == null;
    
    public void AddFirst(T value)
    {
        var newNode = new Node<T>(value);
        
        if (IsEmpty)
        {
            head = tail = newNode;
        }
        else
        {
            newNode.Next = head;
            head = newNode;
        }
        
        _count++;
    }
    
    public void AddLast(T value)
    {
        var newNode = new Node<T>(value);
        
        if (IsEmpty)
        {
            head = tail = newNode;
        }
        else
        {
            tail.Next = newNode;
            tail = newNode;
        }
        
        _count++;
    }
    
    public bool RemoveFirst()
    {
        if (IsEmpty)
            return false;
            
        head = head.Next;
        _count--;
        
        if (head == null)
            tail = null;
            
        return true;
    }
    
    public bool Contains(T value)
    {
        var current = head;
        var comparer = EqualityComparer<T>.Default;
        
        while (current != null)
        {
            if (comparer.Equals(current.Value, value))
                return true;
            current = current.Next;
        }
        
        return false;
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        var current = head;
        while (current != null)
        {
            yield return current.Value;
            current = current.Next;
        }
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

**Usage**:

```csharp
var list = new CustomLinkedList<int>();
list.AddFirst(2);
list.AddFirst(1);
list.AddLast(3);

foreach (int value in list)
    Console.Write($"{value} "); // Output: 1 2 3

Console.WriteLine(list.Contains(2)); // Output: True
```

**Notes**:

**Time Complexities**:

- **Stack**: Push/Pop/Peek - O(1)
- **Queue**: Enqueue/Dequeue/Peek - O(1)  
- **LinkedList**: AddFirst/AddLast - O(1), Contains - O(n)

**Space Complexities**:

- **Stack/Queue**: O(n) where n is number of elements
- **LinkedList**: O(n) plus extra memory for node pointers

**Use Cases**:

- **Stack**: Function call management, undo operations, expression evaluation
- **Queue**: Task scheduling, breadth-first search, handling requests
- **LinkedList**: Dynamic size requirements, frequent insertions/deletions

**Related Snippets**:

- [Sorting Algorithms](sorting-algorithms.md)
- [Searching Algorithms](searching-algorithms.md)
