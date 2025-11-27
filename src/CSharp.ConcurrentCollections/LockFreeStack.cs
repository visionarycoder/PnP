using System.Collections;
using System.Runtime.CompilerServices;

namespace CSharp.ConcurrentCollections;

/// <summary>
/// Lock-free stack implementation using atomic compare-and-swap operations.
/// Provides thread-safe stack operations without blocking.
/// </summary>
/// <typeparam name="T">The type of elements in the stack.</typeparam>
public class LockFreeStack<T> : IEnumerable<T>
{
    private volatile Node? head;

    private class Node
    {
        public T Value { get; }
        public Node? Next { get; set; }

        public Node(T value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Pushes an item onto the stack in a thread-safe manner.
    /// </summary>
    /// <param name="item">The item to push.</param>
    public void Push(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var newNode = new Node(item);
        
        while (true)
        {
            var currentHead = head;
            newNode.Next = currentHead;
            
            // Atomic compare-and-swap
            if (Interlocked.CompareExchange(ref head, newNode, currentHead) == currentHead)
            {
                break;
            }
            
            // If CAS failed, retry with new head value
        }
    }

    /// <summary>
    /// Attempts to pop an item from the stack in a thread-safe manner.
    /// </summary>
    /// <param name="result">The popped item, if any.</param>
    /// <returns>True if an item was popped, false if the stack was empty.</returns>
    public bool TryPop(out T? result)
    {
        while (true)
        {
            var currentHead = head;
            
            if (currentHead == null)
            {
                result = default(T);
                return false;
            }

            // Atomic compare-and-swap to remove head
            if (Interlocked.CompareExchange(ref head, currentHead.Next, currentHead) == currentHead)
            {
                result = currentHead.Value;
                return true;
            }
            
            // If CAS failed, retry with new head value
        }
    }

    /// <summary>
    /// Gets a value indicating whether the stack is empty.
    /// </summary>
    public bool IsEmpty => head == null;

    /// <summary>
    /// Gets the approximate count of items in the stack.
    /// Note: This is a snapshot and may change during enumeration.
    /// </summary>
    public int Count
    {
        get
        {
            int count = 0;
            var current = head;
            
            while (current != null)
            {
                count++;
                current = current.Next;
            }
            
            return count;
        }
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