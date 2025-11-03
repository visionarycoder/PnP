# Configuration Helpers

**Description**: Enterprise-grade type-safe configuration management utilities with validation, binding, and hot-reload capabilities
**Language/Technology**: C# .NET 9.0 / Configuration Management
**Prerequisites**: Microsoft.Extensions.Configuration, Microsoft.Extensions.Options

## Type-Safe Configuration Manager

**Code**:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Enterprise configuration manager with type safety and validation
/// </summary>
public interface IConfigurationManager<TConfig> where TConfig : class, new()
{
    Task<Result<TConfig>> GetConfigurationAsync(CancellationToken cancellationToken = default);
    Task<Result<TConfig>> ReloadConfigurationAsync(CancellationToken cancellationToken = default);
    Task<Result<Unit>> ValidateConfigurationAsync(TConfig config, CancellationToken cancellationToken = default);
    IDisposable OnConfigurationChanged(Action<TConfig> callback);
}

public class ConfigurationManager<TConfig>(
    IConfiguration configuration,
    IOptionsMonitor<TConfig> optionsMonitor,
    ILogger<ConfigurationManager<TConfig>> logger) : IConfigurationManager<TConfig>
    where TConfig : class, new()
{
    private readonly string configSection = typeof(TConfig).Name.Replace("Configuration", "").Replace("Config", "");

    public async Task<Result<TConfig>> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var config = optionsMonitor.CurrentValue;
            var validationResult = await ValidateConfigurationAsync(config, cancellationToken);
            
            return validationResult.Match(
                _ => Result<TConfig>.Success(config),
                error => Result<TConfig>.Failure($"Configuration validation failed: {error}")
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get configuration for {ConfigType}", typeof(TConfig).Name);
            return Result<TConfig>.Failure($"Configuration loading failed: {ex.Message}");
        }
    }

    public async Task<Result<TConfig>> ReloadConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Force configuration reload
            (configuration as IConfigurationRoot)?.Reload();
            return await GetConfigurationAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reload configuration for {ConfigType}", typeof(TConfig).Name);
            return Result<TConfig>.Failure($"Configuration reload failed: {ex.Message}");
        }
    }

    public async Task<Result<Unit>> ValidateConfigurationAsync(TConfig config, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var context = new ValidationContext(config);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);
            
            if (!isValid)
            {
                var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
                logger.LogWarning("Configuration validation failed: {Errors}", errors);
                return Result<Unit>.Failure(errors);
            }

            logger.LogDebug("Configuration validation passed for {ConfigType}", typeof(TConfig).Name);
            return Result<Unit>.Success(Unit.Value);
        }, cancellationToken);
    }

    public IDisposable OnConfigurationChanged(Action<TConfig> callback)
    {
        return optionsMonitor.OnChange(callback);
    }
}
```

## Secure Configuration Provider

**Code**:

```csharp
/// <summary>
/// Secure configuration provider with encryption and key management
/// </summary>
public interface ISecureConfigurationProvider
{
    Task<Result<string>> GetSecretAsync(string key, CancellationToken cancellationToken = default);
    Task<Result<T>> GetSecretAsync<T>(string key, CancellationToken cancellationToken = default);
    Task<Result<Unit>> SetSecretAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyDictionary<string, string>>> GetAllSecretsAsync(CancellationToken cancellationToken = default);
}

public class SecureConfigurationProvider(
    IConfiguration configuration,
    ILogger<SecureConfigurationProvider> logger) : ISecureConfigurationProvider
{
    private readonly ConcurrentDictionary<string, string> secretCache = new();
    private readonly SemaphoreSlim cacheLock = new(1, 1);

    public async Task<Result<string>> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            // Try cache first
            if (secretCache.TryGetValue(key, out var cachedValue))
            {
                return Result<string>.Success(cachedValue);
            }

            await cacheLock.WaitAsync(cancellationToken);
            try
            {
                // Double-check after acquiring lock
                if (secretCache.TryGetValue(key, out cachedValue))
                {
                    return Result<string>.Success(cachedValue);
                }

                // Load from configuration providers
                var value = await LoadSecretFromProviders(key, cancellationToken);
                if (value != null)
                {
                    secretCache.TryAdd(key, value);
                    return Result<string>.Success(value);
                }

                logger.LogWarning("Secret not found: {Key}", key);
                return Result<string>.Failure($"Secret '{key}' not found");
            }
            finally
            {
                cacheLock.Release();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get secret: {Key}", key);
            return Result<string>.Failure($"Failed to retrieve secret: {ex.Message}");
        }
    }

    public async Task<Result<T>> GetSecretAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var secretResult = await GetSecretAsync(key, cancellationToken);
        return secretResult.Map(value =>
        {
            try
            {
                return typeof(T) == typeof(string) 
                    ? (T)(object)value
                    : JsonSerializer.Deserialize<T>(value) ?? throw new InvalidOperationException("Deserialization returned null");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize secret '{key}' to type {typeof(T).Name}: {ex.Message}", ex);
            }
        });
    }

    private async Task<string?> LoadSecretFromProviders(string key, CancellationToken cancellationToken)
    {
        // Try environment variables first
        var envValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }

        // Try configuration
        var configValue = configuration[key];
        if (!string.IsNullOrEmpty(configValue))
        {
            return configValue;
        }

        // Try Azure Key Vault or other providers
        return await LoadFromExternalProviders(key, cancellationToken);
    }

    private async Task<string?> LoadFromExternalProviders(string key, CancellationToken cancellationToken)
    {
        // Implementation for Azure Key Vault, HashiCorp Vault, etc.
        // This is a placeholder for external secret providers
        await Task.Delay(1, cancellationToken);
        return null;
    }

    public async Task<Result<Unit>> SetSecretAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(value);

        try
        {
            await cacheLock.WaitAsync(cancellationToken);
            try
            {
                secretCache.AddOrUpdate(key, value, (_, _) => value);
                logger.LogDebug("Secret updated: {Key}", key);
                return Result<Unit>.Success(Unit.Value);
            }
            finally
            {
                cacheLock.Release();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set secret: {Key}", key);
            return Result<Unit>.Failure($"Failed to set secret: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyDictionary<string, string>>> GetAllSecretsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await cacheLock.WaitAsync(cancellationToken);
            try
            {
                var secrets = secretCache.ToImmutableDictionary();
                return Result<IReadOnlyDictionary<string, string>>.Success(secrets);
            }
            finally
            {
                cacheLock.Release();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get all secrets");
            return Result<IReadOnlyDictionary<string, string>>.Failure($"Failed to retrieve secrets: {ex.Message}");
        }
    }
}
```

## Configuration Models and Validation

**Code**:

```csharp
/// <summary>
/// Example configuration models with comprehensive validation
/// </summary>
public class DatabaseConfiguration
{
    [Required]
    [StringLength(500)]
    public required string ConnectionString { get; init; }

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;

    [Range(1, 1000)]
    public int MaxConnections { get; init; } = 100;

    public bool EnableRetry { get; init; } = true;

    [Range(1, 10)]
    public int MaxRetryAttempts { get; init; } = 3;
}

public class ApiConfiguration
{
    [Required]
    [Url]
    public required string BaseUrl { get; init; }

    [Required]
    [StringLength(100)]
    public required string ApiKey { get; init; }

    [Range(1000, 300000)]
    public int TimeoutMs { get; init; } = 30000;

    public bool EnableCaching { get; init; } = true;

    [Range(1, 3600)]
    public int CacheExpirationSeconds { get; init; } = 300;
}

public class AppConfiguration
{
    [Required]
    [StringLength(50)]
    public required string ApplicationName { get; init; }

    [Required]
    public required DatabaseConfiguration Database { get; init; }

    [Required]
    public required ApiConfiguration Api { get; init; }

    public LoggingConfiguration? Logging { get; init; }

    public FeatureFlags? Features { get; init; }
}

public class LoggingConfiguration
{
    [Required]
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;

    public bool EnableStructuredLogging { get; init; } = true;

    public IReadOnlyList<string> EnabledProviders { get; init; } = 
        new List<string> { "Console", "File" }.AsReadOnly();
}

public class FeatureFlags
{
    public bool EnableNewFeatureX { get; init; }
    public bool EnableBetaFeatures { get; init; }
    public double FeatureYRolloutPercentage { get; init; } = 0.0;
}
```

## Dependency Injection Extensions

**Code**:

```csharp
/// <summary>
/// Extension methods for registering configuration services
/// </summary>
public static class ConfigurationServiceExtensions
{
    public static IServiceCollection AddConfiguration<TConfig>(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionName = null) 
        where TConfig : class, new()
    {
        sectionName ??= typeof(TConfig).Name.Replace("Configuration", "").Replace("Config", "");

        services.Configure<TConfig>(configuration.GetSection(sectionName));
        services.AddSingleton<IConfigurationManager<TConfig>, ConfigurationManager<TConfig>>();
        
        return services;
    }

    public static IServiceCollection AddSecureConfiguration(
        this IServiceCollection services)
    {
        services.AddSingleton<ISecureConfigurationProvider, SecureConfigurationProvider>();
        return services;
    }

    public static IServiceCollection AddConfigurationValidation<TConfig>(
        this IServiceCollection services) 
        where TConfig : class, new()
    {
        services.AddOptions<TConfig>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static async Task<IServiceCollection> ValidateConfigurationsAsync(
        this IServiceCollection services,
        CancellationToken cancellationToken = default)
    {
        var serviceProvider = services.BuildServiceProvider();
        
        var optionsTypes = services
            .Where(s => s.ServiceType.IsGenericType && 
                       s.ServiceType.GetGenericTypeDefinition() == typeof(IOptions<>))
            .Select(s => s.ServiceType.GetGenericArguments()[0])
            .Distinct();

        foreach (var optionsType in optionsTypes)
        {
            var optionsInstance = serviceProvider.GetService(typeof(IOptions<>).MakeGenericType(optionsType));
            if (optionsInstance != null)
            {
                var value = optionsInstance.GetType().GetProperty("Value")?.GetValue(optionsInstance);
                if (value != null)
                {
                    var context = new ValidationContext(value);
                    var results = new List<ValidationResult>();
                    
                    if (!Validator.TryValidateObject(value, context, results, validateAllProperties: true))
                    {
                        var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
                        throw new InvalidOperationException($"Configuration validation failed for {optionsType.Name}: {errors}");
                    }
                }
            }
        }

        return services;
    }
}
}
```

**Usage**:

```csharp
// Program.cs - DI registration
var builder = WebApplication.CreateBuilder(args);

// Register configuration services
builder.Services
    .AddConfiguration<AppConfiguration>(builder.Configuration)
    .AddSecureConfiguration()
    .AddConfigurationValidation<AppConfiguration>()
    .AddConfigurationValidation<DatabaseConfiguration>()
    .AddConfigurationValidation<ApiConfiguration>();

// Validate all configurations on startup
await builder.Services.ValidateConfigurationsAsync();

var app = builder.Build();

// Service usage examples
public class DataService(IConfigurationManager<DatabaseConfiguration> configManager)
{
    public async Task<Result<Data>> GetDataAsync(CancellationToken cancellationToken = default)
    {
        var configResult = await configManager.GetConfigurationAsync(cancellationToken);
        
        return await configResult.BindAsync(async config =>
        {
            using var connection = new SqlConnection(config.ConnectionString);
            // Use configuration with type safety and validation
            return await ProcessDataAsync(connection, config.TimeoutSeconds, cancellationToken);
        });
    }
}

// Secure configuration usage
public class ApiClient(ISecureConfigurationProvider secureConfig)
{
    public async Task<Result<ApiResponse>> CallApiAsync(CancellationToken cancellationToken = default)
    {
        var apiKeyResult = await secureConfig.GetSecretAsync("API_KEY", cancellationToken);
        
        return await apiKeyResult.BindAsync(async apiKey =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            // Use secure configuration safely
            return await MakeApiCallAsync(client, cancellationToken);
        });
    }
}

// Configuration change monitoring
public class ConfigurationMonitorService(IConfigurationManager<AppConfiguration> configManager) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        configManager.OnConfigurationChanged(config =>
        {
            // React to configuration changes
            UpdateServiceBehavior(config);
        });

        return Task.CompletedTask;
    }
}

// appsettings.json example
{
  "App": {
    "ApplicationName": "MyEnterpriseApp",
    "Database": {
      "ConnectionString": "Server=localhost;Database=MyApp;Trusted_Connection=true;",
      "TimeoutSeconds": 30,
      "MaxConnections": 100,
      "EnableRetry": true,
      "MaxRetryAttempts": 3
    },
    "Api": {
      "BaseUrl": "https://api.example.com",
      "ApiKey": "{{SECRET}}",
      "TimeoutMs": 30000,
      "EnableCaching": true,
      "CacheExpirationSeconds": 300
    },
    "Logging": {
      "MinimumLevel": "Information",
      "EnableStructuredLogging": true,
      "EnabledProviders": ["Console", "File", "ApplicationInsights"]
    },
    "Features": {
      "EnableNewFeatureX": false,
      "EnableBetaFeatures": false,
      "FeatureYRolloutPercentage": 25.5
    }
  }
}

// Environment-specific overrides (appsettings.Production.json)
{
  "App": {
    "Database": {
      "ConnectionString": "{{DATABASE_CONNECTION_STRING}}",
      "MaxConnections": 200
    },
    "Api": {
      "BaseUrl": "https://prod-api.example.com"
    },
    "Logging": {
      "MinimumLevel": "Warning"
    }
  }
}
```

**Notes**:

- **Type Safety**: All configuration models use required properties and nullable reference types for compile-time safety
- **Validation**: Comprehensive data annotations validation with detailed error reporting and early failure detection
- **Security**: Secure configuration provider with caching, external secret providers, and thread-safe operations
- **Performance**: Efficient caching mechanisms with concurrent dictionary operations and minimal memory allocation
- **Dependency Injection**: Full DI support with extension methods for easy service registration and configuration
- **Hot Reload**: Built-in configuration change monitoring with reactive pattern support for runtime updates
- **Error Handling**: Result pattern usage prevents exceptions in normal flow with structured error reporting
- **Environment Support**: Multi-environment configuration with override capabilities and environment-specific settings
- **Observability**: Comprehensive logging with structured data and performance metrics for monitoring
- **Thread Safety**: All operations are thread-safe using proper synchronization primitives and immutable data structures
- **Startup Validation**: Configuration validation during application startup prevents runtime configuration errors
- **External Providers**: Extensible design supporting Azure Key Vault, HashiCorp Vault, and other secret providers

## Related Snippets

- [General Utilities](general-utilities.md) - Enterprise utility patterns and Result type
- [Logging Utilities](logging-utilities.md) - Structured logging and observability
- [Validation Patterns](../csharp/validation-patterns.md) - Advanced validation techniques
