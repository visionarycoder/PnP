namespace CSharp.ProducerConsumer;

public interface IPipeline<TInput, TOutput> : IDisposable
{
    Task<TOutput> ProcessAsync(TInput input, CancellationToken cancellationToken = default);
    Task<IEnumerable<TOutput>> ProcessBatchAsync(IEnumerable<TInput> inputs, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TOutput> ProcessStreamAsync(IAsyncEnumerable<TInput> inputs, CancellationToken cancellationToken = default);
}