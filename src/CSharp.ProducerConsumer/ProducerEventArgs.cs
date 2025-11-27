using System.Collections.Concurrent;

namespace CSharp.ProducerConsumer;

// Core interfaces for producer-consumer patterns

// Event argument classes
public class ProducerEventArgs<T>(T item, long queueSize) : EventArgs
{
    public T Item { get; } = item;
    public long QueueSize { get; } = queueSize;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

// Channel-based producer implementation

// Channel-based consumer implementation

// Priority-based producer-consumer using multiple channels

// Batch processor for efficient bulk operations

// Backpressure-aware producer with adaptive rate limiting

// Performance metrics for producer-consumer scenarios

// Work item class for examples