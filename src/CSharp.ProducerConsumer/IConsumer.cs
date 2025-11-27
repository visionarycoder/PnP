namespace CSharp.ProducerConsumer;

public interface IConsumer<T> : IDisposable
{
    event EventHandler<ConsumerEventArgs<T>>? ItemConsumed;
    event EventHandler<Exception>? ConsumptionError;
    Task<T> ConsumeAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> ConsumeBatchAsync(int maxItems, CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> ConsumeAllAsync(CancellationToken cancellationToken = default);
}