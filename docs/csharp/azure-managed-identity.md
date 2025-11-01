# Azure Managed Identity Integration

**Description**: Complete implementation for Azure Managed Identity authentication in .NET applications, including system-assigned and user-assigned identities, Key Vault access, Azure services authentication, and local development patterns.

**Language/Technology**: C#, .NET 8+, Azure

**Code**:

```csharp
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;

// Managed Identity configuration options
public class ManagedIdentityOptions
{
    public string? UserAssignedClientId { get; set; }
    public string? TenantId { get; set; }
    public bool UseSystemAssigned { get; set; } = true;
    public bool EnableLocalDevelopment { get; set; } = true;
    public TimeSpan TokenCacheDuration { get; set; } = TimeSpan.FromMinutes(55);
    public Dictionary<string, ServiceIdentityConfig> Services { get; set; } = new();
}

public class ServiceIdentityConfig
{
    public string? ResourceId { get; set; }
    public string? Scope { get; set; }
    public string[]? Scopes { get; set; }
    public string? ClientId { get; set; } // For user-assigned identity
}

// Managed Identity service interface
public interface IManagedIdentityService
{
    Task<AccessToken> GetAccessTokenAsync(string resource, CancellationToken cancellationToken = default);
    Task<AccessToken> GetAccessTokenAsync(string[] scopes, CancellationToken cancellationToken = default);
    Task<string> GetSecretAsync(string keyVaultUrl, string secretName, CancellationToken cancellationToken = default);
    Task<SqlConnection> GetSqlConnectionAsync(string connectionString, CancellationToken cancellationToken = default);
    Task<BlobServiceClient> GetBlobServiceClientAsync(string storageAccountUrl, CancellationToken cancellationToken = default);
    TokenCredential GetCredential(string? clientId = null);
}

// Managed Identity service implementation
public class ManagedIdentityService : IManagedIdentityService
{
    private readonly ManagedIdentityOptions _options;
    private readonly ILogger<ManagedIdentityService> _logger;
    private readonly Dictionary<string, TokenCredential> _credentialCache;
    private readonly SemaphoreSlim _credentialCacheLock;

    public ManagedIdentityService(
        IOptions<ManagedIdentityOptions> options,
        ILogger<ManagedIdentityService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _credentialCache = new Dictionary<string, TokenCredential>();
        _credentialCacheLock = new SemaphoreSlim(1, 1);
    }

    public async Task<AccessToken> GetAccessTokenAsync(string resource, CancellationToken cancellationToken = default)
    {
        var credential = GetCredential();
        var tokenRequestContext = new TokenRequestContext(new[] { $"{resource}/.default" });
        
        try
        {
            var token = await credential.GetTokenAsync(tokenRequestContext, cancellationToken);
            _logger.LogDebug("Successfully obtained access token for resource: {Resource}", resource);
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain access token for resource: {Resource}", resource);
            throw;
        }
    }

    public async Task<AccessToken> GetAccessTokenAsync(string[] scopes, CancellationToken cancellationToken = default)
    {
        var credential = GetCredential();
        var tokenRequestContext = new TokenRequestContext(scopes);
        
        try
        {
            var token = await credential.GetTokenAsync(tokenRequestContext, cancellationToken);
            _logger.LogDebug("Successfully obtained access token for scopes: {Scopes}", string.Join(", ", scopes));
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain access token for scopes: {Scopes}", string.Join(", ", scopes));
            throw;
        }
    }

    public async Task<string> GetSecretAsync(string keyVaultUrl, string secretName, CancellationToken cancellationToken = default)
    {
        var credential = GetCredential();
        var client = new SecretClient(new Uri(keyVaultUrl), credential);

        try
        {
            var response = await client.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            _logger.LogDebug("Successfully retrieved secret: {SecretName} from Key Vault: {KeyVaultUrl}", secretName, keyVaultUrl);
            return response.Value.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret: {SecretName} from Key Vault: {KeyVaultUrl}", secretName, keyVaultUrl);
            throw;
        }
    }

    public async Task<SqlConnection> GetSqlConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var credential = GetCredential();
        var connection = new SqlConnection(connectionString);

        try
        {
            // Get access token for SQL Database
            var token = await GetAccessTokenAsync("https://database.windows.net/", cancellationToken);
            connection.AccessToken = token.Token;

            await connection.OpenAsync(cancellationToken);
            _logger.LogDebug("Successfully established SQL connection using managed identity");
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish SQL connection using managed identity");
            connection.Dispose();
            throw;
        }
    }

    public async Task<BlobServiceClient> GetBlobServiceClientAsync(string storageAccountUrl, CancellationToken cancellationToken = default)
    {
        var credential = GetCredential();
        
        try
        {
            var client = new BlobServiceClient(new Uri(storageAccountUrl), credential);
            
            // Test the connection by getting account info
            await client.GetAccountInfoAsync(cancellationToken);
            
            _logger.LogDebug("Successfully created Blob Service client for: {StorageAccountUrl}", storageAccountUrl);
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Blob Service client for: {StorageAccountUrl}", storageAccountUrl);
            throw;
        }
    }

    public TokenCredential GetCredential(string? clientId = null)
    {
        var cacheKey = clientId ?? "system";
        
        _credentialCacheLock.Wait();
        try
        {
            if (_credentialCache.TryGetValue(cacheKey, out var cachedCredential))
            {
                return cachedCredential;
            }

            TokenCredential credential = CreateCredential(clientId);
            _credentialCache[cacheKey] = credential;
            return credential;
        }
        finally
        {
            _credentialCacheLock.Release();
        }
    }

    private TokenCredential CreateCredential(string? clientId = null)
    {
        var options = new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = false,
            ExcludeWorkloadIdentityCredential = false,
            ExcludeManagedIdentityCredential = false,
            ExcludeSharedTokenCacheCredential = !_options.EnableLocalDevelopment,
            ExcludeVisualStudioCredential = !_options.EnableLocalDevelopment,
            ExcludeVisualStudioCodeCredential = !_options.EnableLocalDevelopment,
            ExcludeAzureCliCredential = !_options.EnableLocalDevelopment,
            ExcludeAzurePowerShellCredential = !_options.EnableLocalDevelopment,
            ExcludeInteractiveBrowserCredential = false
        };

        if (!string.IsNullOrEmpty(_options.TenantId))
        {
            options.TenantId = _options.TenantId;
        }

        // Use specific managed identity if clientId is provided or configured
        var effectiveClientId = clientId ?? _options.UserAssignedClientId;
        if (!string.IsNullOrEmpty(effectiveClientId))
        {
            options.ManagedIdentityClientId = effectiveClientId;
            _logger.LogDebug("Using user-assigned managed identity: {ClientId}", effectiveClientId);
        }
        else
        {
            _logger.LogDebug("Using system-assigned managed identity");
        }

        return new DefaultAzureCredential(options);
    }
}

// Azure service clients factory
public interface IAzureServiceClientFactory
{
    Task<T> CreateClientAsync<T>(string serviceUrl, string? clientId = null) where T : class;
    Task<SecretClient> CreateKeyVaultClientAsync(string keyVaultUrl, string? clientId = null);
    Task<BlobServiceClient> CreateBlobServiceClientAsync(string storageAccountUrl, string? clientId = null);
    Task<SqlConnection> CreateSqlConnectionAsync(string serverName, string databaseName, string? clientId = null);
}

public class AzureServiceClientFactory : IAzureServiceClientFactory
{
    private readonly IManagedIdentityService _managedIdentityService;
    private readonly ILogger<AzureServiceClientFactory> _logger;

    public AzureServiceClientFactory(
        IManagedIdentityService managedIdentityService,
        ILogger<AzureServiceClientFactory> logger)
    {
        _managedIdentityService = managedIdentityService;
        _logger = logger;
    }

    public async Task<T> CreateClientAsync<T>(string serviceUrl, string? clientId = null) where T : class
    {
        var credential = _managedIdentityService.GetCredential(clientId);
        
        try
        {
            var client = (T?)Activator.CreateInstance(typeof(T), new Uri(serviceUrl), credential);
            if (client == null)
            {
                throw new InvalidOperationException($"Failed to create instance of {typeof(T).Name}");
            }

            _logger.LogDebug("Successfully created {ClientType} for: {ServiceUrl}", typeof(T).Name, serviceUrl);
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create {ClientType} for: {ServiceUrl}", typeof(T).Name, serviceUrl);
            throw;
        }
    }

    public async Task<SecretClient> CreateKeyVaultClientAsync(string keyVaultUrl, string? clientId = null)
    {
        var credential = _managedIdentityService.GetCredential(clientId);
        var client = new SecretClient(new Uri(keyVaultUrl), credential);
        
        _logger.LogDebug("Created Key Vault client for: {KeyVaultUrl}", keyVaultUrl);
        return await Task.FromResult(client);
    }

    public async Task<BlobServiceClient> CreateBlobServiceClientAsync(string storageAccountUrl, string? clientId = null)
    {
        return await _managedIdentityService.GetBlobServiceClientAsync(storageAccountUrl);
    }

    public async Task<SqlConnection> CreateSqlConnectionAsync(string serverName, string databaseName, string? clientId = null)
    {
        var connectionString = $"Server={serverName}; Database={databaseName}; Authentication=Active Directory Default;";
        return await _managedIdentityService.GetSqlConnectionAsync(connectionString);
    }
}

// Configuration service using Managed Identity
public interface IManagedIdentityConfigurationService
{
    Task<string> GetConfigurationValueAsync(string key, CancellationToken cancellationToken = default);
    Task<T> GetConfigurationValueAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task RefreshConfigurationAsync(CancellationToken cancellationToken = default);
}

public class ManagedIdentityConfigurationService : IManagedIdentityConfigurationService
{
    private readonly IManagedIdentityService _managedIdentityService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ManagedIdentityConfigurationService> _logger;
    private readonly Dictionary<string, object> _configCache;
    private readonly SemaphoreSlim _cacheLock;

    public ManagedIdentityConfigurationService(
        IManagedIdentityService managedIdentityService,
        IConfiguration configuration,
        ILogger<ManagedIdentityConfigurationService> logger)
    {
        _managedIdentityService = managedIdentityService;
        _configuration = configuration;
        _logger = logger;
        _configCache = new Dictionary<string, object>();
        _cacheLock = new SemaphoreSlim(1, 1);
    }

    public async Task<string> GetConfigurationValueAsync(string key, CancellationToken cancellationToken = default)
    {
        // Check local configuration first
        var localValue = _configuration[key];
        if (!string.IsNullOrEmpty(localValue) && !IsKeyVaultReference(localValue))
        {
            return localValue;
        }

        // Check cache
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_configCache.TryGetValue(key, out var cachedValue) && cachedValue is string stringValue)
            {
                return stringValue;
            }
        }
        finally
        {
            _cacheLock.Release();
        }

        // Resolve Key Vault reference
        if (IsKeyVaultReference(localValue))
        {
            var (keyVaultUrl, secretName) = ParseKeyVaultReference(localValue!);
            var secretValue = await _managedIdentityService.GetSecretAsync(keyVaultUrl, secretName, cancellationToken);
            
            // Cache the result
            await _cacheLock.WaitAsync(cancellationToken);
            try
            {
                _configCache[key] = secretValue;
            }
            finally
            {
                _cacheLock.Release();
            }

            return secretValue;
        }

        throw new KeyNotFoundException($"Configuration key '{key}' not found");
    }

    public async Task<T> GetConfigurationValueAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var value = await GetConfigurationValueAsync(key, cancellationToken);
        
        if (typeof(T) == typeof(string))
        {
            return (T)(object)value;
        }

        try
        {
            var result = JsonSerializer.Deserialize<T>(value);
            return result ?? throw new InvalidOperationException($"Deserialization returned null for key '{key}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize configuration value for key: {Key}", key);
            throw;
        }
    }

    public async Task RefreshConfigurationAsync(CancellationToken cancellationToken = default)
    {
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            _configCache.Clear();
            _logger.LogInformation("Configuration cache cleared");
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private bool IsKeyVaultReference(string? value)
    {
        return !string.IsNullOrEmpty(value) && value.StartsWith("@Microsoft.KeyVault(", StringComparison.OrdinalIgnoreCase);
    }

    private (string keyVaultUrl, string secretName) ParseKeyVaultReference(string reference)
    {
        // Parse Key Vault reference format: @Microsoft.KeyVault(SecretUri=https://vault.vault.azure.net/secrets/secret-name)
        var match = Regex.Match(reference, @"SecretUri=([^)]+)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            throw new ArgumentException($"Invalid Key Vault reference format: {reference}");
        }

        var secretUri = new Uri(match.Groups[1].Value);
        var keyVaultUrl = $"{secretUri.Scheme}://{secretUri.Host}";
        var secretName = secretUri.Segments.Last();

        return (keyVaultUrl, secretName);
    }
}

// Managed Identity middleware for health checks
public class ManagedIdentityHealthCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IManagedIdentityService _managedIdentityService;
    private readonly ILogger<ManagedIdentityHealthCheckMiddleware> _logger;

    public ManagedIdentityHealthCheckMiddleware(
        RequestDelegate next,
        IManagedIdentityService managedIdentityService,
        ILogger<ManagedIdentityHealthCheckMiddleware> logger)
    {
        _next = next;
        _managedIdentityService = managedIdentityService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Equals("/health/managed-identity", StringComparison.OrdinalIgnoreCase))
        {
            await HandleHealthCheckAsync(context);
            return;
        }

        await _next(context);
    }

    private async Task HandleHealthCheckAsync(HttpContext context)
    {
        try
        {
            // Test managed identity by getting a token for Azure Resource Manager
            var token = await _managedIdentityService.GetAccessTokenAsync("https://management.azure.com/");
            
            var response = new
            {
                Status = "Healthy",
                TokenExpiry = token.ExpiresOn,
                Message = "Managed Identity is working correctly"
            };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Managed Identity health check failed");
            
            context.Response.StatusCode = 503;
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                Status = "Unhealthy",
                Error = ex.Message,
                Message = "Managed Identity is not working correctly"
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}

// Extension methods for dependency injection
public static class ManagedIdentityExtensions
{
    public static IServiceCollection AddManagedIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ManagedIdentityOptions>(configuration.GetSection("ManagedIdentity"));
        
        services.AddSingleton<IManagedIdentityService, ManagedIdentityService>();
        services.AddSingleton<IAzureServiceClientFactory, AzureServiceClientFactory>();
        services.AddSingleton<IManagedIdentityConfigurationService, ManagedIdentityConfigurationService>();

        // Add Azure clients with managed identity
        services.AddAzureClients(builder =>
        {
            builder.UseCredential(serviceProvider =>
            {
                var managedIdentityService = serviceProvider.GetRequiredService<IManagedIdentityService>();
                return managedIdentityService.GetCredential();
            });
        });

        return services;
    }

    public static IApplicationBuilder UseManagedIdentityHealthCheck(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ManagedIdentityHealthCheckMiddleware>();
    }

    public static IServiceCollection AddManagedIdentityHttpClient(
        this IServiceCollection services,
        string name,
        string baseAddress,
        string resource)
    {
        services.AddHttpClient(name, client =>
        {
            client.BaseAddress = new Uri(baseAddress);
        }).AddHttpMessageHandler<ManagedIdentityTokenHandler>();

        services.Configure<ManagedIdentityTokenOptions>(name, options =>
        {
            options.Resource = resource;
        });

        return services;
    }
}

// HTTP message handler for automatic token injection
public class ManagedIdentityTokenHandler : DelegatingHandler
{
    private readonly IManagedIdentityService _managedIdentityService;
    private readonly IOptionsMonitor<ManagedIdentityTokenOptions> _options;
    private readonly string _clientName;

    public ManagedIdentityTokenHandler(
        IManagedIdentityService managedIdentityService,
        IOptionsMonitor<ManagedIdentityTokenOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _managedIdentityService = managedIdentityService;
        _options = options;
        _clientName = string.Empty; // Will be set by the factory
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var options = _options.Get(_clientName);
        
        if (!string.IsNullOrEmpty(options.Resource))
        {
            var token = await _managedIdentityService.GetAccessTokenAsync(options.Resource, cancellationToken);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

public class ManagedIdentityTokenOptions
{
    public string? Resource { get; set; }
    public string[]? Scopes { get; set; }
}
```

**Usage**:

```csharp
// Program.cs - Managed Identity Configuration
var builder = WebApplication.CreateBuilder(args);

// Add Managed Identity services
builder.Services.AddManagedIdentity(builder.Configuration);

// Add specific Azure service clients
builder.Services.AddSingleton<SecretClient>(serviceProvider =>
{
    var managedIdentity = serviceProvider.GetRequiredService<IManagedIdentityService>();
    var keyVaultUrl = builder.Configuration["KeyVault:Url"]!;
    return new SecretClient(new Uri(keyVaultUrl), managedIdentity.GetCredential());
});

// Add HTTP client with managed identity
builder.Services.AddManagedIdentityHttpClient(
    "AzureAPI",
    "https://management.azure.com/",
    "https://management.azure.com/"
);

var app = builder.Build();

// Add managed identity health check
app.UseManagedIdentityHealthCheck();

// Configuration (appsettings.json)
{
  "ManagedIdentity": {
    "UserAssignedClientId": null,
    "TenantId": "your-tenant-id",
    "UseSystemAssigned": true,
    "EnableLocalDevelopment": true,
    "TokenCacheDuration": "00:55:00",
    "Services": {
      "KeyVault": {
        "ResourceId": "https://vault.vault.azure.net/",
        "Scope": "https://vault.vault.azure.net/.default"
      },
      "Storage": {
        "ResourceId": "https://storage.azure.com/",
        "Scope": "https://storage.azure.com/.default"
      },
      "SqlDatabase": {
        "ResourceId": "https://database.windows.net/",
        "Scope": "https://database.windows.net/.default"
      }
    }
  },
  "ConnectionStrings": {
    "SqlDatabase": "@Microsoft.KeyVault(SecretUri=https://vault.vault.azure.net/secrets/sql-connection-string)",
    "StorageAccount": "https://mystorageaccount.blob.core.windows.net/"
  },
  "KeyVault": {
    "Url": "https://vault.vault.azure.net/"
  }
}

// Controller usage
[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    private readonly IManagedIdentityService _managedIdentityService;
    private readonly IManagedIdentityConfigurationService _configurationService;
    private readonly IAzureServiceClientFactory _clientFactory;

    public SecureController(
        IManagedIdentityService managedIdentityService,
        IManagedIdentityConfigurationService configurationService,
        IAzureServiceClientFactory clientFactory)
    {
        _managedIdentityService = managedIdentityService;
        _configurationService = configurationService;
        _clientFactory = clientFactory;
    }

    [HttpGet("secret/{secretName}")]
    public async Task<IActionResult> GetSecret(string secretName)
    {
        var keyVaultUrl = await _configurationService.GetConfigurationValueAsync("KeyVault:Url");
        var secret = await _managedIdentityService.GetSecretAsync(keyVaultUrl, secretName);
        
        return Ok(new { SecretName = secretName, HasValue = !string.IsNullOrEmpty(secret) });
    }

    [HttpGet("storage/containers")]
    public async Task<IActionResult> ListContainers()
    {
        var storageUrl = await _configurationService.GetConfigurationValueAsync("ConnectionStrings:StorageAccount");
        var blobClient = await _clientFactory.CreateBlobServiceClientAsync(storageUrl);
        
        var containers = new List<string>();
        await foreach (var container in blobClient.GetBlobContainersAsync())
        {
            containers.Add(container.Name);
        }

        return Ok(containers);
    }

    [HttpGet("sql/test")]
    public async Task<IActionResult> TestSqlConnection()
    {
        using var connection = await _clientFactory.CreateSqlConnectionAsync(
            "myserver.database.windows.net",
            "mydatabase"
        );

        var command = new SqlCommand("SELECT @@VERSION", connection);
        var version = await command.ExecuteScalarAsync();

        return Ok(new { DatabaseVersion = version?.ToString() });
    }
}

// Background service using Managed Identity
public class ManagedIdentityBackgroundService : BackgroundService
{
    private readonly IManagedIdentityService _managedIdentityService;
    private readonly ILogger<ManagedIdentityBackgroundService> _logger;

    public ManagedIdentityBackgroundService(
        IManagedIdentityService managedIdentityService,
        ILogger<ManagedIdentityBackgroundService> logger)
    {
        _managedIdentityService = managedIdentityService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Perform periodic task using managed identity
                var token = await _managedIdentityService.GetAccessTokenAsync(
                    "https://management.azure.com/", 
                    stoppingToken
                );

                _logger.LogInformation("Token obtained successfully. Expires: {Expiry}", token.ExpiresOn);

                // Wait for next iteration
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in managed identity background service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}

// Local development setup
public class LocalDevelopmentManagedIdentitySetup
{
    public static async Task SetupAsync()
    {
        // For local development, you can use:
        // 1. Azure CLI: az login
        // 2. Visual Studio: Sign in with Azure account
        // 3. Environment variables for service principal
        
        Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", "your-client-id");
        Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", "your-client-secret");
        Environment.SetEnvironmentVariable("AZURE_TENANT_ID", "your-tenant-id");

        // Test the credential
        var credential = new DefaultAzureCredential();
        var tokenRequestContext = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
        
        try
        {
            var token = await credential.GetTokenAsync(tokenRequestContext);
            Console.WriteLine($"Local development authentication successful. Token expires: {token.ExpiresOn}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Local development authentication failed: {ex.Message}");
        }
    }
}
```

**Prerequisites**:

- .NET 8 or later
- Azure.Identity package
- Azure.Security.KeyVault.Secrets package
- Azure.Storage.Blobs package
- Microsoft.Data.SqlClient package
- Microsoft.Extensions.Azure package
- Managed Identity enabled on Azure resource (App Service, VM, Container Instance, etc.)

**Notes**:

- **System vs User-Assigned**: System-assigned identities are tied to the resource lifecycle, while user-assigned identities can be shared across resources
- **Local Development**: Use Azure CLI (`az login`) or Visual Studio authentication for local development
- **Token Caching**: The Azure Identity library automatically caches tokens and handles refresh
- **Fallback Chain**: DefaultAzureCredential tries multiple authentication methods in order
- **Security**: Never store credentials in code or configuration when using Managed Identity
- **Monitoring**: Implement health checks to verify Managed Identity functionality
- **Scopes**: Use specific scopes instead of broad permissions where possible

**Related Snippets**:

- [JWT Authentication](jwt-authentication.md)
- [OAuth Integration](oauth-integration.md)
- [Web Security](web-security.md)

**References**:

- [Azure Managed Identity Documentation](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)
- [Azure Identity Library](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme)
- [DefaultAzureCredential](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)

---

*Created: 2025-11-01*  
*Last Updated: 2025-11-01*  
*Tags: #azure #managed-identity #authentication #security #azure-identity*
