# .NET Aspire Configuration Management

**Description**: Patterns for managing settings across environments with .NET Aspire, including strongly-typed configuration, secrets management, environment-specific settings, and configuration reloading.

**Language/Technology**: C#, .NET Aspire, .NET 9.0

**Code**:

## Configuration Architecture

```csharp
namespace DocumentProcessor.Aspire.Configuration;

using Microsoft.Extensions.Options;

// Strongly-typed configuration classes
public class DocumentProcessingOptions
{
    public const string SectionName = "DocumentProcessing";
    
    public string DefaultLanguage { get; set; } = "en";
    public int MaxDocumentSize { get; set; } = 10 * 1024 * 1024; // 10MB
    public int ProcessingTimeout { get; set; } = 300; // 5 minutes
    public bool EnableParallelProcessing { get; set; } = true;
    public int MaxConcurrentDocuments { get; set; } = Environment.ProcessorCount * 2;
    public StorageConfiguration Storage { get; set; } = new();
    public MLConfiguration ML { get; set; } = new();
}

public class StorageConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "documents";
    public bool UseLocalStorage { get; set; } = false;
    public string LocalStoragePath { get; set; } = "./storage";
    public RetentionPolicy Retention { get; set; } = new();
}

public class RetentionPolicy
{
    public int DocumentRetentionDays { get; set; } = 30;
    public int ProcessingLogRetentionDays { get; set; } = 7;
    public bool EnableAutoCleanup { get; set; } = true;
}

public class MLConfiguration
{
    public string ModelPath { get; set; } = "./models";
    public bool UseRemoteModels { get; set; } = false;
    public string RemoteModelEndpoint { get; set; } = string.Empty;
    public ModelSettings TextClassification { get; set; } = new();
    public ModelSettings SentimentAnalysis { get; set; } = new();
    public ModelSettings TopicModeling { get; set; } = new();
}

public class ModelSettings
{
    public string ModelName { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public double ConfidenceThreshold { get; set; } = 0.7;
    public bool EnableCaching { get; set; } = true;
    public int CacheExpirationMinutes { get; set; } = 60;
}

public class OrleansConfiguration
{
    public const string SectionName = "Orleans";
    
    public string ClusterId { get; set; } = "document-processing-cluster";
    public string ServiceId { get; set; } = "DocumentProcessorService";
    public ConnectionStrings ConnectionStrings { get; set; } = new();
    public ClusteringOptions Clustering { get; set; } = new();
    public DashboardOptions Dashboard { get; set; } = new();
}

public class ConnectionStrings
{
    public string DefaultConnection { get; set; } = string.Empty;
    public string ClusteringConnection { get; set; } = string.Empty;
    public string CacheConnection { get; set; } = string.Empty;
}

public class ClusteringOptions
{
    public string Provider { get; set; } = "AdoNet";
    public int SiloPort { get; set; } = 11111;
    public int GatewayPort { get; set; } = 30000;
    public bool EnableDistributedTracing { get; set; } = true;
}

public class DashboardOptions
{
    public bool Enabled { get; set; } = true;
    public int Port { get; set; } = 8080;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

## Environment-Specific Configuration

### App Host Configuration

```csharp
namespace DocumentProcessor.Aspire.Host;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // Environment detection
        var environment = builder.Environment.EnvironmentName;
        var isDevelopment = builder.Environment.IsDevelopment();

        // Conditional resource configuration based on environment
        var postgres = isDevelopment
            ? builder.AddPostgres("document-db")
                .WithDataVolume()
                .WithPgAdmin()
            : builder.AddPostgres("document-db", password: builder.AddParameter("postgres-password", secret: true))
                .WithDataVolume();

        var redis = isDevelopment
            ? builder.AddRedis("cache")
                .WithRedisCommander()
            : builder.AddRedis("cache", password: builder.AddParameter("redis-password", secret: true));

        var storage = isDevelopment
            ? builder.AddAzureStorage("storage").RunAsEmulator()
            : builder.AddAzureStorage("storage");

        // Configuration based on environment
        var documentApi = builder.AddProject<Projects.DocumentProcessorApi>("document-api")
            .WithReference(postgres)
            .WithReference(redis)
            .WithReference(storage);

        // Environment-specific configuration
        if (isDevelopment)
        {
            documentApi
                .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                .WithEnvironment("DocumentProcessing__ML__UseRemoteModels", "false")
                .WithEnvironment("DocumentProcessing__Storage__UseLocalStorage", "true")
                .WithEnvironment("Orleans__Dashboard__Enabled", "true");
        }
        else
        {
            documentApi
                .WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
                .WithEnvironment("DocumentProcessing__ML__UseRemoteModels", "true")
                .WithEnvironment("DocumentProcessing__Storage__UseLocalStorage", "false")
                .WithEnvironment("Orleans__Dashboard__Enabled", "false");
        }

        // Add secrets for production
        if (!isDevelopment)
        {
            documentApi
                .WithEnvironment("DocumentProcessing__ML__RemoteModelEndpoint", 
                    builder.AddParameter("ml-endpoint", secret: true))
                .WithEnvironment("DocumentProcessing__Storage__ConnectionString", 
                    builder.AddParameter("storage-connection", secret: true));
        }

        builder.Build().Run();
    }
}
```

### Service Configuration

```csharp
namespace DocumentProcessor.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add configuration sources
        ConfigureConfiguration(builder.Configuration, builder.Environment);

        // Add services with configuration
        ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

        var app = builder.Build();

        // Configure middleware
        ConfigureMiddleware(app, app.Environment);

        app.Run();
    }

    private static void ConfigureConfiguration(ConfigurationManager configuration, IWebHostEnvironment environment)
    {
        // Base configuration files
        configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        configuration.AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
        
        // Environment-specific sources
        if (environment.IsDevelopment())
        {
            configuration.AddUserSecrets<Program>();
        }
        else
        {
            // In production, use Azure Key Vault or similar
            var keyVaultUrl = configuration["KeyVault:Url"];
            if (!string.IsNullOrEmpty(keyVaultUrl))
            {
                configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());
            }
        }

        // Environment variables (highest priority)
        configuration.AddEnvironmentVariables();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Register strongly-typed configuration
        services.Configure<DocumentProcessingOptions>(
            configuration.GetSection(DocumentProcessingOptions.SectionName));
        
        services.Configure<OrleansConfiguration>(
            configuration.GetSection(OrleansConfiguration.SectionName));

        // Validate configuration on startup
        services.AddOptions<DocumentProcessingOptions>()
            .Bind(configuration.GetSection(DocumentProcessingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configuration-dependent service registration
        var docOptions = configuration.GetSection(DocumentProcessingOptions.SectionName).Get<DocumentProcessingOptions>()
            ?? new DocumentProcessingOptions();

        if (docOptions.Storage.UseLocalStorage)
        {
            services.AddSingleton<IStorageService, LocalStorageService>();
        }
        else
        {
            services.AddSingleton<IStorageService, AzureStorageService>();
        }

        if (docOptions.ML.UseRemoteModels)
        {
            services.AddHttpClient<IMLModelService, RemoteMLModelService>();
        }
        else
        {
            services.AddSingleton<IMLModelService, LocalMLModelService>();
        }

        // Add configuration validation service
        services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();
        
        // Add configuration monitoring
        services.AddSingleton<IConfigurationMonitor, ConfigurationMonitor>();
        
        services.AddHostedService<ConfigurationValidationService>();
    }

    private static void ConfigureMiddleware(WebApplication app, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}
```

## Configuration Validation

```csharp
namespace DocumentProcessor.Aspire.Configuration;

public interface IConfigurationValidator
{
    Task<ValidationResult> ValidateAsync();
    Task<ValidationResult> ValidateServiceAsync(string serviceName);
}

public class ConfigurationValidator : IConfigurationValidator
{
    private readonly IOptionsMonitor<DocumentProcessingOptions> _docOptions;
    private readonly IOptionsMonitor<OrleansConfiguration> _orleansOptions;
    private readonly ILogger<ConfigurationValidator> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ConfigurationValidator(
        IOptionsMonitor<DocumentProcessingOptions> docOptions,
        IOptionsMonitor<OrleansConfiguration> orleansOptions,
        ILogger<ConfigurationValidator> logger,
        IServiceProvider serviceProvider)
    {
        _docOptions = docOptions;
        _orleansOptions = orleansOptions;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<ValidationResult> ValidateAsync()
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        // Validate document processing configuration
        var docConfig = _docOptions.CurrentValue;
        ValidateDocumentProcessingConfig(docConfig, errors, warnings);

        // Validate Orleans configuration
        var orleansConfig = _orleansOptions.CurrentValue;
        ValidateOrleansConfig(orleansConfig, errors, warnings);

        // Validate service dependencies
        await ValidateServiceDependenciesAsync(errors, warnings);

        var result = new ValidationResult(
            IsValid: errors.Count == 0,
            Errors: errors,
            Warnings: warnings,
            ValidatedAt: DateTime.UtcNow);

        if (errors.Count > 0)
        {
            _logger.LogError("Configuration validation failed with {ErrorCount} errors", errors.Count);
        }
        else if (warnings.Count > 0)
        {
            _logger.LogWarning("Configuration validation passed with {WarningCount} warnings", warnings.Count);
        }
        else
        {
            _logger.LogInformation("Configuration validation passed successfully");
        }

        return result;
    }

    public async Task<ValidationResult> ValidateServiceAsync(string serviceName)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            
            switch (serviceName.ToLowerInvariant())
            {
                case "storage":
                    var storageService = scope.ServiceProvider.GetService<IStorageService>();
                    if (storageService == null)
                    {
                        errors.Add(new ValidationError("Storage", "Storage service not registered"));
                    }
                    else
                    {
                        await ValidateStorageServiceAsync(storageService, errors, warnings);
                    }
                    break;

                case "ml":
                    var mlService = scope.ServiceProvider.GetService<IMLModelService>();
                    if (mlService == null)
                    {
                        errors.Add(new ValidationError("ML", "ML service not registered"));
                    }
                    else
                    {
                        await ValidateMLServiceAsync(mlService, errors, warnings);
                    }
                    break;

                default:
                    errors.Add(new ValidationError("Service", $"Unknown service: {serviceName}"));
                    break;
            }
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError("Service", $"Failed to validate {serviceName}: {ex.Message}"));
        }

        return new ValidationResult(
            IsValid: errors.Count == 0,
            Errors: errors,
            Warnings: warnings,
            ValidatedAt: DateTime.UtcNow);
    }

    private void ValidateDocumentProcessingConfig(
        DocumentProcessingOptions config, 
        List<ValidationError> errors, 
        List<ValidationWarning> warnings)
    {
        // Validate basic settings
        if (config.MaxDocumentSize <= 0)
        {
            errors.Add(new ValidationError("DocumentProcessing.MaxDocumentSize", "Must be greater than 0"));
        }
        
        if (config.MaxDocumentSize > 100 * 1024 * 1024) // 100MB
        {
            warnings.Add(new ValidationWarning("DocumentProcessing.MaxDocumentSize", "Very large max document size may impact performance"));
        }

        if (config.ProcessingTimeout <= 0)
        {
            errors.Add(new ValidationError("DocumentProcessing.ProcessingTimeout", "Must be greater than 0"));
        }

        if (config.MaxConcurrentDocuments <= 0)
        {
            errors.Add(new ValidationError("DocumentProcessing.MaxConcurrentDocuments", "Must be greater than 0"));
        }

        // Validate storage configuration
        if (config.Storage.UseLocalStorage)
        {
            if (string.IsNullOrEmpty(config.Storage.LocalStoragePath))
            {
                errors.Add(new ValidationError("DocumentProcessing.Storage.LocalStoragePath", "Required when UseLocalStorage is true"));
            }
            else if (!Directory.Exists(Path.GetDirectoryName(config.Storage.LocalStoragePath)))
            {
                warnings.Add(new ValidationWarning("DocumentProcessing.Storage.LocalStoragePath", "Parent directory does not exist"));
            }
        }
        else
        {
            if (string.IsNullOrEmpty(config.Storage.ConnectionString))
            {
                errors.Add(new ValidationError("DocumentProcessing.Storage.ConnectionString", "Required when UseLocalStorage is false"));
            }
        }

        // Validate ML configuration
        if (config.ML.UseRemoteModels)
        {
            if (string.IsNullOrEmpty(config.ML.RemoteModelEndpoint))
            {
                errors.Add(new ValidationError("DocumentProcessing.ML.RemoteModelEndpoint", "Required when UseRemoteModels is true"));
            }
            else if (!Uri.TryCreate(config.ML.RemoteModelEndpoint, UriKind.Absolute, out _))
            {
                errors.Add(new ValidationError("DocumentProcessing.ML.RemoteModelEndpoint", "Must be a valid URL"));
            }
        }
        else
        {
            if (string.IsNullOrEmpty(config.ML.ModelPath))
            {
                errors.Add(new ValidationError("DocumentProcessing.ML.ModelPath", "Required when UseRemoteModels is false"));
            }
            else if (!Directory.Exists(config.ML.ModelPath))
            {
                warnings.Add(new ValidationWarning("DocumentProcessing.ML.ModelPath", "Model directory does not exist"));
            }
        }
    }

    private void ValidateOrleansConfig(
        OrleansConfiguration config, 
        List<ValidationError> errors, 
        List<ValidationWarning> warnings)
    {
        if (string.IsNullOrEmpty(config.ClusterId))
        {
            errors.Add(new ValidationError("Orleans.ClusterId", "Required"));
        }

        if (string.IsNullOrEmpty(config.ServiceId))
        {
            errors.Add(new ValidationError("Orleans.ServiceId", "Required"));
        }

        if (config.Clustering.SiloPort <= 0 || config.Clustering.SiloPort > 65535)
        {
            errors.Add(new ValidationError("Orleans.Clustering.SiloPort", "Must be between 1 and 65535"));
        }

        if (config.Clustering.GatewayPort <= 0 || config.Clustering.GatewayPort > 65535)
        {
            errors.Add(new ValidationError("Orleans.Clustering.GatewayPort", "Must be between 1 and 65535"));
        }

        if (config.Clustering.SiloPort == config.Clustering.GatewayPort)
        {
            errors.Add(new ValidationError("Orleans.Clustering", "SiloPort and GatewayPort cannot be the same"));
        }
    }

    private async Task ValidateServiceDependenciesAsync(
        List<ValidationError> errors, 
        List<ValidationWarning> warnings)
    {
        // This would include actual connectivity tests to databases, caches, etc.
        await Task.CompletedTask;
    }

    private async Task ValidateStorageServiceAsync(
        IStorageService storageService, 
        List<ValidationError> errors, 
        List<ValidationWarning> warnings)
    {
        try
        {
            await storageService.HealthCheckAsync();
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError("Storage", $"Health check failed: {ex.Message}"));
        }
    }

    private async Task ValidateMLServiceAsync(
        IMLModelService mlService, 
        List<ValidationError> errors, 
        List<ValidationWarning> warnings)
    {
        try
        {
            await mlService.HealthCheckAsync();
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError("ML", $"Health check failed: {ex.Message}"));
        }
    }
}

// Data models for validation
public record ValidationResult(
    bool IsValid,
    List<ValidationError> Errors,
    List<ValidationWarning> Warnings,
    DateTime ValidatedAt);

public record ValidationError(string Property, string Message);
public record ValidationWarning(string Property, string Message);

// Background service for continuous validation
public class ConfigurationValidationService : BackgroundService
{
    private readonly IConfigurationValidator _validator;
    private readonly ILogger<ConfigurationValidationService> _logger;
    private readonly TimeSpan _validationInterval = TimeSpan.FromMinutes(5);

    public ConfigurationValidationService(
        IConfigurationValidator validator,
        ILogger<ConfigurationValidationService> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial validation
        await ValidateConfigurationAsync();

        // Periodic validation
        using var timer = new PeriodicTimer(_validationInterval);
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ValidateConfigurationAsync();
        }
    }

    private async Task ValidateConfigurationAsync()
    {
        try
        {
            var result = await _validator.ValidateAsync();
            
            if (!result.IsValid)
            {
                _logger.LogError("Configuration validation failed: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => $"{e.Property}: {e.Message}")));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration validation encountered an error");
        }
    }
}
```

## Configuration Monitoring

```csharp
namespace DocumentProcessor.Aspire.Configuration;

public interface IConfigurationMonitor
{
    Task<ConfigurationSnapshot> GetCurrentSnapshotAsync();
    Task<List<ConfigurationChange>> GetRecentChangesAsync(TimeSpan period);
    event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
}

public class ConfigurationMonitor : IConfigurationMonitor
{
    private readonly IOptionsMonitor<DocumentProcessingOptions> _docOptions;
    private readonly IOptionsMonitor<OrleansConfiguration> _orleansOptions;
    private readonly ILogger<ConfigurationMonitor> _logger;
    private readonly ConcurrentQueue<ConfigurationChange> _recentChanges = new();
    private readonly object _lock = new();

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public ConfigurationMonitor(
        IOptionsMonitor<DocumentProcessingOptions> docOptions,
        IOptionsMonitor<OrleansConfiguration> orleansOptions,
        ILogger<ConfigurationMonitor> logger)
    {
        _docOptions = docOptions;
        _orleansOptions = orleansOptions;
        _logger = logger;

        // Subscribe to configuration changes
        _docOptions.OnChange((options, name) =>
        {
            var change = new ConfigurationChange(
                Section: "DocumentProcessing",
                Property: name ?? "Root",
                OldValue: "N/A", // Could track previous values if needed
                NewValue: JsonSerializer.Serialize(options),
                Timestamp: DateTime.UtcNow,
                Source: "Options Monitor");

            RecordChange(change);
        });

        _orleansOptions.OnChange((options, name) =>
        {
            var change = new ConfigurationChange(
                Section: "Orleans",
                Property: name ?? "Root", 
                OldValue: "N/A",
                NewValue: JsonSerializer.Serialize(options),
                Timestamp: DateTime.UtcNow,
                Source: "Options Monitor");

            RecordChange(change);
        });
    }

    public async Task<ConfigurationSnapshot> GetCurrentSnapshotAsync()
    {
        var docConfig = _docOptions.CurrentValue;
        var orleansConfig = _orleansOptions.CurrentValue;

        var snapshot = new ConfigurationSnapshot(
            DocumentProcessing: docConfig,
            Orleans: orleansConfig,
            Timestamp: DateTime.UtcNow,
            Environment: Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            Version: GetConfigurationVersion());

        return await Task.FromResult(snapshot);
    }

    public async Task<List<ConfigurationChange>> GetRecentChangesAsync(TimeSpan period)
    {
        var cutoff = DateTime.UtcNow - period;
        var changes = _recentChanges
            .Where(c => c.Timestamp >= cutoff)
            .OrderByDescending(c => c.Timestamp)
            .ToList();

        return await Task.FromResult(changes);
    }

    private void RecordChange(ConfigurationChange change)
    {
        lock (_lock)
        {
            _recentChanges.Enqueue(change);
            
            // Keep only recent changes (last 100)
            while (_recentChanges.Count > 100)
            {
                _recentChanges.TryDequeue(out _);
            }
        }

        _logger.LogInformation("Configuration changed: {Section}.{Property}", change.Section, change.Property);
        
        ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(change));
    }

    private string GetConfigurationVersion()
    {
        // Generate a hash of current configuration for versioning
        var docConfig = JsonSerializer.Serialize(_docOptions.CurrentValue);
        var orleansConfig = JsonSerializer.Serialize(_orleansOptions.CurrentValue);
        var combined = docConfig + orleansConfig;
        
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hash)[..8]; // First 8 characters
    }
}

public record ConfigurationSnapshot(
    DocumentProcessingOptions DocumentProcessing,
    OrleansConfiguration Orleans,
    DateTime Timestamp,
    string Environment,
    string Version);

public record ConfigurationChange(
    string Section,
    string Property,
    string OldValue,
    string NewValue,
    DateTime Timestamp,
    string Source);

public class ConfigurationChangedEventArgs : EventArgs
{
    public ConfigurationChange Change { get; }

    public ConfigurationChangedEventArgs(ConfigurationChange change)
    {
        Change = change;
    }
}
```

**Usage**:

### Environment Configuration Files

```json
// appsettings.json (Base)
{
  "DocumentProcessing": {
    "DefaultLanguage": "en",
    "MaxDocumentSize": 10485760,
    "ProcessingTimeout": 300,
    "EnableParallelProcessing": true,
    "MaxConcurrentDocuments": 8,
    "Storage": {
      "ContainerName": "documents",
      "Retention": {
        "DocumentRetentionDays": 30,
        "ProcessingLogRetentionDays": 7,
        "EnableAutoCleanup": true
      }
    },
    "ML": {
      "ModelPath": "./models",
      "TextClassification": {
        "ModelName": "document-classifier",
        "Version": "1.0.0",
        "ConfidenceThreshold": 0.7,
        "EnableCaching": true,
        "CacheExpirationMinutes": 60
      }
    }
  },
  "Orleans": {
    "ClusterId": "document-processing-cluster",
    "ServiceId": "DocumentProcessorService",
    "Clustering": {
      "Provider": "AdoNet",
      "SiloPort": 11111,
      "GatewayPort": 30000,
      "EnableDistributedTracing": true
    },
    "Dashboard": {
      "Enabled": true,
      "Port": 8080
    }
  }
}
```

```json
// appsettings.Development.json
{
  "DocumentProcessing": {
    "Storage": {
      "UseLocalStorage": true,
      "LocalStoragePath": "./storage"
    },
    "ML": {
      "UseRemoteModels": false
    }
  },
  "Orleans": {
    "Dashboard": {
      "Enabled": true
    }
  }
}
```

```json
// appsettings.Production.json
{
  "DocumentProcessing": {
    "MaxConcurrentDocuments": 32,
    "Storage": {
      "UseLocalStorage": false
    },
    "ML": {
      "UseRemoteModels": true
    }
  },
  "Orleans": {
    "Dashboard": {
      "Enabled": false
    }
  }
}
```

### Configuration Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IConfigurationMonitor _monitor;
    private readonly IConfigurationValidator _validator;

    public ConfigurationController(IConfigurationMonitor monitor, IConfigurationValidator validator)
    {
        _monitor = monitor;
        _validator = validator;
    }

    [HttpGet("current")]
    public async Task<ActionResult<ConfigurationSnapshot>> GetCurrentConfiguration()
    {
        var snapshot = await _monitor.GetCurrentSnapshotAsync();
        return Ok(snapshot);
    }

    [HttpGet("validate")]
    public async Task<ActionResult<ValidationResult>> ValidateConfiguration()
    {
        var result = await _validator.ValidateAsync();
        return Ok(result);
    }

    [HttpGet("changes")]
    public async Task<ActionResult<List<ConfigurationChange>>> GetRecentChanges(
        [FromQuery] int hours = 24)
    {
        var changes = await _monitor.GetRecentChangesAsync(TimeSpan.FromHours(hours));
        return Ok(changes);
    }
}
```

**Notes**:

- **Type Safety**: All configuration uses strongly-typed classes with validation
- **Environment-Specific**: Separate configuration files for different environments
- **Secrets Management**: Secure handling of sensitive configuration data
- **Validation**: Comprehensive validation with startup and runtime checks
- **Monitoring**: Real-time configuration change tracking and notifications
- **Reloading**: Automatic configuration reloading without application restart

**Related Patterns**:

- [Service Orchestration](service-orchestration.md) - Using configuration in service coordination
- [Local Development Workflow](local-development.md) - Development-specific configuration
- [Health Monitoring](health-monitoring.md) - Configuration health checks
- [Production Deployment](production-deployment.md) - Production configuration management
