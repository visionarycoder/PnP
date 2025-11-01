using System.Collections;

namespace Algorithm.DataStructures;

/// <summary>
/// A custom linked list implementation with modern C# patterns.
/// </summary>
/// <typeparam name="T">The type of elements in the linked list</typeparam>
public class CustomLinkedList<T> : IEnumerable<T>
{
    private Node<T>? head;
    private Node<T>? tail;
    private int count;
    
    private class Node<TNode>(TNode value)
    {
        public TNode Value { get; set; } = value;
        public Node<TNode>? Next { get; set; }
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
        
        count++;
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
            tail!.Next = newNode;
            tail = newNode;
        }
        
        count++;
    }
    
    public bool RemoveFirst()
    {
        if (IsEmpty)
            return false;
            
        head = head!.Next;
        count--;
        
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