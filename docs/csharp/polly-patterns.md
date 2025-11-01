# Polly Integration Patterns and Advanced Resilience

**Description**: Integration patterns with the Polly resilience library, demonstrating how to combine Polly's powerful policies with custom resilience strategies, including advanced circuit breakers, sophisticated retry strategies, timeout handling, bulkhead isolation, and policy composition for enterprise-grade fault tolerance.

**Language/Technology**: C# / .NET with Polly

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Polly.Bulkhead;
using Polly.Registry;
using Polly.Contrib.DuplicateRequestCollapser;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Outcomes;
using Polly.Contrib.Simmy.Latency;

// Polly policy configuration options
public class PollyPolicyOptions
{
    public RetryPolicyOptions Retry { get; set; } = new RetryPolicyOptions();
    public CircuitBreakerPolicyOptions CircuitBreaker { get; set; } = new CircuitBreakerPolicyOptions();
    public TimeoutPolicyOptions Timeout { get; set; } = new TimeoutPolicyOptions();
    public BulkheadPolicyOptions Bulkhead { get; set; } = new BulkheadPolicyOptions();
    public ChaosEngineeringOptions Chaos { get; set; } = new ChaosEngineeringOptions();
}

public class RetryPolicyOptions
{
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public double BackoffMultiplier { get; set; } = 2.0;
    public bool UseJitter { get; set; } = true;
    public List<HttpStatusCode> RetriableStatusCodes { get; set; } = new List<HttpStatusCode>
    {
        HttpStatusCode.RequestTimeout,
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    };
}

public class CircuitBreakerPolicyOptions
{
    public int HandledEventsAllowedBeforeBreaking { get; set; } = 5;
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);
    public int SamplingDuration { get; set; } = 60;
    public int MinimumThroughput { get; set; } = 10;
    public double FailureThreshold { get; set; } = 0.5;
}

public class TimeoutPolicyOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool OptimisticTimeout { get; set; } = false;
}

public class BulkheadPolicyOptions
{
    public int MaxParallelization { get; set; } = 10;
    public int MaxQueuingActions { get; set; } = 20;
}

public class ChaosEngineeringOptions
{
    public bool Enabled { get; set; } = false;
    public double FaultInjectionRate { get; set; } = 0.1;
    public double LatencyInjectionRate { get; set; } = 0.1;
    public TimeSpan MinLatency { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan MaxLatency { get; set; } = TimeSpan.FromSeconds(5);
}

// Custom policy result extensions
public static class PolicyResultExtensions
{
    public static bool IsSuccess&lt;T&gt;(this PolicyResult&lt;T&gt; result)
    {
        return result.Outcome == OutcomeType.Successful;
    }

    public static bool IsFailure&lt;T&gt;(this PolicyResult&lt;T&gt; result)
    {
        return result.Outcome == OutcomeType.Failure;
    }

    public static bool WasRetried&lt;T&gt;(this PolicyResult&lt;T&gt; result)
    {
        return result.Context.ContainsKey("retryCount") && 
               (int)result.Context["retryCount"] > 0;
    }

    public static int GetRetryCount&lt;T&gt;(this PolicyResult&lt;T&gt; result)
    {
        return result.Context.TryGetValue("retryCount", out var count) ? (int)count : 0;
    }
}

// Advanced Polly policy factory
public class AdvancedPollyPolicyFactory
{
    private readonly ILogger<AdvancedPollyPolicyFactory> logger;
    private readonly PollyPolicyOptions options;

    public AdvancedPollyPolicyFactory(IOptions<PollyPolicyOptions> options, ILogger<AdvancedPollyPolicyFactory> logger = null)
    {
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        this.logger = logger;
    }

    // Create comprehensive HTTP retry policy with advanced features
    public IAsyncPolicy<HttpResponseMessage> CreateHttpRetryPolicy(string policyName = "HttpRetry")
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(r => options.Retry.RetriableStatusCodes.Contains(r.StatusCode))
            .WaitAndRetryAsync(
                retryCount: options.Retry.MaxRetryAttempts,
                sleepDurationProvider: (retryAttempt, context) => CalculateDelay(retryAttempt),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    context["retryCount"] = retryCount;
                    
                    var exception = outcome.Exception;
                    var result = outcome.Result;
                    
                    if (exception != null)
                    {
                        logger?.LogWarning(exception, 
                            "Policy {PolicyName}: Retry {RetryCount} after {Delay}ms due to exception", 
                            policyName, retryCount, timespan.TotalMilliseconds);
                    }
                    else if (result != null)
                    {
                        logger?.LogWarning(
                            "Policy {PolicyName}: Retry {RetryCount} after {Delay}ms due to {StatusCode}", 
                            policyName, retryCount, timespan.TotalMilliseconds, result.StatusCode);
                    }
                });
    }

    // Create advanced circuit breaker with detailed monitoring
    public IAsyncPolicy<HttpResponseMessage> CreateHttpCircuitBreakerPolicy(string policyName = "HttpCircuitBreaker")
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(r => options.Retry.RetriableStatusCodes.Contains(r.StatusCode))
            .AdvancedCircuitBreakerAsync(
                failureThreshold: options.CircuitBreaker.FailureThreshold,
                samplingDuration: TimeSpan.FromSeconds(options.CircuitBreaker.SamplingDuration),
                minimumThroughput: options.CircuitBreaker.MinimumThroughput,
                durationOfBreak: options.CircuitBreaker.DurationOfBreak,
                onBreak: (exception, duration) =>
                {
                    logger?.LogError(exception,
                        "Policy {PolicyName}: Circuit breaker opened for {Duration}ms",
                        policyName, duration.TotalMilliseconds);
                },
                onReset: () =>
                {
                    logger?.LogInformation("Policy {PolicyName}: Circuit breaker closed", policyName);
                },
                onHalfOpen: () =>
                {
                    logger?.LogInformation("Policy {PolicyName}: Circuit breaker half-open", policyName);
                });
    }

    // Create timeout policy with cancellation support
    public IAsyncPolicy&lt;T&gt; CreateTimeoutPolicy&lt;T&gt;(string policyName = "Timeout")
    {
        var timeoutStrategy = options.Timeout.OptimisticTimeout 
            ? TimeoutStrategy.Optimistic 
            : TimeoutStrategy.Pessimistic;

        return Policy.TimeoutAsync&lt;T&gt;(
            timeout: options.Timeout.Timeout,
            timeoutStrategy: timeoutStrategy,
            onTimeout: (context, timespan, task) =>
            {
                logger?.LogWarning(
                    "Policy {PolicyName}: Operation timed out after {Timeout}ms",
                    policyName, timespan.TotalMilliseconds);
                
                return Task.CompletedTask;
            });
    }

    // Create bulkhead isolation policy
    public IAsyncPolicy&lt;T&gt; CreateBulkheadPolicy&lt;T&gt;(string policyName = "Bulkhead")
    {
        return Policy.BulkheadAsync&lt;T&gt;(
            maxParallelization: options.Bulkhead.MaxParallelization,
            maxQueuingActions: options.Bulkhead.MaxQueuingActions,
            onBulkheadRejected: (context) =>
            {
                logger?.LogWarning(
                    "Policy {PolicyName}: Request rejected due to bulkhead limits",
                    policyName);
            });
    }

    // Create chaos engineering policies for testing
    public IAsyncPolicy&lt;T&gt; CreateChaosPolicy&lt;T&gt;(string policyName = "Chaos")
    {
        if (!options.Chaos.Enabled)
        {
            return Policy.NoOpAsync&lt;T&gt;();
        }

        // Fault injection policy
        var faultPolicy = MonkeyPolicy.InjectExceptionAsync&lt;T&gt;(with =>
            with.Fault(new InvalidOperationException("Chaos engineering fault injection"))
                .InjectionRate(options.Chaos.FaultInjectionRate)
                .Enabled());

        // Latency injection policy  
        var latencyPolicy = MonkeyPolicy.InjectLatencyAsync&lt;T&gt;(with =>
            with.Latency(TimeSpan.FromMilliseconds(
                Random.Shared.Next(
                    (int)options.Chaos.MinLatency.TotalMilliseconds,
                    (int)options.Chaos.MaxLatency.TotalMilliseconds)))
                .InjectionRate(options.Chaos.LatencyInjectionRate)
                .Enabled());

        // Combine chaos policies
        return Policy.WrapAsync(faultPolicy, latencyPolicy);
    }

    // Calculate retry delay with jitter
    private TimeSpan CalculateDelay(int retryAttempt)
    {
        var baseDelay = options.Retry.BaseDelay.TotalMilliseconds;
        var exponentialDelay = baseDelay * Math.Pow(options.Retry.BackoffMultiplier, retryAttempt - 1);
        
        // Apply max delay cap
        exponentialDelay = Math.Min(exponentialDelay, options.Retry.MaxDelay.TotalMilliseconds);
        
        // Add jitter if enabled
        if (options.Retry.UseJitter)
        {
            var jitterRange = exponentialDelay * 0.1; // 10% jitter
            var jitter = (Random.Shared.NextDouble() - 0.5) * 2 * jitterRange;
            exponentialDelay = Math.Max(0, exponentialDelay + jitter);
        }
        
        return TimeSpan.FromMilliseconds(exponentialDelay);
    }
}

// Policy registry wrapper for advanced policy management
public class EnhancedPolicyRegistry
{
    private readonly IReadOnlyPolicyRegistry<string> registry;
    private readonly ILogger<EnhancedPolicyRegistry> logger;
    private readonly Dictionary<string, PolicyMetrics> metrics;

    public EnhancedPolicyRegistry(IReadOnlyPolicyRegistry<string> registry, ILogger<EnhancedPolicyRegistry> logger = null)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.logger = logger;
        this.metrics = new Dictionary<string, PolicyMetrics>();
    }

    public async Task&lt;T&gt; ExecuteAsync&lt;T&gt;(string policyName, Func<Context, Task&lt;T&gt;> operation, Context context = null)
    {
        context ??= new Context(policyName);
        
        var policy = registry.Get<IAsyncPolicy&lt;T&gt;>(policyName);
        if (policy == null)
        {
            throw new ArgumentException($"Policy '{policyName}' not found in registry");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var result = await policy.ExecuteAsync(operation, context).ConfigureAwait(false);
            
            RecordSuccess(policyName, stopwatch.Elapsed, context);
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure(policyName, stopwatch.Elapsed, ex, context);
            throw;
        }
    }

    public async Task<PolicyResult&lt;T&gt;> ExecuteAndCaptureAsync&lt;T&gt;(string policyName, Func<Context, Task&lt;T&gt;> operation, Context context = null)
    {
        context ??= new Context(policyName);
        
        var policy = registry.Get<IAsyncPolicy&lt;T&gt;>(policyName);
        if (policy == null)
        {
            throw new ArgumentException($"Policy '{policyName}' not found in registry");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await policy.ExecuteAndCaptureAsync(operation, context).ConfigureAwait(false);
        
        if (result.IsSuccess())
        {
            RecordSuccess(policyName, stopwatch.Elapsed, context);
        }
        else
        {
            RecordFailure(policyName, stopwatch.Elapsed, result.FinalException, context);
        }
        
        return result;
    }

    public PolicyMetrics GetMetrics(string policyName)
    {
        return metrics.TryGetValue(policyName, out var metric) ? metric : new PolicyMetrics();
    }

    public IEnumerable<(string Name, PolicyMetrics Metrics)> GetAllMetrics()
    {
        return metrics.Select(kvp => (kvp.Key, kvp.Value));
    }

    private void RecordSuccess(string policyName, TimeSpan duration, Context context)
    {
        if (!metrics.TryGetValue(policyName, out var metric))
        {
            metric = new PolicyMetrics();
            metrics[policyName] = metric;
        }

        metric.RecordSuccess(duration, context);
        
        logger?.LogDebug("Policy {PolicyName}: Success in {Duration}ms", 
            policyName, duration.TotalMilliseconds);
    }

    private void RecordFailure(string policyName, TimeSpan duration, Exception exception, Context context)
    {
        if (!metrics.TryGetValue(policyName, out var metric))
        {
            metric = new PolicyMetrics();
            metrics[policyName] = metric;
        }

        metric.RecordFailure(duration, exception, context);
        
        logger?.LogWarning(exception, "Policy {PolicyName}: Failure after {Duration}ms", 
            policyName, duration.TotalMilliseconds);
    }
}

// Policy metrics for monitoring and observability
public class PolicyMetrics
{
    private readonly object lockObj = new object();
    
    public long TotalExecutions { get; private set; }
    public long SuccessfulExecutions { get; private set; }
    public long FailedExecutions { get; private set; }
    public TimeSpan TotalDuration { get; private set; }
    public TimeSpan MinDuration { get; private set; } = TimeSpan.MaxValue;
    public TimeSpan MaxDuration { get; private set; }
    public DateTime LastExecutionTime { get; private set; }
    public Exception LastException { get; private set; }

    public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions : 0;
    public double FailureRate => TotalExecutions > 0 ? (double)FailedExecutions / TotalExecutions : 0;
    public TimeSpan AverageDuration => TotalExecutions > 0 ? TimeSpan.FromTicks(TotalDuration.Ticks / TotalExecutions) : TimeSpan.Zero;

    public void RecordSuccess(TimeSpan duration, Context context)
    {
        lock (lockObj)
        {
            TotalExecutions++;
            SuccessfulExecutions++;
            UpdateDuration(duration);
            LastExecutionTime = DateTime.UtcNow;
        }
    }

    public void RecordFailure(TimeSpan duration, Exception exception, Context context)
    {
        lock (lockObj)
        {
            TotalExecutions++;
            FailedExecutions++;
            UpdateDuration(duration);
            LastExecutionTime = DateTime.UtcNow;
            LastException = exception;
        }
    }

    private void UpdateDuration(TimeSpan duration)
    {
        TotalDuration = TotalDuration.Add(duration);
        
        if (duration < MinDuration)
            MinDuration = duration;
        
        if (duration > MaxDuration)
            MaxDuration = duration;
    }

    public void Reset()
    {
        lock (lockObj)
        {
            TotalExecutions = 0;
            SuccessfulExecutions = 0;
            FailedExecutions = 0;
            TotalDuration = TimeSpan.Zero;
            MinDuration = TimeSpan.MaxValue;
            MaxDuration = TimeSpan.Zero;
            LastException = null;
        }
    }
}

// HTTP client factory with integrated Polly policies
public class ResilientHttpClientFactory
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly EnhancedPolicyRegistry policyRegistry;
    private readonly ILogger<ResilientHttpClientFactory> logger;

    public ResilientHttpClientFactory(
        IHttpClientFactory httpClientFactory,
        EnhancedPolicyRegistry policyRegistry,
        ILogger<ResilientHttpClientFactory> logger = null)
    {
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
        this.logger = logger;
    }

    public HttpClient CreateClient(string name, string policyName = null)
    {
        var client = httpClientFactory.CreateClient(name);
        
        if (!string.IsNullOrEmpty(policyName))
        {
            // Wrap the client with resilience policies
            return new ResilientHttpClient(client, policyRegistry, policyName, logger);
        }
        
        return client;
    }

    public async Task<HttpResponseMessage> SendAsync(
        string clientName, 
        string policyName,
        Func<HttpClient, Task<HttpResponseMessage>> operation)
    {
        using var client = CreateClient(clientName);
        
        return await policyRegistry.ExecuteAsync(policyName, async context =>
        {
            context["ClientName"] = clientName;
            context["OperationId"] = Guid.NewGuid().ToString();
            
            return await operation(client).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }
}

// Resilient HTTP client wrapper
public class ResilientHttpClient : HttpClient
{
    private readonly HttpClient innerClient;
    private readonly EnhancedPolicyRegistry policyRegistry;
    private readonly string policyName;
    private readonly ILogger logger;

    public ResilientHttpClient(
        HttpClient innerClient,
        EnhancedPolicyRegistry policyRegistry,
        string policyName,
        ILogger logger = null)
    {
        this.innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        this.policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
        this.policyName = policyName ?? throw new ArgumentNullException(nameof(policyName));
        this.logger = logger;
    }

    public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await policyRegistry.ExecuteAsync<HttpResponseMessage>(policyName, async context =>
        {
            context["RequestUri"] = request.RequestUri?.ToString();
            context["Method"] = request.Method.ToString();
            context["OperationId"] = Guid.NewGuid().ToString();
            
            logger?.LogDebug("Sending {Method} request to {Uri} with policy {PolicyName}",
                request.Method, request.RequestUri, policyName);
            
            return await innerClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            innerClient?.Dispose();
        }
        
        base.Dispose(disposing);
    }
}

// Polly policy builder with fluent API
public class PollyPolicyBuilder
{
    private readonly List<IAsyncPolicy> policies = new List<IAsyncPolicy>();
    private readonly ILogger logger;

    public PollyPolicyBuilder(ILogger logger = null)
    {
        this.logger = logger;
    }

    public PollyPolicyBuilder WithRetry(RetryPolicyOptions options, string name = "Retry")
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: options.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt => CalculateRetryDelay(retryAttempt, options),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    logger?.LogWarning(outcome.Exception,
                        "Policy {PolicyName}: Retry {RetryCount} after {Delay}ms",
                        name, retryCount, timespan.TotalMilliseconds);
                });

        policies.Add(retryPolicy);
        return this;
    }

    public PollyPolicyBuilder WithCircuitBreaker(CircuitBreakerPolicyOptions options, string name = "CircuitBreaker")
    {
        var circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .AdvancedCircuitBreakerAsync(
                failureThreshold: options.FailureThreshold,
                samplingDuration: TimeSpan.FromSeconds(options.SamplingDuration),
                minimumThroughput: options.MinimumThroughput,
                durationOfBreak: options.DurationOfBreak,
                onBreak: (exception, duration) =>
                {
                    logger?.LogError(exception, "Policy {PolicyName}: Circuit breaker opened for {Duration}ms",
                        name, duration.TotalMilliseconds);
                },
                onReset: () =>
                {
                    logger?.LogInformation("Policy {PolicyName}: Circuit breaker closed", name);
                });

        policies.Add(circuitBreakerPolicy);
        return this;
    }

    public PollyPolicyBuilder WithTimeout(TimeoutPolicyOptions options, string name = "Timeout")
    {
        var timeoutStrategy = options.OptimisticTimeout 
            ? TimeoutStrategy.Optimistic 
            : TimeoutStrategy.Pessimistic;

        var timeoutPolicy = Policy.TimeoutAsync(
            timeout: options.Timeout,
            timeoutStrategy: timeoutStrategy,
            onTimeout: (context, timespan, task) =>
            {
                logger?.LogWarning("Policy {PolicyName}: Operation timed out after {Timeout}ms",
                    name, timespan.TotalMilliseconds);
                return Task.CompletedTask;
            });

        policies.Add(timeoutPolicy);
        return this;
    }

    public PollyPolicyBuilder WithBulkhead(BulkheadPolicyOptions options, string name = "Bulkhead")
    {
        var bulkheadPolicy = Policy.BulkheadAsync(
            maxParallelization: options.MaxParallelization,
            maxQueuingActions: options.MaxQueuingActions,
            onBulkheadRejected: context =>
            {
                logger?.LogWarning("Policy {PolicyName}: Request rejected due to bulkhead limits", name);
            });

        policies.Add(bulkheadPolicy);
        return this;
    }

    public PollyPolicyBuilder WithFallback&lt;T&gt;(Func<Context, Task&lt;T&gt;> fallbackAction, string name = "Fallback")
    {
        var fallbackPolicy = Policy&lt;T&gt;
            .Handle<Exception>()
            .FallbackAsync(
                fallbackAction: fallbackAction,
                onFallback: (result, context) =>
                {
                    logger?.LogWarning(result.Exception,
                        "Policy {PolicyName}: Executing fallback action", name);
                    return Task.CompletedTask;
                });

        policies.Add(fallbackPolicy);
        return this;
    }

    public IAsyncPolicy Build()
    {
        if (!policies.Any())
        {
            return Policy.NoOpAsync();
        }

        if (policies.Count == 1)
        {
            return policies[0];
        }

        // Wrap policies in order: outermost to innermost
        return Policy.WrapAsync(policies.ToArray());
    }

    public IAsyncPolicy&lt;T&gt; Build&lt;T&gt;()
    {
        var typedPolicies = policies.Cast<IAsyncPolicy&lt;T&gt;>().ToArray();
        
        if (!typedPolicies.Any())
        {
            return Policy.NoOpAsync&lt;T&gt;();
        }

        if (typedPolicies.Length == 1)
        {
            return typedPolicies[0];
        }

        return Policy.WrapAsync(typedPolicies);
    }

    private TimeSpan CalculateRetryDelay(int retryAttempt, RetryPolicyOptions options)
    {
        var baseDelay = options.BaseDelay.TotalMilliseconds;
        var exponentialDelay = baseDelay * Math.Pow(options.BackoffMultiplier, retryAttempt - 1);
        
        exponentialDelay = Math.Min(exponentialDelay, options.MaxDelay.TotalMilliseconds);
        
        if (options.UseJitter)
        {
            var jitterRange = exponentialDelay * 0.1;
            var jitter = (Random.Shared.NextDouble() - 0.5) * 2 * jitterRange;
            exponentialDelay = Math.Max(0, exponentialDelay + jitter);
        }
        
        return TimeSpan.FromMilliseconds(exponentialDelay);
    }
}

// Advanced policy composition with conditional execution
public class ConditionalPolicyExecutor
{
    private readonly EnhancedPolicyRegistry registry;
    private readonly ILogger<ConditionalPolicyExecutor> logger;

    public ConditionalPolicyExecutor(EnhancedPolicyRegistry registry, ILogger<ConditionalPolicyExecutor> logger = null)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.logger = logger;
    }

    public async Task&lt;T&gt; ExecuteWithConditionsAsync&lt;T&gt;(
        Func<Context, Task&lt;T&gt;> operation,
        params (string PolicyName, Func<Context, bool> Condition)[] conditionalPolicies)
    {
        var context = new Context(Guid.NewGuid().ToString());
        
        // Determine which policies to apply based on conditions
        var applicablePolicies = conditionalPolicies
            .Where(cp => cp.Condition(context))
            .Select(cp => cp.PolicyName)
            .ToList();

        if (!applicablePolicies.Any())
        {
            logger?.LogDebug("No conditional policies matched, executing operation directly");
            return await operation(context).ConfigureAwait(false);
        }

        logger?.LogDebug("Applying conditional policies: {Policies}", string.Join(", ", applicablePolicies));

        // Execute with the first applicable policy (could be enhanced to combine policies)
        return await registry.ExecuteAsync(applicablePolicies.First(), operation, context).ConfigureAwait(false);
    }

    public async Task&lt;T&gt; ExecuteWithDynamicPolicyAsync&lt;T&gt;(
        Func<Context, Task&lt;T&gt;> operation,
        Func<Context, string> policySelector)
    {
        var context = new Context(Guid.NewGuid().ToString());
        var selectedPolicy = policySelector(context);
        
        if (string.IsNullOrEmpty(selectedPolicy))
        {
            logger?.LogDebug("No policy selected, executing operation directly");
            return await operation(context).ConfigureAwait(false);
        }

        logger?.LogDebug("Dynamically selected policy: {PolicyName}", selectedPolicy);
        return await registry.ExecuteAsync(selectedPolicy, operation, context).ConfigureAwait(false);
    }
}

// Dependency injection extensions
public static class PollyServiceCollectionExtensions
{
    public static IServiceCollection AddPollyPolicies(this IServiceCollection services, Action<PollyPolicyOptions> configure = null)
    {
        var options = new PollyPolicyOptions();
        configure?.Invoke(options);
        
        services.Configure<PollyPolicyOptions>(opt =>
        {
            opt.Retry = options.Retry;
            opt.CircuitBreaker = options.CircuitBreaker;
            opt.Timeout = options.Timeout;
            opt.Bulkhead = options.Bulkhead;
            opt.Chaos = options.Chaos;
        });

        services.AddSingleton<AdvancedPollyPolicyFactory>();
        services.AddSingleton<IPolicyRegistry<string>, PolicyRegistry>();
        services.AddSingleton<EnhancedPolicyRegistry>();
        services.AddSingleton<ResilientHttpClientFactory>();
        services.AddSingleton<ConditionalPolicyExecutor>();

        return services;
    }

    public static IServiceCollection AddPollyHttpClient(
        this IServiceCollection services,
        string name,
        string policyName,
        Action<HttpClient> configureClient = null)
    {
        services.AddHttpClient(name, configureClient)
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var registry = serviceProvider.GetRequiredService<EnhancedPolicyRegistry>();
                return Policy.WrapAsync(
                    registry.registry.Get<IAsyncPolicy<HttpResponseMessage>>(policyName)
                );
            });

        return services;
    }
}

// Predefined policy configurations for common scenarios
public static class PollyPolicyPresets
{
    public static PollyPolicyOptions WebApiDefaults => new PollyPolicyOptions
    {
        Retry = new RetryPolicyOptions
        {
            MaxRetryAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromSeconds(10),
            BackoffMultiplier = 2.0,
            UseJitter = true
        },
        CircuitBreaker = new CircuitBreakerPolicyOptions
        {
            HandledEventsAllowedBeforeBreaking = 5,
            DurationOfBreak = TimeSpan.FromSeconds(30),
            SamplingDuration = 60,
            MinimumThroughput = 10,
            FailureThreshold = 0.5
        },
        Timeout = new TimeoutPolicyOptions
        {
            Timeout = TimeSpan.FromSeconds(30),
            OptimisticTimeout = false
        },
        Bulkhead = new BulkheadPolicyOptions
        {
            MaxParallelization = 10,
            MaxQueuingActions = 20
        }
    };

    public static PollyPolicyOptions DatabaseDefaults => new PollyPolicyOptions
    {
        Retry = new RetryPolicyOptions
        {
            MaxRetryAttempts = 2,
            BaseDelay = TimeSpan.FromMilliseconds(50),
            MaxDelay = TimeSpan.FromSeconds(5),
            BackoffMultiplier = 1.5,
            UseJitter = false
        },
        CircuitBreaker = new CircuitBreakerPolicyOptions
        {
            HandledEventsAllowedBeforeBreaking = 3,
            DurationOfBreak = TimeSpan.FromSeconds(60),
            SamplingDuration = 120,
            MinimumThroughput = 5,
            FailureThreshold = 0.3
        },
        Timeout = new TimeoutPolicyOptions
        {
            Timeout = TimeSpan.FromSeconds(60),
            OptimisticTimeout = true
        },
        Bulkhead = new BulkheadPolicyOptions
        {
            MaxParallelization = 5,
            MaxQueuingActions = 10
        }
    };

    public static PollyPolicyOptions ExternalServiceDefaults => new PollyPolicyOptions
    {
        Retry = new RetryPolicyOptions
        {
            MaxRetryAttempts = 5,
            BaseDelay = TimeSpan.FromMilliseconds(200),
            MaxDelay = TimeSpan.FromSeconds(30),
            BackoffMultiplier = 2.0,
            UseJitter = true
        },
        CircuitBreaker = new CircuitBreakerPolicyOptions
        {
            HandledEventsAllowedBeforeBreaking = 8,
            DurationOfBreak = TimeSpan.FromMinutes(2),
            SamplingDuration = 180,
            MinimumThroughput = 15,
            FailureThreshold = 0.6
        },
        Timeout = new TimeoutPolicyOptions
        {
            Timeout = TimeSpan.FromSeconds(120),
            OptimisticTimeout = false
        },
        Bulkhead = new BulkheadPolicyOptions
        {
            MaxParallelization = 20,
            MaxQueuingActions = 50
        }
    };
}

// Health check integration
public class PollyHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly EnhancedPolicyRegistry registry;

    public PollyHealthCheck(EnhancedPolicyRegistry registry)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allMetrics = registry.GetAllMetrics().ToList();
            var unhealthyPolicies = new List<string>();
            var data = new Dictionary<string, object>();

            foreach (var (name, metrics) in allMetrics)
            {
                data[name] = new
                {
                    metrics.TotalExecutions,
                    metrics.SuccessRate,
                    metrics.FailureRate,
                    metrics.AverageDuration,
                    metrics.LastExecutionTime
                };

                // Consider policy unhealthy if failure rate is too high
                if (metrics.TotalExecutions > 10 && metrics.FailureRate > 0.5)
                {
                    unhealthyPolicies.Add(name);
                }
            }

            if (unhealthyPolicies.Any())
            {
                return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                    $"High failure rate in policies: {string.Join(", ", unhealthyPolicies)}", 
                    data: data));
            }

            return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                "All policies are healthy", 
                data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "Error checking policy health", 
                ex));
        }
    }
}
```

**Usage**:

```csharp
// Example 1: Basic Polly Policy Setup
Console.WriteLine("Basic Polly Policy Examples:");

var policyOptions = PollyPolicyPresets.WebApiDefaults;
var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AdvancedPollyPolicyFactory>();
var policyFactory = new AdvancedPollyPolicyFactory(Options.Create(policyOptions), logger);

// Create HTTP retry policy
var httpRetryPolicy = policyFactory.CreateHttpRetryPolicy("WebApiRetry");

// Simulate HTTP operations with retry
using var httpClient = new HttpClient();

for (int i = 0; i < 5; i++)
{
    try
    {
        var response = await httpRetryPolicy.ExecuteAsync(async () =>
        {
            // Simulate unreliable HTTP endpoint
            if (Random.Shared.NextDouble() < 0.6) // 60% failure rate
            {
                throw new HttpRequestException($"HTTP call {i + 1} failed");
            }
            
            await Task.Delay(100); // Simulate network latency
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        
        Console.WriteLine($"✓ HTTP call {i + 1} succeeded: {response.StatusCode}");
        response.Dispose();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ HTTP call {i + 1} failed: {ex.Message}");
    }
    
    await Task.Delay(200);
}

// Example 2: Policy Registry with Metrics
Console.WriteLine("\nPolicy Registry and Metrics Examples:");

var registry = new PolicyRegistry();
var enhancedRegistry = new EnhancedPolicyRegistry(registry);

// Register multiple policies
registry.Add("RetryPolicy", policyFactory.CreateHttpRetryPolicy("RetryPolicy"));
registry.Add("CircuitBreakerPolicy", policyFactory.CreateHttpCircuitBreakerPolicy("CircuitBreakerPolicy"));
registry.Add("TimeoutPolicy", policyFactory.CreateTimeoutPolicy<string>("TimeoutPolicy"));

// Execute operations using registry
for (int i = 0; i < 10; i++)
{
    try
    {
        var result = await enhancedRegistry.ExecuteAsync<string>("RetryPolicy", async context =>
        {
            context["OperationId"] = i.ToString();
            
            if (Random.Shared.NextDouble() < 0.4) // 40% failure rate
            {
                throw new InvalidOperationException($"Operation {i + 1} simulated failure");
            }
            
            await Task.Delay(50);
            return $"Operation {i + 1} result";
        });
        
        Console.WriteLine($"✓ {result}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Operation {i + 1} failed: {ex.Message}");
    }
}

// Display policy metrics
Console.WriteLine("\nPolicy Metrics:");
foreach (var (name, metrics) in enhancedRegistry.GetAllMetrics())
{
    Console.WriteLine($"Policy: {name}");
    Console.WriteLine($"  Total Executions: {metrics.TotalExecutions}");
    Console.WriteLine($"  Success Rate: {metrics.SuccessRate:P2}");
    Console.WriteLine($"  Average Duration: {metrics.AverageDuration.TotalMilliseconds:F1}ms");
    Console.WriteLine();
}

// Example 3: Fluent Policy Builder
Console.WriteLine("Fluent Policy Builder Examples:");

var builder = new PollyPolicyBuilder(logger);

var compositePolicy = builder
    .WithTimeout(new TimeoutPolicyOptions { Timeout = TimeSpan.FromSeconds(2) })
    .WithRetry(new RetryPolicyOptions 
    { 
        MaxRetryAttempts = 3, 
        BaseDelay = TimeSpan.FromMilliseconds(100),
        UseJitter = true
    })
    .WithCircuitBreaker(new CircuitBreakerPolicyOptions
    {
        HandledEventsAllowedBeforeBreaking = 3,
        DurationOfBreak = TimeSpan.FromSeconds(10)
    })
    .Build();

// Test composite policy
for (int i = 0; i < 8; i++)
{
    try
    {
        await compositePolicy.ExecuteAsync(async () =>
        {
            Console.WriteLine($"  Executing composite operation {i + 1}");
            
            // Simulate various failure conditions
            if (i < 3 && Random.Shared.NextDouble() < 0.7) 
                throw new InvalidOperationException("Simulated failure");
            
            if (i == 5) 
                await Task.Delay(3000); // Timeout scenario
            
            await Task.Delay(100);
        });
        
        Console.WriteLine($"✓ Composite operation {i + 1} succeeded");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Composite operation {i + 1} failed: {ex.GetType().Name}");
    }
    
    await Task.Delay(500);
}

// Example 4: Resilient HTTP Client Factory
Console.WriteLine("\nResilient HTTP Client Examples:");

var services = new ServiceCollection();

// Configure Polly policies in DI container
services.AddPollyPolicies(options =>
{
    options.Retry.MaxRetryAttempts = 2;
    options.Retry.BaseDelay = TimeSpan.FromMilliseconds(200);
    options.CircuitBreaker.HandledEventsAllowedBeforeBreaking = 3;
    options.Timeout.Timeout = TimeSpan.FromSeconds(5);
});

services.AddHttpClient();
services.AddLogging(builder => builder.AddConsole());

var serviceProvider = services.BuildServiceProvider();
var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
var policyRegistry = serviceProvider.GetService<EnhancedPolicyRegistry>();

// Register HTTP policies
var httpPolicyFactory = serviceProvider.GetService<AdvancedPollyPolicyFactory>();
policyRegistry.registry.Add("HttpPolicy", httpPolicyFactory.CreateHttpRetryPolicy("HttpPolicy"));

var resilientFactory = new ResilientHttpClientFactory(httpClientFactory, policyRegistry);

// Use resilient HTTP client
try
{
    var response = await resilientFactory.SendAsync("default", "HttpPolicy", async client =>
    {
        // Simulate HTTP request
        if (Random.Shared.NextDouble() < 0.5)
        {
            throw new HttpRequestException("Simulated HTTP error");
        }
        
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Success response")
        };
    });
    
    var content = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"✓ HTTP response: {content}");
    response.Dispose();
}
catch (Exception ex)
{
    Console.WriteLine($"✗ HTTP request failed: {ex.Message}");
}

// Example 5: Conditional Policy Execution
Console.WriteLine("\nConditional Policy Execution Examples:");

var conditionalExecutor = new ConditionalPolicyExecutor(enhancedRegistry);

// Define conditional policies
var conditionalPolicies = new[]
{
    ("RetryPolicy", (Context ctx) => ctx.ContainsKey("RequireRetry")),
    ("CircuitBreakerPolicy", (Context ctx) => ctx.ContainsKey("RequireCircuitBreaker")),
    ("TimeoutPolicy", (Context ctx) => ctx.ContainsKey("RequireTimeout"))
};

// Test different conditions
var scenarios = new[]
{
    new { Name = "No Conditions", Context = new Context() },
    new { Name = "With Retry", Context = new Context { ["RequireRetry"] = true } },
    new { Name = "With Circuit Breaker", Context = new Context { ["RequireCircuitBreaker"] = true } },
    new { Name = "With Timeout", Context = new Context { ["RequireTimeout"] = true } }
};

foreach (var scenario in scenarios)
{
    try
    {
        var result = await conditionalExecutor.ExecuteWithConditionsAsync(async context =>
        {
            // Copy scenario context to execution context
            foreach (var kvp in scenario.Context)
            {
                context[kvp.Key] = kvp.Value;
            }
            
            await Task.Delay(100);
            return $"Result for {scenario.Name}";
        }, conditionalPolicies);
        
        Console.WriteLine($"✓ {scenario.Name}: {result}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ {scenario.Name} failed: {ex.Message}");
    }
}

// Example 6: Dynamic Policy Selection
Console.WriteLine("\nDynamic Policy Selection Examples:");

var requests = new[]
{
    new { Type = "Critical", Data = "Important operation" },
    new { Type = "Normal", Data = "Regular operation" },
    new { Type = "Background", Data = "Background task" }
};

foreach (var request in requests)
{
    try
    {
        var result = await conditionalExecutor.ExecuteWithDynamicPolicyAsync(async context =>
        {
            context["RequestType"] = request.Type;
            context["RequestData"] = request.Data;
            
            // Simulate different failure rates based on request type
            var failureRate = request.Type switch
            {
                "Critical" => 0.2,    // 20% failure rate
                "Normal" => 0.4,      // 40% failure rate
                "Background" => 0.6,  // 60% failure rate
                _ => 0.3
            };
            
            if (Random.Shared.NextDouble() < failureRate)
            {
                throw new InvalidOperationException($"{request.Type} operation failed");
            }
            
            await Task.Delay(50);
            return $"Processed {request.Type} request: {request.Data}";
        }, 
        context =>
        {
            // Select policy based on request type
            return context.TryGetValue("RequestType", out var type) ? type?.ToString() switch
            {
                "Critical" => "CircuitBreakerPolicy",
                "Normal" => "RetryPolicy",
                "Background" => "TimeoutPolicy",
                _ => null
            } : null;
        });
        
        Console.WriteLine($"✓ {result}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ {request.Type} request failed: {ex.Message}");
    }
}

// Example 7: Chaos Engineering with Polly
Console.WriteLine("\nChaos Engineering Examples:");

var chaosOptions = new PollyPolicyOptions
{
    Chaos = new ChaosEngineeringOptions
    {
        Enabled = true,
        FaultInjectionRate = 0.3,
        LatencyInjectionRate = 0.2,
        MinLatency = TimeSpan.FromMilliseconds(500),
        MaxLatency = TimeSpan.FromSeconds(2)
    }
};

var chaosFactory = new AdvancedPollyPolicyFactory(Options.Create(chaosOptions));
var chaosPolicy = chaosFactory.CreateChaosPolicy<string>("ChaosTest");

// Test with chaos engineering
for (int i = 0; i < 10; i++)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    try
    {
        var result = await chaosPolicy.ExecuteAsync(async () =>
        {
            await Task.Delay(100); // Normal operation time
            return $"Chaos test {i + 1} completed";
        });
        
        Console.WriteLine($"✓ {result} (took {stopwatch.ElapsedMilliseconds}ms)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Chaos test {i + 1} failed after {stopwatch.ElapsedMilliseconds}ms: {ex.Message}");
    }
}

// Example 8: Policy Result Capture and Analysis
Console.WriteLine("\nPolicy Result Capture Examples:");

for (int i = 0; i < 5; i++)
{
    var policyResult = await enhancedRegistry.ExecuteAndCaptureAsync<string>("RetryPolicy", async context =>
    {
        context["AttemptNumber"] = i.ToString();
        
        if (Random.Shared.NextDouble() < 0.6) // 60% failure rate
        {
            throw new InvalidOperationException($"Attempt {i + 1} failed");
        }
        
        return $"Attempt {i + 1} succeeded";
    });
    
    if (policyResult.IsSuccess())
    {
        Console.WriteLine($"✓ {policyResult.Result} (Retried: {policyResult.WasRetried()})");
    }
    else
    {
        Console.WriteLine($"✗ Failed with {policyResult.FinalException?.GetType().Name}: {policyResult.FinalException?.Message}");
        Console.WriteLine($"   Retry count: {policyResult.GetRetryCount()}");
    }
}

// Cleanup
serviceProvider?.Dispose();

Console.WriteLine("\nPolly integration pattern examples completed!");
```

**Notes**:

- Polly provides production-ready resilience patterns with extensive configuration options
- Use policy registries to centralize policy management and enable reuse across applications
- Combine multiple policies using Policy.WrapAsync() with careful consideration of execution order
- Implement comprehensive metrics collection for monitoring and observability
- Use chaos engineering policies for testing system resilience under failure conditions
- Configure policies based on service characteristics (web API, database, external services)
- Leverage dependency injection for clean policy management in ASP.NET Core applications
- Consider using optimistic vs pessimistic timeout strategies based on operation characteristics
- Monitor circuit breaker states and policy metrics for operational insights
- Use conditional and dynamic policy execution for context-aware resilience
- Integrate with health checks to monitor overall system resilience
- Test policies thoroughly with various failure scenarios and load conditions

**Prerequisites**:

- Polly NuGet package (Microsoft.Extensions.Http.Polly for HTTP integration)
- Understanding of async/await patterns and Task-based programming
- Familiarity with dependency injection and ASP.NET Core service registration
- Knowledge of HTTP status codes and network error handling
- Understanding of circuit breaker and bulkhead isolation patterns
- Experience with logging and monitoring frameworks

**Related Snippets**:

- [Circuit Breaker](circuit-breaker.md) - Custom circuit breaker implementation
- [Exception Handling](exception-handling.md) - Structured exception handling strategies
- [Logging Patterns](logging-patterns.md) - Comprehensive logging and observability
- [Async Patterns](async-enumerable.md) - Asynchronous programming patterns
