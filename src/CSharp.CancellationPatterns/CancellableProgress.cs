namespace CSharp.CancellationPatterns;

/// <summary>
/// Progress reporter that respects cancellation tokens
/// </summary>
public class CancellableProgress<T> : IProgress<T>
{
    private readonly Action<T> _handler;
    private readonly CancellationToken _cancellationToken;
    private readonly SynchronizationContext? _synchronizationContext;

    /// <summary>
    /// Creates a new cancellable progress reporter
    /// </summary>
    public CancellableProgress(Action<T> handler, CancellationToken cancellationToken = default)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _cancellationToken = cancellationToken;
        _synchronizationContext = SynchronizationContext.Current;
    }

    /// <summary>
    /// Reports progress if not cancelled
    /// </summary>
    public void Report(T value)
    {
        if (_cancellationToken.IsCancellationRequested)
            return;

        if (_synchronizationContext != null)
        {
            _synchronizationContext.Post(_ =>
            {
                if (!_cancellationToken.IsCancellationRequested)
                    _handler(value);
            }, null);
        }
        else
        {
            _handler(value);
        }
    }
}