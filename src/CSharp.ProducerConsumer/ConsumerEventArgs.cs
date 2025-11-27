namespace CSharp.ProducerConsumer;

public class ConsumerEventArgs<T>(T item, TimeSpan processingTime) : EventArgs
{
    public T Item { get; } = item;
    public TimeSpan ProcessingTime { get; } = processingTime;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}