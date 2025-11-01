# Data Structures

**Description**: Implementation of fundamental data structures with common operations.
**Language/Technology**: C#, Data Structures
**Performance Complexity**: Various operations with different time complexities

## Stack Implementation

**Code**:

```csharp
using System;
using System.Collections;
using System.Collections.Generic;

public class CustomStack<T> : IEnumerable<T>
{
    private T[] _items;
    private int _count;
    private const int DefaultCapacity = 4;
    
    public CustomStack()
    {
        _items = new T[DefaultCapacity];
        _count = 0;
    }
    
    public CustomStack(int capacity)
    {
        _items = new T[capacity];
        _count = 0;
    }
    
    public int Count => _count;
    public bool IsEmpty => _count == 0;
    
    public void Push(T item)
    {
        if (_count == _items.Length)
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
            
        return _items[_count - 1];
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
        T[] newItems = new T[_items.Length * 2];
        Array.Copy(_items, newItems, _count);
        _items = newItems;
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = _count - 1; i >= 0; i--)
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
    private T[] _items;
    private int _head;
    private int _tail;
    private int _count;
    private const int DefaultCapacity = 4;
    
    public CustomQueue()
    {
        _items = new T[DefaultCapacity];
    }
    
    public int Count => _count;
    public bool IsEmpty => _count == 0;
    
    public void Enqueue(T item)
    {
        if (_count == _items.Length)
            Resize();
            
        _items[_tail] = item;
        _tail = (_tail + 1) % _items.Length;
        _count++;
    }
    
    public T Dequeue()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Queue is empty");
            
        T item = _items[_head];
        _items[_head] = default(T);
        _head = (_head + 1) % _items.Length;
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
        T[] newItems = new T[_items.Length * 2];
        
        for (int i = 0; i < _count; i++)
        {
            newItems[i] = _items[(_head + i) % _items.Length];
        }
        
        _items = newItems;
        _head = 0;
        _tail = _count;
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
            yield return _items[(_head + i) % _items.Length];
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
    private Node<T> _head;
    private Node<T> _tail;
    private int _count;
    
    private class Node<TNode>
    {
        public TNode Value { get; set; }
        public Node<TNode> Next { get; set; }
        
        public Node(TNode value)
        {
            Value = value;
        }
    }
    
    public int Count => _count;
    public bool IsEmpty => _head == null;
    
    public void AddFirst(T value)
    {
        var newNode = new Node<T>(value);
        
        if (IsEmpty)
        {
            _head = _tail = newNode;
        }
        else
        {
            newNode.Next = _head;
            _head = newNode;
        }
        
        _count++;
    }
    
    public void AddLast(T value)
    {
        var newNode = new Node<T>(value);
        
        if (IsEmpty)
        {
            _head = _tail = newNode;
        }
        else
        {
            _tail.Next = newNode;
            _tail = newNode;
        }
        
        _count++;
    }
    
    public bool RemoveFirst()
    {
        if (IsEmpty)
            return false;
            
        _head = _head.Next;
        _count--;
        
        if (_head == null)
            _tail = null;
            
        return true;
    }
    
    public bool Contains(T value)
    {
        var current = _head;
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
        var current = _head;
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
