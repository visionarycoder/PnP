namespace CSharp.CancellationPatterns;

/// <summary>
/// Coordinates multiple cancellation tokens and provides centralized cancellation control
/// </summary>
public class CancellationCoordinator : IDisposable
{
    private readonly object _lock = new();
    private readonly List<CancellationTokenSource> _tokenSources = new();
    private volatile bool _isDisposed;

    /// <summary>
    /// Creates a new cancellation token linked to the coordinator
    /// </summary>
    public CancellationToken CreateLinkedToken(CancellationToken parentToken = default)
    {
        ThrowIfDisposed();
        
        lock (_lock)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
            _tokenSources.Add(cts);
            return cts.Token;
        }
    }

    /// <summary>
    /// Creates a new cancellation token with a timeout
    /// </summary>
    public CancellationToken CreateLinkedToken(TimeSpan timeout, CancellationToken parentToken = default)
    {
        ThrowIfDisposed();
        
        lock (_lock)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
            cts.CancelAfter(timeout);
            _tokenSources.Add(cts);
            return cts.Token;
        }
    }

    /// <summary>
    /// Cancels all active tokens managed by this coordinator
    /// </summary>
    public void CancelAll()
    {
        ThrowIfDisposed();
        
        lock (_lock)
        {
            foreach (var cts in _tokenSources.Where(c => !c.IsCancellationRequested))
            {
                try
                {
                    cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Token source was already disposed, ignore
                }
            }
        }
    }

    /// <summary>
    /// Gets the count of active (non-cancelled, non-disposed) token sources
    /// </summary>
    public int ActiveTokenCount
    {
        get
        {
            lock (_lock)
            {
                return _tokenSources.Count(cts => 
                {
                    try
                    {
                        return !cts.IsCancellationRequested;
                    }
                    catch (ObjectDisposedException)
                    {
                        return false;
                    }
                });
            }
        }
    }

    /// <summary>
    /// Cleans up disposed token sources
    /// </summary>
    public void CleanupDisposedSources()
    {
        ThrowIfDisposed();
        
        lock (_lock)
        {
            for (int i = _tokenSources.Count - 1; i >= 0; i--)
            {
                try
                {
                    // Try to access the token to see if it's disposed
                    _ = _tokenSources[i].Token;
                }
                catch (ObjectDisposedException)
                {
                    _tokenSources.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// Disposes all managed token sources
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        lock (_lock)
        {
            foreach (var cts in _tokenSources)
            {
                try
                {
                    cts.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // Already disposed, ignore
                }
            }
            _tokenSources.Clear();
        }

        _isDisposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(CancellationCoordinator));
    }
}