# Circuit Breaker Pattern and Resilience Patterns

**Description**: Implementation of the Circuit Breaker pattern and related resilience patterns for fault tolerance, including automatic failure detection, recovery mechanisms, bulkhead isolation, and retry strategies for building robust distributed systems.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Circuit breaker states
public enum CircuitBreakerState
{
    Closed,     // Normal operation, requests flow through
    Open,       // Failures detected, requests fail fast
    HalfOpen    // Testing if service has recovered
}

// Circuit breaker configuration
public class CircuitBreakerOptions
{
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int HalfOpenMaxCalls { get; set; } = 3;
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(60);
    public double FailureRateThreshold { get; set; } = 0.5; // 50%
    public int MinimumThroughput { get; set; } = 10;
}

// Circuit breaker metrics
public class CircuitBreakerMetrics
{
    private readonly object lockObj = new object();
    private readonly Queue<DateTime> recentCalls = new Queue<DateTime>();
    private readonly Queue<DateTime> recentFailures = new Queue<DateTime>();
    
    public int TotalCalls { get; private set; }
    public int FailedCalls { get; private set; }
    public int SuccessfulCalls { get; private set; }
    public DateTime LastFailureTime { get; private set; }
    public DateTime LastSuccessTime { get; private set; }

    public void RecordCall()
    {
        lock (lockObj)
        {
            TotalCalls++;
            recentCalls.Enqueue(DateTime.UtcNow);
        }
    }

    public void RecordSuccess()
    {
        lock (lockObj)
        {
            SuccessfulCalls++;
            LastSuccessTime = DateTime.UtcNow;
        }
    }

    public void RecordFailure()
    {
        lock (lockObj)
        {
            FailedCalls++;
            LastFailureTime = DateTime.UtcNow;
            recentFailures.Enqueue(DateTime.UtcNow);
        }
    }

    public (int calls, int failures) GetRecentStats(TimeSpan window)
    {
        lock (lockObj)
        {
            var cutoff = DateTime.UtcNow - window;
            
            // Clean old entries
            while (recentCalls.Count > 0 && recentCalls.Peek() < cutoff)
                recentCalls.Dequeue();
            
            while (recentFailures.Count > 0 && recentFailures.Peek() < cutoff)
                recentFailures.Dequeue();

            return (recentCalls.Count, recentFailures.Count);
        }
    }

    public double GetFailureRate(TimeSpan window)
    {
        var (calls, failures) = GetRecentStats(window);
        return calls > 0 ? (double)failures / calls : 0;
    }

    public void Reset()
    {
        lock (lockObj)
        {
            TotalCalls = 0;
            FailedCalls = 0;
            SuccessfulCalls = 0;
            recentCalls.Clear();
            recentFailures.Clear();
        }
    }
}

// Circuit breaker exception
public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerState State { get; }
    public TimeSpan RetryAfter { get; }

    public CircuitBreakerOpenException(CircuitBreakerState state, TimeSpan retryAfter)
        : base($"Circuit breaker is {state}. Retry after {retryAfter}")
    {
        State = state;
        RetryAfter = retryAfter;
    }
}

// Main circuit breaker implementation
public class CircuitBreaker
{
    private readonly CircuitBreakerOptions options;
    private readonly ILogger<CircuitBreaker> logger;
    private readonly CircuitBreakerMetrics metrics;
    private readonly object stateLock = new object();

    private CircuitBreakerState state = CircuitBreakerState.Closed;
    private DateTime stateChangeTime = DateTime.UtcNow;
    private int halfOpenCallCount = 0;

    public CircuitBreaker(CircuitBreakerOptions options, ILogger<CircuitBreaker> logger = null)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.logger = logger;
        this.metrics = new CircuitBreakerMetrics();
    }

    public CircuitBreakerState State 
    { 
        get 
        { 
            lock (stateLock) 
            { 
                return state; 
            } 
        } 
    }

    public CircuitBreakerMetrics Metrics => metrics;

    // Execute a function with circuit breaker protection
    public async Task&lt;T&gt; ExecuteAsync&lt;T&gt;(Func<Task&lt;T&gt;> operation, CancellationToken cancellationToken = default)
    {
        if (!CanExecute())
        {
            var retryAfter = GetRetryAfter();
            throw new CircuitBreakerOpenException(State, retryAfter);
        }

        metrics.RecordCall();

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

    // Execute an action with circuit breaker protection
    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (!CanExecute())
        {
            var retryAfter = GetRetryAfter();
            throw new CircuitBreakerOpenException(State, retryAfter);
        }

        metrics.RecordCall();

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

    // Synchronous execution
    public T Execute&lt;T&gt;(Func&lt;T&gt; operation)
    {
        if (!CanExecute())
        {
            var retryAfter = GetRetryAfter();
            throw new CircuitBreakerOpenException(State, retryAfter);
        }

        metrics.RecordCall();

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
        lock (stateLock)
        {
            switch (state)
            {
                case CircuitBreakerState.Closed:
                    return true;

                case CircuitBreakerState.Open:
                    if (DateTime.UtcNow - stateChangeTime >= options.OpenTimeout)
                    {
                        TransitionToHalfOpen();
                        return true;
                    }
                    return false;

                case CircuitBreakerState.HalfOpen:
                    return halfOpenCallCount < options.HalfOpenMaxCalls;

                default:
                    return false;
            }
        }
    }

    private void OnSuccess()
    {
        metrics.RecordSuccess();

        lock (stateLock)
        {
            if (state == CircuitBreakerState.HalfOpen)
            {
                halfOpenCallCount++;
                
                // If all half-open calls succeed, transition to closed
                if (halfOpenCallCount >= options.HalfOpenMaxCalls)
                {
                    TransitionToClosed();
                }
            }
        }

        logger?.LogDebug("Circuit breaker: Operation succeeded. State: {State}", state);
    }

    private void OnFailure(Exception exception)
    {
        metrics.RecordFailure();

        lock (stateLock)
        {
            if (state == CircuitBreakerState.HalfOpen)
            {
                // Any failure in half-open state transitions back to open
                TransitionToOpen();
            }
            else if (state == CircuitBreakerState.Closed)
            {
                // Check if we should transition to open
                var (calls, failures) = metrics.GetRecentStats(options.SamplingDuration);
                
                if (calls >= options.MinimumThroughput)
                {
                    var failureRate = metrics.GetFailureRate(options.SamplingDuration);
                    
                    if (failureRate >= options.FailureRateThreshold)
                    {
                        TransitionToOpen();
                    }
                }
                else if (failures >= options.FailureThreshold)
                {
                    // Fallback to simple failure count
                    TransitionToOpen();
                }
            }
        }

        logger?.LogWarning(exception, "Circuit breaker: Operation failed. State: {State}", state);
    }

    private void TransitionToClosed()
    {
        state = CircuitBreakerState.Closed;
        stateChangeTime = DateTime.UtcNow;
        halfOpenCallCount = 0;
        metrics.Reset();
        
        logger?.LogInformation("Circuit breaker transitioned to CLOSED");
    }

    private void TransitionToOpen()
    {
        state = CircuitBreakerState.Open;
        stateChangeTime = DateTime.UtcNow;
        halfOpenCallCount = 0;
        
        logger?.LogWarning("Circuit breaker transitioned to OPEN");
    }

    private void TransitionToHalfOpen()
    {
        state = CircuitBreakerState.HalfOpen;
        stateChangeTime = DateTime.UtcNow;
        halfOpenCallCount = 0;
        
        logger?.LogInformation("Circuit breaker transitioned to HALF-OPEN");
    }

    private TimeSpan GetRetryAfter()
    {
        lock (stateLock)
        {
            if (state == CircuitBreakerState.Open)
            {
                var elapsed = DateTime.UtcNow - stateChangeTime;
                return options.OpenTimeout - elapsed;
            }
            
            return TimeSpan.Zero;
        }
    }

    // Manual state control
    public void ForceOpen()
    {
        lock (stateLock)
        {
            TransitionToOpen();
        }
    }

    public void ForceClosed()
    {
        lock (stateLock)
        {
            TransitionToClosed();
        }
    }

    public void Reset()
    {
        lock (stateLock)
        {
            TransitionToClosed();
        }
    }
}

// Generic circuit breaker for typed operations
public class CircuitBreaker&lt;T&gt; : CircuitBreaker
{
    public CircuitBreaker(CircuitBreakerOptions options, ILogger<CircuitBreaker&lt;T&gt;> logger = null)
        : base(options, logger as ILogger<CircuitBreaker>)
    {
    }

    public async Task<TResult> ExecuteAsync<TResult>(Func<T, Task<TResult>> operation, T parameter, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(() => operation(parameter), cancellationToken).ConfigureAwait(false);
    }
}

// Circuit breaker registry for managing multiple breakers
public class CircuitBreakerRegistry
{
    private readonly ConcurrentDictionary<string, CircuitBreaker> circuitBreakers = new();
    private readonly ILogger<CircuitBreakerRegistry> logger;

    public CircuitBreakerRegistry(ILogger<CircuitBreakerRegistry> logger = null)
    {
        this.logger = logger;
    }

    public CircuitBreaker GetOrCreate(string name, CircuitBreakerOptions options)
    {
        return circuitBreakers.GetOrAdd(name, _ => 
        {
            logger?.LogInformation("Creating circuit breaker: {Name}", name);
            return new CircuitBreaker(options, logger as ILogger<CircuitBreaker>);
        });
    }

    public CircuitBreaker GetOrCreate(string name, Func<CircuitBreakerOptions> optionsFactory)
    {
        return circuitBreakers.GetOrAdd(name, _ => 
        {
            var options = optionsFactory();
            logger?.LogInformation("Creating circuit breaker: {Name}", name);
            return new CircuitBreaker(options, logger as ILogger<CircuitBreaker>);
        });
    }

    public bool TryGet(string name, out CircuitBreaker circuitBreaker)
    {
        return circuitBreakers.TryGetValue(name, out circuitBreaker);
    }

    public void Remove(string name)
    {
        if (circuitBreakers.TryRemove(name, out _))
        {
            logger?.LogInformation("Removed circuit breaker: {Name}", name);
        }
    }

    public IEnumerable<(string Name, CircuitBreaker CircuitBreaker)> GetAll()
    {
        return circuitBreakers.Select(kvp => (kvp.Key, kvp.Value));
    }

    public void ResetAll()
    {
        foreach (var cb in circuitBreakers.Values)
        {
            cb.Reset();
        }
        
        logger?.LogInformation("Reset all circuit breakers");
    }
}

// Bulkhead isolation pattern
public class BulkheadIsolation
{
    private readonly SemaphoreSlim semaphore;
    private readonly string name;
    private readonly ILogger logger;

    public BulkheadIsolation(string name, int maxConcurrency, ILogger logger = null)
    {
        this.name = name ?? throw new ArgumentNullException(nameof(name));
        this.semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        this.logger = logger;
        
        MaxConcurrency = maxConcurrency;
    }

    public int MaxConcurrency { get; }
    public int CurrentCount => semaphore.CurrentCount;
    public int AvailableCount => MaxConcurrency - CurrentCount;

    public async Task&lt;T&gt; ExecuteAsync&lt;T&gt;(Func<Task&lt;T&gt;> operation, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(operation, TimeSpan.FromMilliseconds(-1), cancellationToken).ConfigureAwait(false);
    }

    public async Task&lt;T&gt; ExecuteAsync&lt;T&gt;(Func<Task&lt;T&gt;> operation, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var acquired = false;
        
        try
        {
            acquired = await semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
            
            if (!acquired)
            {
                throw new TimeoutException($"Bulkhead {name}: Unable to acquire slot within timeout {timeout}");
            }

            logger?.LogDebug("Bulkhead {Name}: Acquired slot. Available: {Available}/{Max}", 
                name, CurrentCount, MaxConcurrency);

            return await operation().ConfigureAwait(false);
        }
        finally
        {
            if (acquired)
            {
                semaphore.Release();
                
                logger?.LogDebug("Bulkhead {Name}: Released slot. Available: {Available}/{Max}", 
                    name, CurrentCount, MaxConcurrency);
            }
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync<object>(async () =>
        {
            await operation().ConfigureAwait(false);
            return null;
        }, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        semaphore?.Dispose();
    }
}

// Retry policy with various strategies
public enum RetryStrategy
{
    FixedInterval,
    ExponentialBackoff,
    LinearBackoff,
    Jitter
}

public class RetryOptions
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public RetryStrategy Strategy { get; set; } = RetryStrategy.ExponentialBackoff;
    public double BackoffMultiplier { get; set; } = 2.0;
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public Func<Exception, bool> RetryPredicate { get; set; } = ex => true;
    public bool UseJitter { get; set; } = true;
}

public class RetryPolicy
{
    private readonly RetryOptions options;
    private readonly ILogger logger;
    private readonly Random jitterRandom = new Random();

    public RetryPolicy(RetryOptions options, ILogger logger = null)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.logger = logger;
    }

    public async Task&lt;T&gt; ExecuteAsync&lt;T&gt;(Func<Task&lt;T&gt;> operation, CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        Exception lastException = null;

        while (attempt <= options.MaxRetries)
        {
            try
            {
                var result = await operation().ConfigureAwait(false);
                
                if (attempt > 0)
                {
                    logger?.LogInformation("Retry succeeded on attempt {Attempt}", attempt + 1);
                }
                
                return result;
            }
            catch (Exception ex) when (options.RetryPredicate(ex))
            {
                lastException = ex;
                attempt++;

                if (attempt > options.MaxRetries)
                {
                    logger?.LogError(ex, "All {MaxRetries} retry attempts failed", options.MaxRetries);
                    break;
                }

                var delay = CalculateDelay(attempt);
                
                logger?.LogWarning(ex, "Operation failed on attempt {Attempt}. Retrying in {Delay}ms", 
                    attempt, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? new InvalidOperationException("Retry failed without exception");
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return Task.CompletedTask;
        }, cancellationToken).ConfigureAwait(false);
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        TimeSpan delay = options.Strategy switch
        {
            RetryStrategy.FixedInterval => options.BaseDelay,
            RetryStrategy.LinearBackoff => TimeSpan.FromMilliseconds(options.BaseDelay.TotalMilliseconds * attempt),
            RetryStrategy.ExponentialBackoff => TimeSpan.FromMilliseconds(
                options.BaseDelay.TotalMilliseconds * Math.Pow(options.BackoffMultiplier, attempt - 1)),
            RetryStrategy.Jitter => CalculateJitteredDelay(attempt),
            _ => options.BaseDelay
        };

        // Apply max delay cap
        if (delay > options.MaxDelay)
            delay = options.MaxDelay;

        // Apply jitter if enabled
        if (options.UseJitter && options.Strategy != RetryStrategy.Jitter)
        {
            var jitterRange = delay.TotalMilliseconds * 0.1; // 10% jitter
            var jitter = (jitterRandom.NextDouble() - 0.5) * 2 * jitterRange;
            delay = TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));
        }

        return delay;
    }

    private TimeSpan CalculateJitteredDelay(int attempt)
    {
        // Full jitter strategy
        var exponentialDelay = options.BaseDelay.TotalMilliseconds * Math.Pow(options.BackoffMultiplier, attempt - 1);
        var jitteredDelay = jitterRandom.NextDouble() * exponentialDelay;
        return TimeSpan.FromMilliseconds(jitteredDelay);
    }
}

// Timeout policy
public class TimeoutPolicy
{
    private readonly TimeSpan timeout;
    private readonly ILogger logger;

    public TimeoutPolicy(TimeSpan timeout, ILogger logger = null)
    {
        this.timeout = timeout;
        this.logger = logger;
    }

    public async Task&lt;T&gt; ExecuteAsync&lt;T&gt;(Func<Task&lt;T&gt;> operation, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            logger?.LogWarning("Operation timed out after {Timeout}", timeout);
            throw new TimeoutException($"Operation timed out after {timeout}");
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return Task.CompletedTask;
        }, cancellationToken).ConfigureAwait(false);
    }
}

// Composite resilience policy combining multiple patterns
public class ResiliencePolicy
{
    private readonly List<IResiliencePolicy> policies = new List<IResiliencePolicy>();
    private readonly ILogger logger;

    public ResiliencePolicy(ILogger logger = null)
    {
        this.logger = logger;
    }

    public ResiliencePolicy AddCircuitBreaker(CircuitBreaker circuitBreaker)
    {
        policies.Add(new CircuitBreakerPolicy(circuitBreaker));
        return this;
    }

    public ResiliencePolicy AddRetry(RetryPolicy retryPolicy)
    {
        policies.Add(new RetryPolicyWrapper(retryPolicy));
        return this;
    }

    public ResiliencePolicy AddTimeout(TimeoutPolicy timeoutPolicy)
    {
        policies.Add(new TimeoutPolicyWrapper(timeoutPolicy));
        return this;
    }

    public ResiliencePolicy AddBulkhead(BulkheadIsolation bulkhead)
    {
        policies.Add(new BulkheadPolicy(bulkhead));
        return this;
    }

    public async Task&lt;T&gt; ExecuteAsync&lt;T&gt;(Func<Task&lt;T&gt;> operation, CancellationToken cancellationToken = default)
    {
        Func<Task&lt;T&gt;> wrappedOperation = operation;

        // Wrap operation with policies in reverse order (last added = innermost)
        for (int i = policies.Count - 1; i >= 0; i--)
        {
            var policy = policies[i];
            var currentOperation = wrappedOperation;
            wrappedOperation = () => policy.ExecuteAsync(currentOperation, cancellationToken);
        }

        return await wrappedOperation().ConfigureAwait(false);
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return Task.CompletedTask;
        }, cancellationToken).ConfigureAwait(false);
    }
}

// Internal interfaces and wrappers for composite policy
internal interface IResiliencePolicy
{
    Task&lt;T&gt; ExecuteAsync&lt;T&gt;(Func<Task&lt;T&gt;> operation, CancellationToken cancellationToken);
}

internal class CircuitBreakerPolicy : IResiliencePolicy
{
    private readonly CircuitBreaker circuitBreaker;

    public CircuitBreakerPolicy(CircuitBreaker circuitBreaker)
    {
        this.circuitBreaker = circuitBreaker;
    }

    public async Task&lt;T&gt; ExecuteAsync&lt;T&gt;(Func<Task&lt;T&gt;> operation, CancellationToken cancellationToken)
    {
        return await circuitBreaker.ExecuteAsync(operation, cancellationToken).ConfigureAwait(false);
    }
}

internal class RetryPolicyWrapper : IResiliencePolicy
{
    private readonly RetryPolicy retryPolicy;

    public RetryPolicyWrapper(RetryPolicy retryPolicy)
    {
        this.retryPolicy = retryPolicy;
    }

    public async Task&lt;T&gt; ExecuteAsync&lt;T&gt;(Func<Task&lt;T&gt;> operation, CancellationToken cancellationToken)
    {
        return await retryPolicy.ExecuteAsync(operation, cancellationToken).ConfigureAwait(false);
    }
}

internal class TimeoutPolicyWrapper : IResiliencePolicy
{
    private readonly TimeoutPolicy timeoutPolicy;

    public TimeoutPolicyWrapper(TimeoutPolicy timeoutPolicy)
    {
        this.timeoutPolicy = timeoutPolicy;
    }

    public async Task&lt;T&gt; ExecuteAsync&lt;T&gt;(Func<Task&lt;T&gt;> operation, CancellationToken cancellationToken)
    {
        return await timeoutPolicy.ExecuteAsync(operation, cancellationToken).ConfigureAwait(false);
    }
}

internal class BulkheadPolicy : IResiliencePolicy
{
    private readonly BulkheadIsolation bulkhead;

    public BulkheadPolicy(BulkheadIsolation bulkhead)
    {
        this.bulkhead = bulkhead;
    }

    public async Task&lt;T&gt; ExecuteAsync&lt;T&gt;(Func<Task&lt;T&gt;> operation, CancellationToken cancellationToken)
    {
        return await bulkhead.ExecuteAsync(operation, cancellationToken).ConfigureAwait(false);
    }
}

// Health monitoring and diagnostics
public class ResilienceHealthMonitor
{
    private readonly CircuitBreakerRegistry registry;
    private readonly ILogger logger;
    private readonly Timer healthCheckTimer;

    public ResilienceHealthMonitor(CircuitBreakerRegistry registry, ILogger logger = null)
    {
        this.registry = registry;
        this.logger = logger;
        
        // Check health every 30 seconds
        this.healthCheckTimer = new Timer(CheckHealth, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    private void CheckHealth(object state)
    {
        try
        {
            var allBreakers = registry.GetAll().ToList();
            var openBreakers = allBreakers.Where(cb => cb.CircuitBreaker.State == CircuitBreakerState.Open).ToList();
            var halfOpenBreakers = allBreakers.Where(cb => cb.CircuitBreaker.State == CircuitBreakerState.HalfOpen).ToList();

            if (openBreakers.Any())
            {
                logger?.LogWarning("Health Check: {Count} circuit breakers are OPEN: {Names}", 
                    openBreakers.Count, string.Join(", ", openBreakers.Select(cb => cb.Name)));
            }

            if (halfOpenBreakers.Any())
            {
                logger?.LogInformation("Health Check: {Count} circuit breakers are HALF-OPEN: {Names}", 
                    halfOpenBreakers.Count, string.Join(", ", halfOpenBreakers.Select(cb => cb.Name)));
            }

            // Log metrics for each circuit breaker
            foreach (var (name, circuitBreaker) in allBreakers)
            {
                var metrics = circuitBreaker.Metrics;
                logger?.LogDebug("Circuit Breaker {Name}: State={State}, Total={Total}, Success={Success}, Failed={Failed}", 
                    name, circuitBreaker.State, metrics.TotalCalls, metrics.SuccessfulCalls, metrics.FailedCalls);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during resilience health check");
        }
    }

    public void Dispose()
    {
        healthCheckTimer?.Dispose();
    }
}

// Extension methods for easier usage
public static class ResilienceExtensions
{
    public static CircuitBreaker CreateCircuitBreaker(this CircuitBreakerRegistry registry, string name)
    {
        return registry.GetOrCreate(name, new CircuitBreakerOptions());
    }

    public static async Task&lt;T&gt; WithCircuitBreaker&lt;T&gt;(this Task&lt;T&gt; task, CircuitBreaker circuitBreaker)
    {
        return await circuitBreaker.ExecuteAsync(() => task).ConfigureAwait(false);
    }

    public static async Task WithCircuitBreaker(this Task task, CircuitBreaker circuitBreaker)
    {
        await circuitBreaker.ExecuteAsync(() => task).ConfigureAwait(false);
    }

    public static async Task&lt;T&gt; WithRetry&lt;T&gt;(this Func<Task&lt;T&gt;> operation, RetryOptions options, ILogger logger = null)
    {
        var retryPolicy = new RetryPolicy(options, logger);
        return await retryPolicy.ExecuteAsync(operation).ConfigureAwait(false);
    }

    public static async Task&lt;T&gt; WithTimeout&lt;T&gt;(this Func<Task&lt;T&gt;> operation, TimeSpan timeout, ILogger logger = null)
    {
        var timeoutPolicy = new TimeoutPolicy(timeout, logger);
        return await timeoutPolicy.ExecuteAsync(operation).ConfigureAwait(false);
    }

    public static async Task&lt;T&gt; WithBulkhead&lt;T&gt;(this Func<Task&lt;T&gt;> operation, BulkheadIsolation bulkhead)
    {
        return await bulkhead.ExecuteAsync(operation).ConfigureAwait(false);
    }
}

// Predefined resilience policies for common scenarios
public static class ResiliencePolicies
{
    public static CircuitBreakerOptions WebServiceDefaults => new CircuitBreakerOptions
    {
        FailureThreshold = 5,
        OpenTimeout = TimeSpan.FromSeconds(30),
        HalfOpenMaxCalls = 3,
        SamplingDuration = TimeSpan.FromMinutes(1),
        FailureRateThreshold = 0.5,
        MinimumThroughput = 10
    };

    public static CircuitBreakerOptions DatabaseDefaults => new CircuitBreakerOptions
    {
        FailureThreshold = 3,
        OpenTimeout = TimeSpan.FromSeconds(60),
        HalfOpenMaxCalls = 2,
        SamplingDuration = TimeSpan.FromMinutes(2),
        FailureRateThreshold = 0.3,
        MinimumThroughput = 5
    };

    public static RetryOptions ExponentialBackoffDefaults => new RetryOptions
    {
        MaxRetries = 3,
        BaseDelay = TimeSpan.FromMilliseconds(100),
        Strategy = RetryStrategy.ExponentialBackoff,
        BackoffMultiplier = 2.0,
        MaxDelay = TimeSpan.FromSeconds(10),
        UseJitter = true
    };

    public static RetryOptions QuickRetryDefaults => new RetryOptions
    {
        MaxRetries = 2,
        BaseDelay = TimeSpan.FromMilliseconds(50),
        Strategy = RetryStrategy.FixedInterval,
        MaxDelay = TimeSpan.FromSeconds(1),
        UseJitter = false
    };

    public static ResiliencePolicy CreateWebServicePolicy(ILogger logger = null)
    {
        var circuitBreaker = new CircuitBreaker(WebServiceDefaults, logger as ILogger<CircuitBreaker>);
        var retryPolicy = new RetryPolicy(ExponentialBackoffDefaults, logger);
        var timeoutPolicy = new TimeoutPolicy(TimeSpan.FromSeconds(30), logger);

        return new ResiliencePolicy(logger)
            .AddTimeout(timeoutPolicy)
            .AddRetry(retryPolicy)
            .AddCircuitBreaker(circuitBreaker);
    }

    public static ResiliencePolicy CreateDatabasePolicy(ILogger logger = null)
    {
        var circuitBreaker = new CircuitBreaker(DatabaseDefaults, logger as ILogger<CircuitBreaker>);
        var retryPolicy = new RetryPolicy(QuickRetryDefaults, logger);
        var timeoutPolicy = new TimeoutPolicy(TimeSpan.FromSeconds(60), logger);

        return new ResiliencePolicy(logger)
            .AddTimeout(timeoutPolicy)
            .AddRetry(retryPolicy)
            .AddCircuitBreaker(circuitBreaker);
    }
}
```

**Usage**:

```csharp
// Example 1: Basic Circuit Breaker Usage
Console.WriteLine("Basic Circuit Breaker Examples:");

var cbOptions = new CircuitBreakerOptions
{
    FailureThreshold = 3,
    OpenTimeout = TimeSpan.FromSeconds(10),
    HalfOpenMaxCalls = 2
};

var circuitBreaker = new CircuitBreaker(cbOptions);

// Simulate service calls
for (int i = 0; i < 10; i++)
{
    try
    {
        var result = await circuitBreaker.ExecuteAsync(async () =>
        {
            // Simulate unreliable service
            await Task.Delay(100);
            
            if (Random.Shared.NextDouble() < 0.7) // 70% failure rate
            {
                throw new HttpRequestException($"Service call {i + 1} failed");
            }
            
            return $"Success on call {i + 1}";
        });
        
        Console.WriteLine($"✓ {result}");
    }
    catch (CircuitBreakerOpenException ex)
    {
        Console.WriteLine($"⚡ Circuit breaker is {ex.State}. Retry after: {ex.RetryAfter}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Call {i + 1} failed: {ex.Message}");
    }
    
    Console.WriteLine($"   State: {circuitBreaker.State}, " +
                     $"Success: {circuitBreaker.Metrics.SuccessfulCalls}, " +
                     $"Failed: {circuitBreaker.Metrics.FailedCalls}");
    
    await Task.Delay(500); // Brief pause between calls
}

// Example 2: Circuit Breaker Registry
Console.WriteLine("\nCircuit Breaker Registry Examples:");

var registry = new CircuitBreakerRegistry();

// Create different circuit breakers for different services
var webServiceCB = registry.GetOrCreate("WebService", ResiliencePolicies.WebServiceDefaults);
var databaseCB = registry.GetOrCreate("Database", ResiliencePolicies.DatabaseDefaults);
var apiCB = registry.GetOrCreate("ExternalAPI", () => new CircuitBreakerOptions
{
    FailureThreshold = 2,
    OpenTimeout = TimeSpan.FromSeconds(15)
});

Console.WriteLine("Created circuit breakers:");
foreach (var (name, cb) in registry.GetAll())
{
    Console.WriteLine($"  {name}: State = {cb.State}");
}

// Example 3: Retry Policy
Console.WriteLine("\nRetry Policy Examples:");

var retryOptions = new RetryOptions
{
    MaxRetries = 3,
    BaseDelay = TimeSpan.FromMilliseconds(100),
    Strategy = RetryStrategy.ExponentialBackoff,
    UseJitter = true,
    RetryPredicate = ex => ex is HttpRequestException || ex is TimeoutException
};

var retryPolicy = new RetryPolicy(retryOptions);

try
{
    var result = await retryPolicy.ExecuteAsync(async () =>
    {
        Console.WriteLine($"  Attempting operation at {DateTime.Now:HH:mm:ss.fff}");
        
        // Simulate failing operation that eventually succeeds
        if (Random.Shared.NextDouble() < 0.6) // 60% failure rate
        {
            throw new HttpRequestException("Temporary service unavailable");
        }
        
        return "Operation succeeded!";
    });
    
    Console.WriteLine($"✓ {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Final failure: {ex.Message}");
}

// Example 4: Bulkhead Isolation
Console.WriteLine("\nBulkhead Isolation Examples:");

var bulkhead = new BulkheadIsolation("DatabasePool", maxConcurrency: 3);

// Simulate concurrent operations
var tasks = new List<Task<string>>();

for (int i = 0; i < 8; i++)
{
    int callId = i + 1;
    
    var task = bulkhead.ExecuteAsync(async () =>
    {
        Console.WriteLine($"  Call {callId} started. Available slots: {bulkhead.CurrentCount}/{bulkhead.MaxConcurrency}");
        
        await Task.Delay(Random.Shared.Next(1000, 3000)); // Simulate work
        
        Console.WriteLine($"  Call {callId} completed");
        return $"Result {callId}";
    });
    
    tasks.Add(task);
    
    await Task.Delay(200); // Stagger the start times
}

// Wait for all tasks with timeout
try
{
    var results = await Task.WhenAll(tasks.Select(t => 
        bulkhead.ExecuteAsync(() => t, TimeSpan.FromSeconds(10))));
    
    Console.WriteLine($"All operations completed: {string.Join(", ", results)}");
}
catch (TimeoutException)
{
    Console.WriteLine("Some operations timed out due to bulkhead limits");
}

bulkhead.Dispose();

// Example 5: Timeout Policy
Console.WriteLine("\nTimeout Policy Examples:");

var timeoutPolicy = new TimeoutPolicy(TimeSpan.FromSeconds(2));

var timeoutTasks = new[]
{
    ("Fast operation", 500),
    ("Slow operation", 3000),
    ("Medium operation", 1500)
};

foreach (var (name, delay) in timeoutTasks)
{
    try
    {
        var result = await timeoutPolicy.ExecuteAsync(async () =>
        {
            await Task.Delay(delay);
            return $"{name} completed";
        });
        
        Console.WriteLine($"✓ {result}");
    }
    catch (TimeoutException ex)
    {
        Console.WriteLine($"⏱ {name} timed out: {ex.Message}");
    }
}

// Example 6: Composite Resilience Policy
Console.WriteLine("\nComposite Resilience Policy Examples:");

var compositePolicy = new ResiliencePolicy()
    .AddTimeout(new TimeoutPolicy(TimeSpan.FromSeconds(5)))
    .AddRetry(new RetryPolicy(new RetryOptions
    {
        MaxRetries = 2,
        BaseDelay = TimeSpan.FromMilliseconds(200),
        Strategy = RetryStrategy.ExponentialBackoff
    }))
    .AddCircuitBreaker(new CircuitBreaker(new CircuitBreakerOptions
    {
        FailureThreshold = 3,
        OpenTimeout = TimeSpan.FromSeconds(10)
    }))
    .AddBulkhead(new BulkheadIsolation("CompositePool", 2));

// Test the composite policy
for (int i = 0; i < 5; i++)
{
    try
    {
        var result = await compositePolicy.ExecuteAsync(async () =>
        {
            Console.WriteLine($"  Composite operation {i + 1} executing...");
            
            // Simulate various failure scenarios
            if (i == 1) await Task.Delay(6000); // Timeout
            if (i == 2) throw new InvalidOperationException("Service error");
            
            await Task.Delay(500);
            return $"Composite result {i + 1}";
        });
        
        Console.WriteLine($"✓ {result}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Composite operation {i + 1} failed: {ex.GetType().Name} - {ex.Message}");
    }
    
    await Task.Delay(1000);
}

// Example 7: Predefined Policies
Console.WriteLine("\nPredefined Policy Examples:");

// Web service policy (timeout + retry + circuit breaker)
var webPolicy = ResiliencePolicies.CreateWebServicePolicy();

try
{
    var webResult = await webPolicy.ExecuteAsync(async () =>
    {
        // Simulate web service call
        await Task.Delay(200);
        
        if (Random.Shared.NextDouble() < 0.3)
            throw new HttpRequestException("Web service temporarily unavailable");
        
        return "Web service response";
    });
    
    Console.WriteLine($"✓ Web policy result: {webResult}");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Web policy failed: {ex.Message}");
}

// Database policy (shorter timeout + quick retry + circuit breaker)
var dbPolicy = ResiliencePolicies.CreateDatabasePolicy();

try
{
    var dbResult = await dbPolicy.ExecuteAsync(async () =>
    {
        // Simulate database operation
        await Task.Delay(100);
        return "Database query result";
    });
    
    Console.WriteLine($"✓ Database policy result: {dbResult}");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Database policy failed: {ex.Message}");
}

// Example 8: Extension Methods
Console.WriteLine("\nExtension Method Examples:");

// Create circuit breaker using extension
var extensionCB = registry.CreateCircuitBreaker("ExtensionTest");

// Use extension methods for fluent API
try
{
    var result = await (async () =>
    {
        await Task.Delay(100);
        return "Extension method result";
    }).WithRetry(new RetryOptions { MaxRetries = 2 });
    
    Console.WriteLine($"✓ Extension retry result: {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Extension retry failed: {ex.Message}");
}

// Chain multiple resilience patterns
try
{
    var chainedResult = await (async () =>
    {
        await Task.Delay(200);
        return "Chained operations result";
    })
    .WithTimeout(TimeSpan.FromSeconds(1))
    .WithCircuitBreaker(extensionCB);
    
    Console.WriteLine($"✓ Chained result: {chainedResult}");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Chained operations failed: {ex.Message}");
}

// Example 9: Health Monitoring
Console.WriteLine("\nHealth Monitoring Examples:");

var healthMonitor = new ResilienceHealthMonitor(registry);

// Simulate some operations to generate metrics
var monitoredCB = registry.GetOrCreate("MonitoredService", new CircuitBreakerOptions
{
    FailureThreshold = 2,
    OpenTimeout = TimeSpan.FromSeconds(5)
});

for (int i = 0; i < 5; i++)
{
    try
    {
        await monitoredCB.ExecuteAsync(async () =>
        {
            if (i < 3) throw new Exception($"Monitored failure {i + 1}");
            return "Success";
        });
    }
    catch
    {
        // Ignore for demonstration
    }
}

Console.WriteLine($"Monitored service state: {monitoredCB.State}");
Console.WriteLine($"Metrics - Total: {monitoredCB.Metrics.TotalCalls}, " +
                 $"Success: {monitoredCB.Metrics.SuccessfulCalls}, " +
                 $"Failed: {monitoredCB.Metrics.FailedCalls}");

// Example 10: Advanced Retry Strategies
Console.WriteLine("\nAdvanced Retry Strategy Examples:");

var strategies = new[]
{
    ("Fixed Interval", RetryStrategy.FixedInterval),
    ("Linear Backoff", RetryStrategy.LinearBackoff),
    ("Exponential Backoff", RetryStrategy.ExponentialBackoff),
    ("Jitter", RetryStrategy.Jitter)
};

foreach (var (name, strategy) in strategies)
{
    Console.WriteLine($"\n{name} Strategy:");
    
    var strategyOptions = new RetryOptions
    {
        MaxRetries = 3,
        BaseDelay = TimeSpan.FromMilliseconds(100),
        Strategy = strategy,
        BackoffMultiplier = 2.0
    };
    
    var strategyPolicy = new RetryPolicy(strategyOptions);
    
    var stopwatch = Stopwatch.StartNew();
    var attempt = 0;
    
    try
    {
        await strategyPolicy.ExecuteAsync(async () =>
        {
            attempt++;
            Console.WriteLine($"  Attempt {attempt} at {stopwatch.ElapsedMilliseconds}ms");
            
            if (attempt < 4) // Fail first 3 attempts
            {
                throw new Exception("Simulated failure");
            }
            
            return "Success";
        });
        
        Console.WriteLine($"  ✓ Succeeded after {attempt} attempts in {stopwatch.ElapsedMilliseconds}ms");
    }
    catch (Exception)
    {
        Console.WriteLine($"  ✗ Failed after {attempt} attempts in {stopwatch.ElapsedMilliseconds}ms");
    }
}

// Cleanup
healthMonitor.Dispose();

Console.WriteLine("\nCircuit breaker and resilience pattern examples completed!");
```

**Notes**:

- Circuit breakers prevent cascading failures by failing fast when services are down
- Use different thresholds and timeouts based on service characteristics (web vs database)
- Bulkhead isolation limits resource consumption and prevents one failing component from affecting others
- Retry policies should use exponential backoff with jitter to avoid thundering herd problems
- Combine multiple resilience patterns for comprehensive fault tolerance
- Monitor circuit breaker states and metrics for operational visibility
- Consider the order of policy composition (timeout → retry → circuit breaker → bulkhead)
- Use predefined policies for common scenarios to ensure consistent behavior
- Test resilience patterns under various failure conditions
- Configure appropriate timeouts based on SLA requirements
- Implement health checks to monitor system resilience
- Use logging for troubleshooting and operational insights

**Prerequisites**:

- Understanding of distributed system failure modes
- Knowledge of async/await and Task-based programming
- Familiarity with dependency injection and logging frameworks
- Understanding of concurrent programming and thread safety
- Knowledge of HTTP status codes and network error handling
- Experience with monitoring and observability tools

**Related Snippets**:

- [Polly Patterns](polly-patterns.md) - Integration with Polly resilience library
- [Exception Handling](exception-handling.md) - Structured exception handling strategies
- [Logging Patterns](logging-patterns.md) - Comprehensive logging and observability
- [Async Patterns](async-enumerable.md) - Asynchronous programming patterns
