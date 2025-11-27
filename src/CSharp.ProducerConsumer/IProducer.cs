namespace CSharp.ProducerConsumer;

public interface IProducer<T> : IDisposable
{
    event EventHandler<ProducerEventArgs<T>>? ItemProduced;
    event EventHandler<Exception>? ProductionError;
    bool IsCompleted { get; }
    Task ProduceAsync(T item, CancellationToken cancellationToken = default);
    Task ProduceBatchAsync(IEnumerable<T> items, CancellationToken cancellationToken = default);
    Task CompleteAsync();
}