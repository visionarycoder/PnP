# .NET Aspire Resource Dependencies

**Description**: Dependency management between Aspire services, including resource lifecycle management, dependency resolution, service discovery patterns, and inter-service communication strategies.

**Language/Technology**: C#, .NET Aspire, .NET 9.0

**Code**:

## Resource Dependency Management

```csharp
namespace DocumentProcessor.Aspire.Dependencies;

// Resource dependency container
public interface IResourceDependencyManager
{
    Task<T> GetResourceAsync<T>(string resourceName, CancellationToken cancellationToken = default) where T : class;
    Task<bool> IsResourceAvailableAsync(string resourceName, CancellationToken cancellationToken = default);
    Task WaitForResourceAsync(string resourceName, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<ResourceHealth> CheckResourceHealthAsync(string resourceName, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ResourceStatusUpdate> WatchResourceAsync(string resourceName, CancellationToken cancellationToken = default);
}

public class ResourceDependencyManager : IResourceDependencyManager
{
    private readonly IServiceProvider serviceProvider;
    private readonly IConfiguration configuration;
    private readonly ILogger<ResourceDependencyManager> logger;
    private readonly ConcurrentDictionary<string, ResourceInfo> resourceCache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> resourceLocks = new();

    public ResourceDependencyManager(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<ResourceDependencyManager> logger)
    {serviceProvider = serviceProvider;configuration = configuration;logger = logger;
    }

    public async Task<T> GetResourceAsync<T>(string resourceName, CancellationToken cancellationToken = default) where T : class
    {
        using var activity = Activity.Current?.Source.StartActivity("ResourceDependency.GetResource");
        activity?.SetTag("resource.name", resourceName);
        activity?.SetTag("resource.type", typeof(T).Name);

        var resourceLock =resourceLocks.GetOrAdd(resourceName, _ => new SemaphoreSlim(1, 1));
        
        await resourceLock.WaitAsync(cancellationToken);
        try
        {
            // Check cache first
            if (resourceCache.TryGetValue(resourceName, out var cachedInfo) && 
                cachedInfo.Resource is T cachedResource &&
                cachedInfo.ExpiresAt > DateTime.UtcNow)
            {logger.LogDebug("Retrieved cached resource {ResourceName}", resourceName);
                return cachedResource;
            }

            // Resolve resource
            var resource = await ResolveResourceAsync<T>(resourceName, cancellationToken);
            
            // Cache the resource
            var resourceInfo = new ResourceInfo
            {
                Resource = resource,
                Type = typeof(T),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5) // 5-minute cache
            };resourceCache.AddOrUpdate(resourceName, resourceInfo, (_, _) => resourceInfo);logger.LogDebug("Resolved and cached resource {ResourceName}", resourceName);
            return resource;
        }
        finally
        {
            resourceLock.Release();
        }
    }

    public async Task<bool> IsResourceAvailableAsync(string resourceName, CancellationToken cancellationToken = default)
    {
        try
        {
            var health = await CheckResourceHealthAsync(resourceName, cancellationToken);
            return health.Status == ResourceHealthStatus.Healthy;
        }
        catch (Exception ex)
        {logger.LogWarning(ex, "Failed to check availability of resource {ResourceName}", resourceName);
            return false;
        }
    }

    public async Task WaitForResourceAsync(string resourceName, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("ResourceDependency.WaitForResource");
        activity?.SetTag("resource.name", resourceName);
        activity?.SetTag("timeout.seconds", timeout.TotalSeconds);

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var startTime = DateTime.UtcNow;
        var retryDelay = TimeSpan.FromSeconds(1);
        var maxRetryDelay = TimeSpan.FromSeconds(30);

        while (!combinedCts.Token.IsCancellationRequested)
        {
            try
            {
                if (await IsResourceAvailableAsync(resourceName, combinedCts.Token))
                {
                    var waitTime = DateTime.UtcNow - startTime;logger.LogInformation("Resource {ResourceName} became available after {WaitTime}ms", 
                        resourceName, waitTime.TotalMilliseconds);
                    return;
                }logger.LogDebug("Resource {ResourceName} not yet available, retrying in {RetryDelay}ms", 
                    resourceName, retryDelay.TotalMilliseconds);

                await Task.Delay(retryDelay, combinedCts.Token);
                
                // Exponential backoff
                retryDelay = TimeSpan.FromMilliseconds(Math.Min(retryDelay.TotalMilliseconds * 1.5, maxRetryDelay.TotalMilliseconds));
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                throw new TimeoutException($"Resource {resourceName} did not become available within {timeout.TotalSeconds} seconds");
            }
        }
    }

    public async Task<ResourceHealth> CheckResourceHealthAsync(string resourceName, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("ResourceDependency.CheckHealth");
        activity?.SetTag("resource.name", resourceName);

        try
        {
            var healthChecker = await GetHealthCheckerAsync(resourceName, cancellationToken);
            return await healthChecker.CheckHealthAsync(cancellationToken);
        }
        catch (Exception ex)
        {logger.LogError(ex, "Health check failed for resource {ResourceName}", resourceName);
            return new ResourceHealth
            {
                ResourceName = resourceName,
                Status = ResourceHealthStatus.Unhealthy,
                Error = ex.Message,
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    public async IAsyncEnumerable<ResourceStatusUpdate> WatchResourceAsync(
        string resourceName, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("ResourceDependency.WatchResource");
        activity?.SetTag("resource.name", resourceName);logger.LogDebug("Starting to watch resource {ResourceName}", resourceName);

        var previousHealth = await CheckResourceHealthAsync(resourceName, cancellationToken);
        yield return new ResourceStatusUpdate
        {
            ResourceName = resourceName,
            Status = previousHealth.Status,
            Timestamp = DateTime.UtcNow,
            IsInitial = true
        };

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                var currentHealth = await CheckResourceHealthAsync(resourceName, cancellationToken);
                
                if (currentHealth.Status != previousHealth.Status)
                {logger.LogInformation("Resource {ResourceName} status changed from {PreviousStatus} to {CurrentStatus}",
                        resourceName, previousHealth.Status, currentHealth.Status);

                    yield return new ResourceStatusUpdate
                    {
                        ResourceName = resourceName,
                        Status = currentHealth.Status,
                        PreviousStatus = previousHealth.Status,
                        Timestamp = DateTime.UtcNow,
                        Error = currentHealth.Error
                    };

                    previousHealth = currentHealth;
                }
            }
            catch (Exception ex)
            {logger.LogError(ex, "Error watching resource {ResourceName}", resourceName);
                
                yield return new ResourceStatusUpdate
                {
                    ResourceName = resourceName,
                    Status = ResourceHealthStatus.Unhealthy,
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }
        }
    }

    private async Task<T> ResolveResourceAsync<T>(string resourceName, CancellationToken cancellationToken) where T : class
    {
        // Try to resolve from service provider first
        var service =serviceProvider.GetService<T>();
        if (service != null)
        {
            return service;
        }

        // Try to resolve by name
        var namedService =serviceProvider.GetKeyedService<T>(resourceName);
        if (namedService != null)
        {
            return namedService;
        }

        // Try connection strings for database resources
        if (typeof(T) == typeof(IDbConnection) || typeof(T).IsAssignableTo(typeof(IDbConnection)))
        {
            var connectionString =configuration.GetConnectionString(resourceName);
            if (!string.IsNullOrEmpty(connectionString))
            {
                return CreateDatabaseConnection<T>(connectionString);
            }
        }

        // Try HTTP clients
        if (typeof(T) == typeof(HttpClient) || typeof(T).IsAssignableTo(typeof(HttpClient)))
        {
            var httpClientFactory =serviceProvider.GetService<IHttpClientFactory>();
            if (httpClientFactory != null)
            {
                var httpClient = httpClientFactory.CreateClient(resourceName);
                return (T)(object)httpClient;
            }
        }

        throw new InvalidOperationException($"Unable to resolve resource '{resourceName}' of type {typeof(T).Name}");
    }

    private T CreateDatabaseConnection<T>(string connectionString) where T : class
    {
        // This is a simplified example - in practice, you'd determine the database type
        // and create the appropriate connection type
        if (connectionString.Contains("postgres", StringComparison.OrdinalIgnoreCase))
        {
            var connection = new Npgsql.NpgsqlConnection(connectionString);
            return (T)(object)connection;
        }
        
        throw new NotSupportedException($"Database type not supported for connection string: {connectionString}");
    }

    private async Task<IResourceHealthChecker> GetHealthCheckerAsync(string resourceName, CancellationToken cancellationToken)
    {
        // Try to get a named health checker
        var namedChecker =serviceProvider.GetKeyedService<IResourceHealthChecker>(resourceName);
        if (namedChecker != null)
        {
            return namedChecker;
        }

        // Fall back to default health checker
        var defaultChecker =serviceProvider.GetService<IResourceHealthChecker>();
        if (defaultChecker != null)
        {
            return defaultChecker;
        }

        // Create a basic health checker based on resource type
        return new BasicResourceHealthChecker(resourceName, serviceProvider, configuration, logger);
    }
}

// Resource information cache
public class ResourceInfo
{
    public object Resource { get; init; } = null!;
    public Type Type { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
}

// Resource health checking
public interface IResourceHealthChecker
{
    Task<ResourceHealth> CheckHealthAsync(CancellationToken cancellationToken = default);
}

public class BasicResourceHealthChecker : IResourceHealthChecker
{
    private readonly string resourceName;
    private readonly IServiceProvider serviceProvider;
    private readonly IConfiguration configuration;
    private readonly ILogger logger;

    public BasicResourceHealthChecker(
        string resourceName,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger logger)
    {resourceName = resourceName;serviceProvider = serviceProvider;configuration = configuration;logger = logger;
    }

    public async Task<ResourceHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if it's a database connection
            var connectionString =configuration.GetConnectionString(resourceName);
            if (!string.IsNullOrEmpty(connectionString))
            {
                return await CheckDatabaseHealthAsync(connectionString, cancellationToken);
            }

            // Check if it's an HTTP endpoint
            var httpClient =serviceProvider.GetService<IHttpClientFactory>()?.CreateClient(resourceName);
            if (httpClient != null)
            {
                return await CheckHttpHealthAsync(httpClient, cancellationToken);
            }

            // Default to healthy if no specific check available
            return new ResourceHealth
            {
                ResourceName =resourceName,
                Status = ResourceHealthStatus.Healthy,
                CheckedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new ResourceHealth
            {
                ResourceName =resourceName,
                Status = ResourceHealthStatus.Unhealthy,
                Error = ex.Message,
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<ResourceHealth> CheckDatabaseHealthAsync(string connectionString, CancellationToken cancellationToken)
    {
        try
        {
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            return new ResourceHealth
            {
                ResourceName =resourceName,
                Status = ResourceHealthStatus.Healthy,
                CheckedAt = DateTime.UtcNow,
                Details = new Dictionary<string, object>
                {
                    ["database"] = connection.Database,
                    ["server"] = connection.Host
                }
            };
        }
        catch (Exception ex)
        {
            return new ResourceHealth
            {
                ResourceName =resourceName,
                Status = ResourceHealthStatus.Unhealthy,
                Error = ex.Message,
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<ResourceHealth> CheckHttpHealthAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        try
        {
            // Try to make a simple HEAD request to check connectivity
            using var request = new HttpRequestMessage(HttpMethod.Head, "/health");
            using var response = await httpClient.SendAsync(request, cancellationToken);

            var status = response.IsSuccessStatusCode 
                ? ResourceHealthStatus.Healthy 
                : ResourceHealthStatus.Degraded;

            return new ResourceHealth
            {
                ResourceName =resourceName,
                Status = status,
                CheckedAt = DateTime.UtcNow,
                Details = new Dictionary<string, object>
                {
                    ["statusCode"] = (int)response.StatusCode,
                    ["baseAddress"] = httpClient.BaseAddress?.ToString() ?? "Unknown"
                }
            };
        }
        catch (Exception ex)
        {
            return new ResourceHealth
            {
                ResourceName =resourceName,
                Status = ResourceHealthStatus.Unhealthy,
                Error = ex.Message,
                CheckedAt = DateTime.UtcNow
            };
        }
    }
}
```

## Service Discovery Integration

```csharp
namespace DocumentProcessor.Aspire.Discovery;

// Service discovery interface
public interface IServiceDiscovery
{
    Task<ServiceEndpoint?> DiscoverServiceAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<List<ServiceEndpoint>> DiscoverServicesAsync(string serviceName, CancellationToken cancellationToken = default);
    Task RegisterServiceAsync(ServiceRegistration registration, CancellationToken cancellationToken = default);
    Task UnregisterServiceAsync(string serviceId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ServiceDiscoveryEvent> WatchServicesAsync(string serviceName, CancellationToken cancellationToken = default);
}

// Aspire-based service discovery
public class AspireServiceDiscovery : IServiceDiscovery
{
    private readonly IConfiguration configuration;
    private readonly ILogger<AspireServiceDiscovery> logger;
    private readonly ConcurrentDictionary<string, List<ServiceEndpoint>> serviceCache = new();
    private readonly Timer cacheRefreshTimer;

    public AspireServiceDiscovery(IConfiguration configuration, ILogger<AspireServiceDiscovery> logger)
    {configuration = configuration;logger = logger;
        
        // Refresh cache every 30 secondscacheRefreshTimer = new Timer(RefreshServiceCache, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    public async Task<ServiceEndpoint?> DiscoverServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var services = await DiscoverServicesAsync(serviceName, cancellationToken);
        return services.FirstOrDefault();
    }

    public async Task<List<ServiceEndpoint>> DiscoverServicesAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        using var activity = Activity.Current?.Source.StartActivity("ServiceDiscovery.DiscoverServices");
        activity?.SetTag("service.name", serviceName);

        // Check cache first
        if (serviceCache.TryGetValue(serviceName, out var cachedServices))
        {logger.LogDebug("Found {ServiceCount} cached endpoints for service {ServiceName}", 
                cachedServices.Count, serviceName);
            return cachedServices;
        }

        // Discover from configuration
        var services = await DiscoverFromConfigurationAsync(serviceName, cancellationToken);
        
        // Cache the resultsserviceCache.AddOrUpdate(serviceName, services, (_, _) => services);logger.LogDebug("Discovered {ServiceCount} endpoints for service {ServiceName}", 
            services.Count, serviceName);

        return services;
    }

    public Task RegisterServiceAsync(ServiceRegistration registration, CancellationToken cancellationToken = default)
    {
        // In Aspire, services are typically registered through the AppHost
        // This method could integrate with a service registry if neededlogger.LogInformation("Service registration requested for {ServiceName} at {Endpoint}", 
            registration.ServiceName, registration.Endpoint);
        
        return Task.CompletedTask;
    }

    public Task UnregisterServiceAsync(string serviceId, CancellationToken cancellationToken = default)
    {logger.LogInformation("Service unregistration requested for {ServiceId}", serviceId);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<ServiceDiscoveryEvent> WatchServicesAsync(
        string serviceName, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {logger.LogDebug("Starting to watch services for {ServiceName}", serviceName);

        var previousServices = await DiscoverServicesAsync(serviceName, cancellationToken);
        
        yield return new ServiceDiscoveryEvent
        {
            Type = ServiceDiscoveryEventType.Initial,
            ServiceName = serviceName,
            Endpoints = previousServices,
            Timestamp = DateTime.UtcNow
        };

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                var currentServices = await DiscoverServicesAsync(serviceName, cancellationToken);
                
                if (!ServiceEndpointsEqual(previousServices, currentServices))
                {logger.LogInformation("Service endpoints changed for {ServiceName}: {PreviousCount} -> {CurrentCount}",
                        serviceName, previousServices.Count, currentServices.Count);

                    yield return new ServiceDiscoveryEvent
                    {
                        Type = ServiceDiscoveryEventType.Updated,
                        ServiceName = serviceName,
                        Endpoints = currentServices,
                        PreviousEndpoints = previousServices,
                        Timestamp = DateTime.UtcNow
                    };

                    previousServices = currentServices;
                }
            }
            catch (Exception ex)
            {logger.LogError(ex, "Error watching services for {ServiceName}", serviceName);
                
                yield return new ServiceDiscoveryEvent
                {
                    Type = ServiceDiscoveryEventType.Error,
                    ServiceName = serviceName,
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
            }
        }
    }

    private async Task<List<ServiceEndpoint>> DiscoverFromConfigurationAsync(string serviceName, CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Configuration reading is synchronous
        
        var endpoints = new List<ServiceEndpoint>();

        // Try to find service endpoints in configuration
        var serviceSection =configuration.GetSection($"Services:{serviceName}");
        if (serviceSection.Exists())
        {
            var endpointUrls = serviceSection.GetSection("Endpoints").Get<string[]>();
            if (endpointUrls != null)
            {
                foreach (var url in endpointUrls)
                {
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    {
                        endpoints.Add(new ServiceEndpoint
                        {
                            ServiceName = serviceName,
                            Host = uri.Host,
                            Port = uri.Port,
                            Scheme = uri.Scheme,
                            IsSecure = uri.Scheme == "https",
                            Metadata = new Dictionary<string, string>
                            {
                                ["source"] = "configuration"
                            }
                        });
                    }
                }
            }
        }

        // Try connection strings as fallback
        var connectionString =configuration.GetConnectionString(serviceName);
        if (!string.IsNullOrEmpty(connectionString) && endpoints.Count == 0)
        {
            var endpoint = ParseConnectionStringEndpoint(serviceName, connectionString);
            if (endpoint != null)
            {
                endpoints.Add(endpoint);
            }
        }

        return endpoints;
    }

    private ServiceEndpoint? ParseConnectionStringEndpoint(string serviceName, string connectionString)
    {
        try
        {
            // Parse PostgreSQL connection string
            if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            {
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                return new ServiceEndpoint
                {
                    ServiceName = serviceName,
                    Host = builder.Host ?? "localhost",
                    Port = builder.Port,
                    Scheme = "postgresql",
                    Metadata = new Dictionary<string, string>
                    {
                        ["database"] = builder.Database ?? string.Empty,
                        ["source"] = "connectionString"
                    }
                };
            }

            // Parse Redis connection string
            if (connectionString.Contains("localhost:6379") || connectionString.Contains("redis"))
            {
                var parts = connectionString.Split(':');
                var host = parts.Length > 0 ? parts[0] : "localhost";
                var port = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 6379;

                return new ServiceEndpoint
                {
                    ServiceName = serviceName,
                    Host = host,
                    Port = port,
                    Scheme = "redis",
                    Metadata = new Dictionary<string, string>
                    {
                        ["source"] = "connectionString"
                    }
                };
            }

            return null;
        }
        catch (Exception ex)
        {logger.LogWarning(ex, "Failed to parse connection string for service {ServiceName}", serviceName);
            return null;
        }
    }

    private void RefreshServiceCache(object? state)
    {
        try
        {
            var servicesToRefresh =serviceCache.Keys.ToList();
            
            foreach (var serviceName in servicesToRefresh)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var services = await DiscoverFromConfigurationAsync(serviceName, CancellationToken.None);serviceCache.AddOrUpdate(serviceName, services, (_, _) => services);
                    }
                    catch (Exception ex)
                    {logger.LogWarning(ex, "Failed to refresh cache for service {ServiceName}", serviceName);
                    }
                });
            }
        }
        catch (Exception ex)
        {logger.LogError(ex, "Error during service cache refresh");
        }
    }

    private static bool ServiceEndpointsEqual(List<ServiceEndpoint> list1, List<ServiceEndpoint> list2)
    {
        if (list1.Count != list2.Count) return false;
        
        var set1 = list1.Select(e => $"{e.Host}:{e.Port}").ToHashSet();
        var set2 = list2.Select(e => $"{e.Host}:{e.Port}").ToHashSet();
        
        return set1.SetEquals(set2);
    }

    public void Dispose()
    {
        cacheRefreshTimer?.Dispose();
    }
}

// Dependency-aware HTTP client factory
public class DependencyAwareHttpClientFactory : IHttpClientFactory
{
    private readonly IHttpClientFactory innerFactory;
    private readonly IServiceDiscovery serviceDiscovery;
    private readonly IResourceDependencyManager dependencyManager;
    private readonly ILogger<DependencyAwareHttpClientFactory> logger;

    public DependencyAwareHttpClientFactory(
        IHttpClientFactory innerFactory,
        IServiceDiscovery serviceDiscovery,
        IResourceDependencyManager dependencyManager,
        ILogger<DependencyAwareHttpClientFactory> logger)
    {innerFactory = innerFactory;serviceDiscovery = serviceDiscovery;dependencyManager = dependencyManager;logger = logger;
    }

    public HttpClient CreateClient(string name)
    {
        var client =innerFactory.CreateClient(name);
        
        // If the client doesn't have a base address, try to discover it
        if (client.BaseAddress == null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var endpoint = await serviceDiscovery.DiscoverServiceAsync(name);
                    if (endpoint != null)
                    {
                        client.BaseAddress = new Uri($"{endpoint.Scheme}://{endpoint.Host}:{endpoint.Port}");logger.LogDebug("Set base address for HTTP client {ClientName} to {BaseAddress}", 
                            name, client.BaseAddress);
                    }
                }
                catch (Exception ex)
                {logger.LogWarning(ex, "Failed to discover endpoint for HTTP client {ClientName}", name);
                }
            });
        }

        return client;
    }
}
```

## Data Models

```csharp
namespace DocumentProcessor.Aspire.Models;

// Resource health models
public enum ResourceHealthStatus { Healthy, Degraded, Unhealthy }

public record ResourceHealth
{
    public string ResourceName { get; init; } = string.Empty;
    public ResourceHealthStatus Status { get; init; }
    public string? Error { get; init; }
    public DateTime CheckedAt { get; init; }
    public Dictionary<string, object> Details { get; init; } = new();
}

public record ResourceStatusUpdate
{
    public string ResourceName { get; init; } = string.Empty;
    public ResourceHealthStatus Status { get; init; }
    public ResourceHealthStatus? PreviousStatus { get; init; }
    public DateTime Timestamp { get; init; }
    public string? Error { get; init; }
    public bool IsInitial { get; init; }
}

// Service discovery models
public record ServiceEndpoint
{
    public string ServiceName { get; init; } = string.Empty;
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public string Scheme { get; init; } = string.Empty;
    public bool IsSecure { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

public record ServiceRegistration
{
    public string ServiceId { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public Dictionary<string, string> Tags { get; init; } = new();
    public TimeSpan? Ttl { get; init; }
}

public enum ServiceDiscoveryEventType { Initial, Added, Updated, Removed, Error }

public record ServiceDiscoveryEvent
{
    public ServiceDiscoveryEventType Type { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public List<ServiceEndpoint> Endpoints { get; init; } = new();
    public List<ServiceEndpoint>? PreviousEndpoints { get; init; }
    public DateTime Timestamp { get; init; }
    public string? Error { get; init; }
}
```

## Service Registration

```csharp
namespace DocumentProcessor.Aspire.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddResourceDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        // Core dependency management services
        services.AddSingleton<IResourceDependencyManager, ResourceDependencyManager>();
        services.AddSingleton<IServiceDiscovery, AspireServiceDiscovery>();
        
        // Health checkers
        services.AddKeyedSingleton<IResourceHealthChecker>("database", (provider, key) =>
            new DatabaseHealthChecker(
                configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
                provider.GetRequiredService<ILogger<DatabaseHealthChecker>>()));

        services.AddKeyedSingleton<IResourceHealthChecker>("cache", (provider, key) =>
            new RedisHealthChecker(
                configuration.GetConnectionString("Cache") ?? string.Empty,
                provider.GetRequiredService<ILogger<RedisHealthChecker>>()));

        // Enhanced HTTP client factory
        services.Decorate<IHttpClientFactory, DependencyAwareHttpClientFactory>();

        return services;
    }

    public static IServiceCollection AddResourceHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck<ResourceDependencyHealthCheck>("resource-dependencies")
            .AddNpgSql(
                configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
                name: "database")
            .AddRedis(
                configuration.GetConnectionString("Cache") ?? string.Empty,
                name: "cache");

        return services;
    }
}

// Health check for resource dependencies
public class ResourceDependencyHealthCheck : IHealthCheck
{
    private readonly IResourceDependencyManager dependencyManager;
    private readonly ILogger<ResourceDependencyHealthCheck> logger;

    public ResourceDependencyHealthCheck(
        IResourceDependencyManager dependencyManager,
        ILogger<ResourceDependencyHealthCheck> logger)
    {dependencyManager = dependencyManager;logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var criticalResources = new[] { "database", "cache" };
            var healthResults = new Dictionary<string, object>();
            var allHealthy = true;

            foreach (var resource in criticalResources)
            {
                var health = await dependencyManager.CheckResourceHealthAsync(resource, cancellationToken);
                healthResults[resource] = new
                {
                    status = health.Status.ToString(),
                    checkedAt = health.CheckedAt,
                    error = health.Error
                };

                if (health.Status != ResourceHealthStatus.Healthy)
                {
                    allHealthy = false;
                }
            }

            return allHealthy
                ? HealthCheckResult.Healthy("All critical resources are healthy", healthResults)
                : HealthCheckResult.Degraded("Some resources are not healthy", null, healthResults);
        }
        catch (Exception ex)
        {logger.LogError(ex, "Error checking resource dependencies health");
            return HealthCheckResult.Unhealthy("Failed to check resource dependencies", ex);
        }
    }
}

// Specific health checkers
public class DatabaseHealthChecker : IResourceHealthChecker
{
    private readonly string connectionString;
    private readonly ILogger<DatabaseHealthChecker> logger;

    public DatabaseHealthChecker(string connectionString, ILogger<DatabaseHealthChecker> logger)
    {connectionString = connectionString;logger = logger;
    }

    public async Task<ResourceHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT version()";
            var version = await command.ExecuteScalarAsync(cancellationToken) as string;

            return new ResourceHealth
            {
                ResourceName = "database",
                Status = ResourceHealthStatus.Healthy,
                CheckedAt = DateTime.UtcNow,
                Details = new Dictionary<string, object>
                {
                    ["version"] = version ?? "Unknown",
                    ["database"] = connection.Database,
                    ["server"] = connection.Host
                }
            };
        }
        catch (Exception ex)
        {logger.LogError(ex, "Database health check failed");
            return new ResourceHealth
            {
                ResourceName = "database",
                Status = ResourceHealthStatus.Unhealthy,
                Error = ex.Message,
                CheckedAt = DateTime.UtcNow
            };
        }
    }
}

public class RedisHealthChecker : IResourceHealthChecker
{
    private readonly string connectionString;
    private readonly ILogger<RedisHealthChecker> logger;

    public RedisHealthChecker(string connectionString, ILogger<RedisHealthChecker> logger)
    {connectionString = connectionString;logger = logger;
    }

    public async Task<ResourceHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var redis = StackExchange.Redis.ConnectionMultiplexer.Connect(connectionString);
            var database = redis.GetDatabase();
            
            await database.PingAsync();

            return new ResourceHealth
            {
                ResourceName = "cache",
                Status = ResourceHealthStatus.Healthy,
                CheckedAt = DateTime.UtcNow,
                Details = new Dictionary<string, object>
                {
                    ["connected"] = redis.IsConnected,
                    ["endpoints"] = redis.GetEndPoints().Select(ep => ep.ToString()).ToArray()
                }
            };
        }
        catch (Exception ex)
        {logger.LogError(ex, "Redis health check failed");
            return new ResourceHealth
            {
                ResourceName = "cache",
                Status = ResourceHealthStatus.Unhealthy,
                Error = ex.Message,
                CheckedAt = DateTime.UtcNow
            };
        }
    }
}
```

**Usage**:

### Resource Dependency Resolution

```csharp
// Resolve database connection
var dependencyManager = serviceProvider.GetRequiredService<IResourceDependencyManager>();

try
{
    var dbConnection = await dependencyManager.GetResourceAsync<IDbConnection>("database");
    await dbConnection.OpenAsync();
    
    // Use the connection...
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to get database connection");
}

// Wait for resource availability
await dependencyManager.WaitForResourceAsync("database", TimeSpan.FromMinutes(2));

// Check resource health
var health = await dependencyManager.CheckResourceHealthAsync("cache");
if (health.Status == ResourceHealthStatus.Healthy)
{
    // Proceed with cache operations
}
```

### Service Discovery

```csharp
// Discover service endpoint
var serviceDiscovery = serviceProvider.GetRequiredService<IServiceDiscovery>();

var endpoint = await serviceDiscovery.DiscoverServiceAsync("document-api");
if (endpoint != null)
{
    var baseUrl = $"{endpoint.Scheme}://{endpoint.Host}:{endpoint.Port}";
    // Create HTTP client with discovered endpoint
}

// Watch for service changes
await foreach (var update in serviceDiscovery.WatchServicesAsync("document-api"))
{
    Console.WriteLine($"Service update: {update.Type} - {update.Endpoints.Count} endpoints");
}
```

### App Host Configuration

```csharp
// In Program.cs (App Host)
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("database");
var redis = builder.AddRedis("cache");

var documentApi = builder.AddProject<Projects.DocumentProcessorApi>("document-api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithEnvironment("Services:document-api:Endpoints:0", "https://localhost:7001");

builder.Build().Run();
```

**Notes**:

- **Automatic Resolution**: Resources resolved automatically through service provider and configuration
- **Health Monitoring**: Comprehensive health checking for all resource dependencies
- **Service Discovery**: Dynamic service endpoint discovery with caching and watching
- **Resilience**: Built-in retry and timeout handling for resource access
- **Integration**: Seamless integration with Aspire's resource management
- **Extensible**: Easy to add new resource types and health checkers

**Related Patterns**:

- [Service Orchestration](service-orchestration.md) - Service coordination with dependencies
- [Configuration Management](configuration-management.md) - Resource configuration patterns
- [Health Monitoring](health-monitoring.md) - Resource health tracking
- [Local Development](local-development.md) - Development resource dependencies
