using Microsoft.Extensions.Logging;

namespace CSharp.CircuitBreaker;

/// <summary>
/// Main circuit breaker implementation for fault tolerance
/// </summary>
public class CircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger<CircuitBreaker>? _logger;
    private readonly CircuitBreakerMetrics _metrics;
    private readonly object _stateLock = new();

    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private DateTime _stateChangeTime = DateTime.UtcNow;
    private int _halfOpenCallCount = 0;

    public CircuitBreaker(CircuitBreakerOptions options, ILogger<CircuitBreaker>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _metrics = new CircuitBreakerMetrics();
    }

    public CircuitBreakerState State 
    { 
        get 
        { 
            lock (_stateLock) 
            { 
                return _state; 
            } 
        } 
    }

    public CircuitBreakerMetrics Metrics => _metrics;

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (!CanExecute())
        {
            var retryAfter = GetRetryAfter();
            throw new CircuitBreakerOpenException(State, retryAfter);
        }

        _metrics.RecordCall();

        try
        {
            var result = await operation().ConfigureAwait(false);
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (!CanExecute())
        {
            var retryAfter = GetRetryAfter();
            throw new CircuitBreakerOpenException(State, retryAfter);
        }

        _metrics.RecordCall();

        try
        {
            await operation().ConfigureAwait(false);
            OnSuccess();
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            throw;
        }
    }

    public T Execute<T>(Func<T> operation)
    {
        if (!CanExecute())
        {
            var retryAfter = GetRetryAfter();
            throw new CircuitBreakerOpenException(State, retryAfter);
        }

        _metrics.RecordCall();

        try
        {
            var result = operation();
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            throw;
        }
    }

    private bool CanExecute()
    {
        lock (_stateLock)
        {
            return _state switch
            {
                CircuitBreakerState.Closed => true,
                CircuitBreakerState.Open => CheckOpenTimeout(),
                CircuitBreakerState.HalfOpen => _halfOpenCallCount < _options.HalfOpenMaxCalls,
                _ => false
            };
        }
    }

    private bool CheckOpenTimeout()
    {
        if (DateTime.UtcNow - _stateChangeTime >= _options.OpenTimeout)
        {
            TransitionToHalfOpen();
            return true;
        }
        return false;
    }

    private void OnSuccess()
    {
        _metrics.RecordSuccess();

        lock (_stateLock)
        {
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _halfOpenCallCount++;
                if (_halfOpenCallCount >= _options.HalfOpenMaxCalls)
                {
                    TransitionToClosed();
                }
            }
        }
    }

    private void OnFailure(Exception exception)
    {
        _metrics.RecordFailure();

        lock (_stateLock)
        {
            if (_state == CircuitBreakerState.HalfOpen)
            {
                TransitionToOpen();
            }
            else if (_state == CircuitBreakerState.Closed && ShouldOpenCircuit())
            {
                TransitionToOpen();
            }
        }

        _logger?.LogWarning(exception, "Circuit breaker: Operation failed. State: {State}", _state);
    }

    private bool ShouldOpenCircuit()
    {
        var (calls, failures) = _metrics.GetRecentStats(_options.SamplingDuration);
        
        if (calls >= _options.MinimumThroughput)
        {
            var failureRate = _metrics.GetFailureRate(_options.SamplingDuration);
            return failureRate >= _options.FailureRateThreshold;
        }
        
        return failures >= _options.FailureThreshold;
    }

    private void TransitionToClosed()
    {
        _state = CircuitBreakerState.Closed;
        _stateChangeTime = DateTime.UtcNow;
        _halfOpenCallCount = 0;
        _metrics.Reset();
        _logger?.LogInformation("Circuit breaker transitioned to CLOSED");
    }

    private void TransitionToOpen()
    {
        _state = CircuitBreakerState.Open;
        _stateChangeTime = DateTime.UtcNow;
        _halfOpenCallCount = 0;
        _logger?.LogWarning("Circuit breaker transitioned to OPEN");
    }

    private void TransitionToHalfOpen()
    {
        _state = CircuitBreakerState.HalfOpen;
        _stateChangeTime = DateTime.UtcNow;
        _halfOpenCallCount = 0;
        _logger?.LogInformation("Circuit breaker transitioned to HALF-OPEN");
    }

    private TimeSpan GetRetryAfter()
    {
        lock (_stateLock)
        {
            if (_state == CircuitBreakerState.Open)
            {
                var elapsed = DateTime.UtcNow - _stateChangeTime;
                return _options.OpenTimeout - elapsed;
            }
            return TimeSpan.Zero;
        }
    }

    public void ForceOpen()
    {
        lock (_stateLock)
        {
            TransitionToOpen();
        }
    }

    public void ForceClosed()
    {
        lock (_stateLock)
        {
            TransitionToClosed();
        }
    }

    public void Reset()
    {
        lock (_stateLock)
        {
            TransitionToClosed();
        }
    }
}
