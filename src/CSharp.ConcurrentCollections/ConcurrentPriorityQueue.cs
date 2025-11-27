namespace CSharp.ConcurrentCollections;

/// <summary>
/// Thread-safe priority queue implementation using concurrent collections.
/// </summary>
/// <typeparam name="T">The type of elements in the priority queue.</typeparam>
public class ConcurrentPriorityQueue<T> where T : IComparable<T>
{
    private readonly object lockObject = new();
    private readonly List<T> heap = new();

    /// <summary>
    /// Adds an item to the priority queue.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Enqueue(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        lock (lockObject)
        {
            heap.Add(item);
            HeapifyUp(heap.Count - 1);
        }
    }

    /// <summary>
    /// Attempts to remove and return the highest priority item.
    /// </summary>
    /// <param name="result">The dequeued item, if any.</param>
    /// <returns>True if an item was dequeued, false if the queue was empty.</returns>
    public bool TryDequeue(out T? result)
    {
        lock (lockObject)
        {
            if (heap.Count == 0)
            {
                result = default;
                return false;
            }

            result = heap[0];
            
            // Move last element to root and heapify down
            heap[0] = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            
            if (heap.Count > 0)
            {
                HeapifyDown(0);
            }
            
            return true;
        }
    }

    /// <summary>
    /// Attempts to peek at the highest priority item without removing it.
    /// </summary>
    /// <param name="result">The highest priority item, if any.</param>
    /// <returns>True if an item was found, false if the queue was empty.</returns>
    public bool TryPeek(out T? result)
    {
        lock (lockObject)
        {
            if (heap.Count == 0)
            {
                result = default;
                return false;
            }

            result = heap[0];
            return true;
        }
    }

    /// <summary>
    /// Gets the current count of items in the priority queue.
    /// </summary>
    public int Count
    {
        get
        {
            lock (lockObject)
            {
                return heap.Count;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the priority queue is empty.
    /// </summary>
    public bool IsEmpty => Count == 0;

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            
            if (heap[index].CompareTo(heap[parentIndex]) <= 0)
                break;
            
            (heap[index], heap[parentIndex]) = (heap[parentIndex], heap[index]);
            index = parentIndex;
        }
    }

    private void HeapifyDown(int index)
    {
        while (true)
        {
            int largest = index;
            int leftChild = 2 * index + 1;
            int rightChild = 2 * index + 2;

            if (leftChild < heap.Count && heap[leftChild].CompareTo(heap[largest]) > 0)
                largest = leftChild;

            if (rightChild < heap.Count && heap[rightChild].CompareTo(heap[largest]) > 0)
                largest = rightChild;

            if (largest == index)
                break;

            (heap[index], heap[largest]) = (heap[largest], heap[index]);
            index = largest;
        }
    }
}