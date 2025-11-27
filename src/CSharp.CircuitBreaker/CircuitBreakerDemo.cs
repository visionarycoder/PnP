using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CSharp.CircuitBreaker;

/// <summary>
/// Demonstrates circuit breaker patterns and resilience strategies for fault tolerance.
/// </summary>
public class CircuitBreakerDemo
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Circuit Breaker and Resilience Patterns Demo ===\n");

        await DemoBasicCircuitBreaker();
        Console.WriteLine();

        await DemoCircuitBreakerRegistry();
        Console.WriteLine();

        await DemoRetryPolicy();
        Console.WriteLine();

        await DemoBulkheadIsolation();
        Console.WriteLine();

        await DemoTimeoutPolicy();
        Console.WriteLine();

        await DemoResilienceHealthMonitor();
    }

    private static async Task DemoBasicCircuitBreaker()
    {
        Console.WriteLine("--- Basic Circuit Breaker Demo ---");

        var cbOptions = new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            OpenTimeout = TimeSpan.FromSeconds(5),
            HalfOpenMaxCalls = 2,
            SamplingDuration = TimeSpan.FromSeconds(30),
            FailureRateThreshold = 0.6,
            MinimumThroughput = 5
        };

        var circuitBreaker = new CircuitBreaker(cbOptions);
        var callCount = 0;

        Console.WriteLine("Simulating service calls with 70% failure rate:");

        for (int i = 0; i < 12; i++)
        {
            try
            {
                var result = await circuitBreaker.ExecuteAsync(async () =>
                {
                    callCount++;
                    // Simulate unreliable service
                    await Task.Delay(100);
                    
                    if (Random.Shared.NextDouble() < 0.7) // 70% failure rate
                    {
                        throw new HttpRequestException($"Service call {callCount} failed");
                    }
                    
                    return $"Success on call {callCount}";
                });
                
                Console.WriteLine($"✓ {result}");
            }
            catch (CircuitBreakerOpenException ex)
            {
                Console.WriteLine($"⚡ Circuit breaker is {ex.State}. Retry after: {ex.RetryAfter.TotalSeconds:F1}s");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Call {callCount} failed: {ex.Message}");
            }
            
            var metrics = circuitBreaker.Metrics;
            Console.WriteLine($"   State: {circuitBreaker.State}, " +
                             $"Success: {metrics.SuccessfulCalls}, " +
                             $"Failed: {metrics.FailedCalls}, " +
                             $"Rate: {metrics.GetFailureRate(cbOptions.SamplingDuration):P1}");
            
            await Task.Delay(800); // Brief pause between calls
        }

        // Wait for circuit breaker to transition to half-open
        Console.WriteLine("\nWaiting for circuit breaker to transition to half-open...");
        await Task.Delay(6000);

        // Try some more calls
        Console.WriteLine("Attempting calls after timeout:");
        for (int i = 0; i < 3; i++)
        {
            try
            {
                var result = await circuitBreaker.ExecuteAsync(async () =>
                {
                    callCount++;
                    await Task.Delay(100);
                    
                    // Simulate recovery - lower failure rate
                    if (Random.Shared.NextDouble() < 0.2) // 20% failure rate
                    {
                        throw new HttpRequestException($"Service call {callCount} failed");
                    }
                    
                    return $"Success on call {callCount} (recovered)";
                });
                
                Console.WriteLine($"✓ {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Call {callCount} failed: {ex.Message}");
            }
            
            Console.WriteLine($"   State: {circuitBreaker.State}");
            await Task.Delay(500);
        }
    }

    private static async Task DemoCircuitBreakerRegistry()
    {
        Console.WriteLine("--- Circuit Breaker Registry Demo ---");

        var registry = new CircuitBreakerRegistry();

        // Create different circuit breakers for different services
        var webServiceCB = registry.GetOrCreate("WebService", ResiliencePolicies.WebServiceDefaults);
        var databaseCB = registry.GetOrCreate("Database", ResiliencePolicies.DatabaseDefaults);
        var apiCB = registry.GetOrCreate("ExternalAPI", new CircuitBreakerOptions
        {
            FailureThreshold = 2,
            OpenTimeout = TimeSpan.FromSeconds(15)
        });

        Console.WriteLine("Created circuit breakers:");
        foreach (var (name, cb) in registry.GetAll())
        {
            Console.WriteLine($"  {name}: State = {cb.State}");
        }

        // Test database circuit breaker
        Console.WriteLine("\nTesting Database circuit breaker:");
        for (int i = 0; i < 5; i++)
        {
            try
            {
                var result = await databaseCB.ExecuteAsync(async () =>
                {
                    await Task.Delay(200);
                    
                    // Simulate database timeouts
                    if (Random.Shared.NextDouble() < 0.8)
                    {
                        throw new TimeoutException("Database connection timeout");
                    }
                    
                    return $"Database query {i + 1} successful";
                });
                
                Console.WriteLine($"✓ {result}");
            }
            catch (CircuitBreakerOpenException ex)
            {
                Console.WriteLine($"⚡ Database circuit breaker is {ex.State}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Database error: {ex.Message}");
            }
            
            Console.WriteLine($"   Database CB State: {databaseCB.State}");
            await Task.Delay(300);
        }
    }

    private static async Task DemoRetryPolicy()
    {
        Console.WriteLine("--- Retry Policy Demo ---");

        var retryOptions = new RetryOptions
        {
            MaxRetries = 4,
            BaseDelay = TimeSpan.FromMilliseconds(200),
            Strategy = RetryStrategy.ExponentialBackoff,
            UseJitter = true,
            RetryPredicate = ex => ex is HttpRequestException || ex is TimeoutException
        };

        var retryPolicy = new RetryPolicy(retryOptions);
        var attemptCount = 0;

        Console.WriteLine("Testing retry with exponential backoff and jitter:");

        try
        {
            var result = await retryPolicy.ExecuteAsync(async () =>
            {
                attemptCount++;
                Console.WriteLine($"  Attempt {attemptCount} at {DateTime.Now:HH:mm:ss.fff}");
                
                // Simulate failing operation that eventually succeeds
                if (attemptCount <= 3)
                {
                    throw new HttpRequestException($"Temporary service unavailable (attempt {attemptCount})");
                }
                
                return "Operation succeeded after retries!";
            });
            
            Console.WriteLine($"✓ {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Final failure: {ex.Message}");
        }

        // Test with different strategies
        Console.WriteLine("\nTesting different retry strategies:");
        
        var strategies = new[]
        {
            (RetryStrategy.FixedInterval, "Fixed Interval"),
            (RetryStrategy.LinearBackoff, "Linear Backoff"),
            (RetryStrategy.ExponentialBackoff, "Exponential Backoff"),
            (RetryStrategy.Jitter, "Full Jitter")
        };

        foreach (var (strategy, name) in strategies)
        {
            Console.WriteLine($"\n{name} Strategy:");
            var options = new RetryOptions
            {
                MaxRetries = 3,
                BaseDelay = TimeSpan.FromMilliseconds(100),
                Strategy = strategy,
                UseJitter = false // Disable for clearer demonstration
            };

            var policy = new RetryPolicy(options);
            var sw = Stopwatch.StartNew();

            try
            {
                await policy.ExecuteAsync(async () =>
                {
                    Console.WriteLine($"  Delay: {sw.ElapsedMilliseconds}ms");
                    throw new HttpRequestException("Simulated failure");
                });
            }
            catch
            {
                // Expected to fail
            }
        }
    }

    private static async Task DemoBulkheadIsolation()
    {
        Console.WriteLine("--- Bulkhead Isolation Demo ---");

        var bulkhead = new BulkheadIsolation("DatabasePool", maxConcurrency: 3);
        var tasks = new List<Task<string>>();

        Console.WriteLine("Simulating 8 concurrent operations with bulkhead limit of 3:");

        for (int i = 0; i < 8; i++)
        {
            int callId = i + 1;
            
            var task = bulkhead.ExecuteAsync(async () =>
            {
                Console.WriteLine($"  Call {callId} started. Available slots: {bulkhead.CurrentCount}/{bulkhead.MaxConcurrency}");
                
                // Simulate varying work duration
                var workDuration = Random.Shared.Next(1000, 2500);
                await Task.Delay(workDuration);
                
                Console.WriteLine($"  Call {callId} completed after {workDuration}ms");
                return $"Result {callId}";
            });
            
            tasks.Add(task);
            
            await Task.Delay(200); // Stagger the start times
        }

        // Wait for all tasks to complete
        try
        {
            var results = await Task.WhenAll(tasks);
            Console.WriteLine($"\nAll operations completed. Results: [{string.Join(", ", results)}]");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Some operations failed: {ex.Message}");
        }

        bulkhead.Dispose();
    }

    private static async Task DemoTimeoutPolicy()
    {
        Console.WriteLine("--- Timeout Policy Demo ---");

        var timeoutPolicy = new TimeoutPolicy(TimeSpan.FromSeconds(2));

        // Test successful operation within timeout
        Console.WriteLine("Testing operation that completes within timeout:");
        try
        {
            var result = await timeoutPolicy.ExecuteAsync(async () =>
            {
                await Task.Delay(1000); // 1 second - within timeout
                return "Fast operation completed";
            });
            
            Console.WriteLine($"✓ {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ {ex.Message}");
        }

        // Test operation that exceeds timeout
        Console.WriteLine("\nTesting operation that exceeds timeout:");
        try
        {
            var result = await timeoutPolicy.ExecuteAsync(async () =>
            {
                await Task.Delay(3000); // 3 seconds - exceeds timeout
                return "Slow operation completed";
            });
            
            Console.WriteLine($"✓ {result}");
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"⏱ {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ {ex.Message}");
        }

        // Test timeout with cancellation
        Console.WriteLine("\nTesting timeout with external cancellation:");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        
        try
        {
            var result = await timeoutPolicy.ExecuteAsync(async () =>
            {
                await Task.Delay(5000, cts.Token); // Will be cancelled
                return "This won't complete";
            }, cts.Token);
            
            Console.WriteLine($"✓ {result}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("🚫 Operation was cancelled externally");
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"⏱ {ex.Message}");
        }
    }

    private static async Task DemoResilienceHealthMonitor()
    {
        Console.WriteLine("--- Resilience Health Monitor Demo ---");

        var registry = new CircuitBreakerRegistry();
        using var healthMonitor = new ResilienceHealthMonitor(registry);

        // Create some circuit breakers and trigger different states
        var serviceCB = registry.GetOrCreate("TestService", new CircuitBreakerOptions
        {
            FailureThreshold = 2,
            OpenTimeout = TimeSpan.FromSeconds(10)
        });

        var apiCB = registry.GetOrCreate("TestAPI", ResiliencePolicies.WebServiceDefaults);

        Console.WriteLine("Creating failures to trigger circuit breaker state changes:");

        // Force some failures to open the circuit breaker
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await serviceCB.ExecuteAsync(() =>
                {
                    throw new HttpRequestException("Simulated service failure");
                });
            }
            catch
            {
                // Expected failures
            }
        }

        Console.WriteLine($"TestService circuit breaker state: {serviceCB.State}");
        Console.WriteLine($"TestAPI circuit breaker state: {apiCB.State}");

        // Let the health monitor run and log status
        Console.WriteLine("\nHealth monitor is running... (check debug output for detailed metrics)");
        await Task.Delay(3000);

        Console.WriteLine("Health monitoring demo completed.");
    }
}