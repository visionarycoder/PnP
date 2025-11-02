using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using CSharp.AzureManagedIdentity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CSharp.AzureManagedIdentity;

/// <summary>
/// Demonstrates Azure Managed Identity patterns for secure, credential-free Azure service authentication.
/// 
/// Managed Identity provides Azure services with an automatically managed identity in Azure AD
/// that can authenticate to any service that supports Azure AD authentication, without storing
/// credentials in code or configuration.
/// 
/// Key Features Demonstrated:
/// - System-assigned and User-assigned Managed Identity
/// - Token acquisition and caching strategies
/// - Integration with Azure services (Key Vault, Storage, SQL)
/// - Local development fallback patterns
/// - Health monitoring and diagnostics
/// - Configuration and service registration
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Azure Managed Identity Pattern Demo ===\n");

        var host = CreateHost();
        
        await DemoBasicManagedIdentity(host.Services);
        await DemoKeyVaultIntegration(host.Services);
        await DemoStorageIntegration(host.Services);
        await DemoSqlIntegration(host.Services);
        await DemoTokenManagement(host.Services);
        await DemoHealthMonitoring(host.Services);
        await DemoConfigurationPatterns(host.Services);

        Console.WriteLine("\n=== Demo Complete ===");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Demonstrates basic managed identity token acquisition
    /// </summary>
    private static async Task DemoBasicManagedIdentity(IServiceProvider services)
    {
        Console.WriteLine("1. Basic Managed Identity");
        Console.WriteLine("------------------------");

        var managedIdentityService = services.GetRequiredService<IManagedIdentityService>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            // Get token for Azure Resource Manager
            var armToken = await managedIdentityService.GetAccessTokenAsync(
                "https://management.azure.com/");
            
            Console.WriteLine("✅ ARM Token acquired successfully");
            Console.WriteLine($"   Token expires: {armToken.ExpiresOn:yyyy-MM-dd HH:mm:ss UTC}");
            Console.WriteLine($"   Token length: {armToken.Token.Length} characters");

            // Get token for Microsoft Graph
            var graphToken = await managedIdentityService.GetAccessTokenAsync(
                "https://graph.microsoft.com/");
            
            Console.WriteLine("✅ Graph Token acquired successfully");
            Console.WriteLine($"   Token expires: {graphToken.ExpiresOn:yyyy-MM-dd HH:mm:ss UTC}");

            // Demonstrate token validation
            var isValid = await managedIdentityService.ValidateTokenAsync(armToken.Token);
            Console.WriteLine($"   Token validation: {(isValid ? "✅ Valid" : "❌ Invalid")}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to acquire managed identity token");
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine("   This is expected when running outside Azure or without managed identity configured");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates Key Vault integration with managed identity
    /// </summary>
    private static async Task DemoKeyVaultIntegration(IServiceProvider services)
    {
        Console.WriteLine("2. Key Vault Integration");
        Console.WriteLine("-----------------------");

        var managedIdentityService = services.GetRequiredService<IManagedIdentityService>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            // Create Key Vault client with managed identity
            var keyVaultClient = await managedIdentityService.CreateKeyVaultClientAsync(
                "https://your-keyvault.vault.azure.net/");

            Console.WriteLine("✅ Key Vault client created with managed identity");

            // Simulate secret operations
            var secretOperations = new[]
            {
                "database-connection-string",
                "api-key", 
                "storage-account-key",
                "service-bus-connection"
            };

            foreach (var secretName in secretOperations)
            {
                try
                {
                    // In a real scenario, this would fetch the actual secret
                    Console.WriteLine($"   📋 Accessing secret: {secretName}");
                    
                    // Simulate secret retrieval
                    await Task.Delay(50);
                    Console.WriteLine($"      ✅ Secret retrieved successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"      ❌ Failed to retrieve {secretName}: {ex.Message}");
                }
            }

            // Demonstrate secret caching
            var cachedSecret = await managedIdentityService.GetCachedSecretAsync("database-connection-string");
            if (cachedSecret != null)
            {
                Console.WriteLine("   🔄 Using cached secret value");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Key Vault integration failed");
            Console.WriteLine($"❌ Key Vault Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates Azure Storage integration with managed identity
    /// </summary>
    private static async Task DemoStorageIntegration(IServiceProvider services)
    {
        Console.WriteLine("3. Storage Integration");
        Console.WriteLine("--------------------");

        var managedIdentityService = services.GetRequiredService<IManagedIdentityService>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            // Create Storage client with managed identity
            var blobClient = await managedIdentityService.CreateBlobServiceClientAsync(
                "https://yourstorageaccount.blob.core.windows.net/");

            Console.WriteLine("✅ Blob Storage client created with managed identity");

            // Simulate blob operations
            var containers = new[] { "documents", "images", "logs", "backups" };

            foreach (var containerName in containers)
            {
                try
                {
                    Console.WriteLine($"   📁 Accessing container: {containerName}");
                    
                    // Simulate container operations
                    await Task.Delay(30);
                    Console.WriteLine($"      ✅ Container access successful");
                    
                    // Simulate blob listing
                    var blobCount = Random.Shared.Next(5, 25);
                    Console.WriteLine($"      📄 Found {blobCount} blobs in container");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"      ❌ Container access failed: {ex.Message}");
                }
            }

            // Demonstrate permission levels
            var permissions = await managedIdentityService.GetStoragePermissionsAsync();
            Console.WriteLine($"   🔐 Storage permissions: {string.Join(", ", permissions)}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Storage integration failed");
            Console.WriteLine($"❌ Storage Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates SQL Database integration with managed identity
    /// </summary>
    private static async Task DemoSqlIntegration(IServiceProvider services)
    {
        Console.WriteLine("4. SQL Database Integration");
        Console.WriteLine("---------------------------");

        var managedIdentityService = services.GetRequiredService<IManagedIdentityService>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            // Create SQL connection with managed identity
            var sqlConnection = await managedIdentityService.CreateSqlConnectionAsync(
                "your-sql-server.database.windows.net", 
                "your-database");

            Console.WriteLine("✅ SQL connection created with managed identity");

            // Simulate database operations
            var operations = new[]
            {
                ("SELECT COUNT(*) FROM Users", "User count query"),
                ("SELECT TOP 10 * FROM Orders", "Recent orders query"),
                ("SELECT * FROM SystemLog WHERE LogLevel = 'Error'", "Error log query"),
                ("EXEC GetDashboardData", "Dashboard stored procedure")
            };

            foreach (var (query, description) in operations)
            {
                try
                {
                    Console.WriteLine($"   🔍 Executing: {description}");
                    
                    // Simulate query execution
                    await Task.Delay(40);
                    var rowCount = Random.Shared.Next(0, 100);
                    Console.WriteLine($"      ✅ Query successful - {rowCount} rows affected");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"      ❌ Query failed: {ex.Message}");
                }
            }

            // Demonstrate connection pooling with managed identity
            var poolStats = await managedIdentityService.GetConnectionPoolStatsAsync();
            Console.WriteLine($"   🔗 Active connections: {poolStats.ActiveConnections}");
            Console.WriteLine($"   🔗 Pool size: {poolStats.PoolSize}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SQL integration failed");
            Console.WriteLine($"❌ SQL Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates token lifecycle management
    /// </summary>
    private static async Task DemoTokenManagement(IServiceProvider services)
    {
        Console.WriteLine("5. Token Lifecycle Management");
        Console.WriteLine("-----------------------------");

        var managedIdentityService = services.GetRequiredService<IManagedIdentityService>();

        // Demonstrate token caching
        Console.WriteLine("🔄 Token Caching Demonstration:");
        
        var startTime = DateTime.UtcNow;
        
        try
        {
            // First token request (cache miss)
            var token1 = await managedIdentityService.GetAccessTokenAsync("https://management.azure.com/");
            var firstRequestTime = DateTime.UtcNow - startTime;
            Console.WriteLine($"   First request (cache miss): {firstRequestTime.TotalMilliseconds:F0}ms");

            // Second token request (cache hit)
            startTime = DateTime.UtcNow;
            var token2 = await managedIdentityService.GetAccessTokenAsync("https://management.azure.com/");
            var secondRequestTime = DateTime.UtcNow - startTime;
            Console.WriteLine($"   Second request (cache hit): {secondRequestTime.TotalMilliseconds:F0}ms");

            // Demonstrate token refresh logic
            var refreshStats = await managedIdentityService.GetTokenRefreshStatsAsync();
            Console.WriteLine($"   📊 Cache statistics:");
            Console.WriteLine($"      Cache hits: {refreshStats.CacheHits}");
            Console.WriteLine($"      Cache misses: {refreshStats.CacheMisses}");
            Console.WriteLine($"      Tokens refreshed: {refreshStats.TokensRefreshed}");

            // Demonstrate proactive token refresh
            Console.WriteLine("   🔄 Proactive token refresh demonstration...");
            await managedIdentityService.RefreshTokensAsync();
            Console.WriteLine("      ✅ Tokens refreshed proactively");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Token management error: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates health monitoring for managed identity
    /// </summary>
    private static async Task DemoHealthMonitoring(IServiceProvider services)
    {
        Console.WriteLine("6. Health Monitoring");
        Console.WriteLine("-------------------");

        var managedIdentityService = services.GetRequiredService<IManagedIdentityService>();

        // Check managed identity health
        var healthCheck = await managedIdentityService.CheckHealthAsync();
        
        Console.WriteLine($"🏥 Managed Identity Health Status: {healthCheck.Status}");
        Console.WriteLine($"   Response time: {healthCheck.ResponseTime.TotalMilliseconds:F0}ms");
        Console.WriteLine($"   Last successful token: {healthCheck.LastSuccessfulToken:yyyy-MM-dd HH:mm:ss UTC}");

        if (healthCheck.Issues.Any())
        {
            Console.WriteLine("   ⚠️ Health issues detected:");
            foreach (var issue in healthCheck.Issues)
            {
                Console.WriteLine($"      • {issue}");
            }
        }

        // Demonstrate endpoint availability
        var endpoints = new[]
        {
            "Azure Resource Manager",
            "Microsoft Graph", 
            "Key Vault",
            "Storage Account",
            "SQL Database"
        };

        Console.WriteLine("\n🔍 Service Endpoint Availability:");
        foreach (var endpoint in endpoints)
        {
            var isAvailable = await managedIdentityService.CheckEndpointAsync(endpoint);
            var status = isAvailable ? "✅ Available" : "❌ Unavailable";
            Console.WriteLine($"   {endpoint}: {status}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates configuration patterns for managed identity
    /// </summary>
    private static async Task DemoConfigurationPatterns(IServiceProvider services)
    {
        Console.WriteLine("7. Configuration Patterns");
        Console.WriteLine("------------------------");

        var options = services.GetRequiredService<IOptions<ManagedIdentityOptions>>().Value;

        Console.WriteLine("📋 Current Configuration:");
        Console.WriteLine($"   Identity Type: {(options.UseSystemAssigned ? "System-Assigned" : "User-Assigned")}");
        
        if (!options.UseSystemAssigned && !string.IsNullOrEmpty(options.UserAssignedClientId))
        {
            Console.WriteLine($"   Client ID: {options.UserAssignedClientId}");
        }

        Console.WriteLine($"   Local Development: {(options.EnableLocalDevelopment ? "Enabled" : "Disabled")}");
        Console.WriteLine($"   Token Cache Duration: {options.TokenCacheDuration}");

        Console.WriteLine("\n🔧 Service Configurations:");
        foreach (var (serviceName, config) in options.Services)
        {
            Console.WriteLine($"   {serviceName}:");
            if (!string.IsNullOrEmpty(config.ResourceId))
            {
                Console.WriteLine($"      Resource ID: {config.ResourceId}");
            }
            if (!string.IsNullOrEmpty(config.Scope))
            {
                Console.WriteLine($"      Scope: {config.Scope}");
            }
            if (config.Scopes?.Any() == true)
            {
                Console.WriteLine($"      Scopes: {string.Join(", ", config.Scopes)}");
            }
        }

        // Demonstrate environment detection
        var environment = await DetectEnvironmentAsync();
        Console.WriteLine($"\n🌍 Environment: {environment}");

        // Demonstrate configuration validation
        var validationResult = await ValidateConfigurationAsync(options);
        Console.WriteLine($"🔍 Configuration Validation: {(validationResult.IsValid ? "✅ Valid" : "❌ Invalid")}");
        
        if (!validationResult.IsValid)
        {
            Console.WriteLine("   Issues found:");
            foreach (var issue in validationResult.Issues)
            {
                Console.WriteLine($"      • {issue}");
            }
        }

        Console.WriteLine();
    }

    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(builder => 
                    builder.AddConsole().SetMinimumLevel(LogLevel.Information));

                // Configure managed identity options
                services.Configure<ManagedIdentityOptions>(options =>
                {
                    options.UseSystemAssigned = true;
                    options.EnableLocalDevelopment = true;
                    options.TokenCacheDuration = TimeSpan.FromMinutes(55);
                    
                    // Add service-specific configurations
                    options.Services["KeyVault"] = new ServiceIdentityConfig
                    {
                        Scope = "https://vault.azure.net/.default"
                    };
                    
                    options.Services["Storage"] = new ServiceIdentityConfig
                    {
                        Scope = "https://storage.azure.com/.default"
                    };
                    
                    options.Services["SQL"] = new ServiceIdentityConfig
                    {
                        Scope = "https://database.windows.net/.default"
                    };
                });

                // Register managed identity services
                services.AddSingleton<IManagedIdentityService, ManagedIdentityService>();
            })
            .Build();
    }

    private static async Task<string> DetectEnvironmentAsync()
    {
        // Simulate environment detection logic
        await Task.Delay(50);
        
        if (Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") != null)
            return "Azure App Service";
        
        if (Environment.GetEnvironmentVariable("AKS_NODE_NAME") != null)
            return "Azure Kubernetes Service";
            
        if (Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") != null)
            return "Azure VM/VMSS";
            
        return "Local Development";
    }

    private static async Task<(bool IsValid, List<string> Issues)> ValidateConfigurationAsync(ManagedIdentityOptions options)
    {
        await Task.Delay(30);
        
        var issues = new List<string>();

        if (!options.UseSystemAssigned && string.IsNullOrEmpty(options.UserAssignedClientId))
        {
            issues.Add("User-assigned identity selected but no client ID provided");
        }

        if (options.TokenCacheDuration < TimeSpan.FromMinutes(1))
        {
            issues.Add("Token cache duration too short (minimum 1 minute recommended)");
        }

        if (options.TokenCacheDuration > TimeSpan.FromHours(1))
        {
            issues.Add("Token cache duration too long (maximum 1 hour recommended)");
        }

        return (issues.Count == 0, issues);
    }
}