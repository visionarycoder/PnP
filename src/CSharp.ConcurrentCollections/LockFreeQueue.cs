using System.Collections;

namespace CSharp.ConcurrentCollections;

/// <summary>
/// Lock-free queue implementation using Michael &amp; Scott algorithm.
/// Provides high-performance thread-safe queue operations without blocking.
/// </summary>
/// <typeparam name="T">The type of elements in the queue. Must be a reference type.</typeparam>
public class LockFreeQueue<T> : IEnumerable<T> where T : class
{
    private volatile Node head;
    private volatile Node tail;

    private class Node
    {
        public volatile T? Value;
        public volatile Node? Next;

        public Node(T? value = null)
        {
            Value = value;
        }
    }

    public LockFreeQueue()
    {
        var sentinel = new Node();
        head = tail = sentinel;
    }

    /// <summary>
    /// Adds an item to the end of the queue in a lock-free manner.
    /// </summary>
    /// <param name="item">The item to enqueue.</param>
    /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
    public void Enqueue(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var newNode = new Node(item);

        while (true)
        {
            var currentTail = tail;
            var tailNext = currentTail.Next;

            // Check if tail still points to the last node
            if (currentTail == tail)
            {
                if (tailNext == null)
                {
                    // Attempt to link new node to the end of the list
                    if (Interlocked.CompareExchange(ref currentTail.Next, newNode, null) == null)
                    {
                        // Successfully added new node, now update tail
                        Interlocked.CompareExchange(ref tail, newNode, currentTail);
                        break;
                    }
                }
                else
                {
                    // Tail was lagging, try to advance it
                    Interlocked.CompareExchange(ref tail, tailNext, currentTail);
                }
            }
        }
    }

    /// <summary>
    /// Attempts to remove and return an item from the front of the queue.
    /// </summary>
    /// <param name="result">The dequeued item, if successful.</param>
    /// <returns>True if an item was dequeued, false if the queue was empty.</returns>
    public bool TryDequeue(out T? result)
    {
        while (true)
        {
            var currentHead = head;
            var currentTail = tail;
            var headNext = currentHead.Next;

            // Check if head still points to the first node
            if (currentHead == head)
            {
                if (currentHead == currentTail)
                {
                    if (headNext == null)
                    {
                        // Queue is empty
                        result = null;
                        return false;
                    }

                    // Tail is lagging, try to advance it
                    Interlocked.CompareExchange(ref tail, headNext, currentTail);
                }
                else
                {
                    if (headNext == null)
                    {
                        // Inconsistent state, retry
                        continue;
                    }

                    // Read value before attempting CAS
                    result = headNext.Value;

                    // Attempt to move head to the next node
                    if (Interlocked.CompareExchange(ref head, headNext, currentHead) == currentHead)
                    {
                        return true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the queue is empty.
    /// Note: This is a snapshot and may change immediately after the call.
    /// </summary>
    public bool IsEmpty => head.Next == null;

    /// <summary>
    /// Gets the approximate count of items in the queue.
    /// Note: This operation requires traversing the entire queue and is O(n).
    /// </summary>
    public int Count
    {
        get
        {
            int count = 0;
            var current = head.Next; // Skip sentinel

            while (current != null)
            {
                count++;
                current = current.Next;
            }

            return count;
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the queue.
    /// Note: The enumerator provides a snapshot of the queue at the time of creation.
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        var current = head.Next; // Skip sentinel

        while (current != null)
        {
            if (current.Value != null)
            {
                yield return current.Value;
            }
            current = current.Next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}