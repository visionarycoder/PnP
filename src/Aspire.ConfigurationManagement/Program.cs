namespace Aspire.ConfigurationManagement;

/// <summary>
/// Demonstrates enterprise configuration management patterns with .NET Aspire
/// Including hierarchical configuration, Azure Key Vault integration, feature flags,
/// configuration validation, and audit trails for compliance
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.WriteLine("Enterprise Configuration Management with .NET Aspire");
        Console.WriteLine("====================================================");

        StronglyTypedConfigurationExample();
        EnvironmentSpecificConfigurationExample();
        HierarchicalConfigurationExample();
        ConfigurationValidationExample();
        SecretManagementExample();
        FeatureFlagExample();
        ConfigurationAuditExample();
        ConfigurationAsCodeExample();
    }

    /// <summary>
    /// Demonstrates strongly-typed configuration classes
    /// </summary>
    private static void StronglyTypedConfigurationExample()
    {
        Console.WriteLine("1. Strongly-Typed Configuration:");
        Console.WriteLine("   // Configuration classes");
        Console.WriteLine("   public class DocumentProcessingOptions");
        Console.WriteLine("   {");
        Console.WriteLine("       public const string SectionName = \"DocumentProcessing\";");
        Console.WriteLine("       public string DefaultLanguage { get; set; } = \"en\";");
        Console.WriteLine("       public int MaxDocumentSize { get; set; } = 10 * 1024 * 1024;");
        Console.WriteLine("       public int ProcessingTimeout { get; set; } = 300;");
        Console.WriteLine("       public bool EnableParallelProcessing { get; set; } = true;");
        Console.WriteLine("       public StorageConfiguration Storage { get; set; } = new();");
        Console.WriteLine("       public MLConfiguration ML { get; set; } = new();");
        Console.WriteLine("   }");
        Console.WriteLine("");
        Console.WriteLine("   // Service registration");
        Console.WriteLine("   builder.Services.Configure<DocumentProcessingOptions>(");
        Console.WriteLine("       builder.Configuration.GetSection(DocumentProcessingOptions.SectionName));");

        // C# implementation
        var configOptions = new DocumentProcessingOptions
        {
            DefaultLanguage = "en",
            MaxDocumentSize = 10 * 1024 * 1024,
            ProcessingTimeout = 300,
            EnableParallelProcessing = true,
            Storage = new StorageConfiguration
            {
                ContainerName = "documents",
                UseLocalStorage = false,
                LocalStoragePath = "./storage"
            },
            ML = new MLConfiguration
            {
                ModelPath = "./models",
                UseRemoteModels = false,
                TextClassification = new ModelSettings 
                { 
                    ModelName = "text-classifier-v1", 
                    ConfidenceThreshold = 0.7 
                }
            }
        };

        Console.WriteLine($"   Configured: Language={configOptions.DefaultLanguage}, " +
                         $"MaxSize={configOptions.MaxDocumentSize}, " +
                         $"Parallel={configOptions.EnableParallelProcessing}");
        Console.WriteLine("");
    }

    /// <summary>
    /// Demonstrates environment-specific configuration
    /// </summary>
    private static void EnvironmentSpecificConfigurationExample()
    {
        Console.WriteLine("2. Environment-Specific Configuration:");
        Console.WriteLine("   // App Host Configuration");
        Console.WriteLine("   var environment = builder.Environment.EnvironmentName;");
        Console.WriteLine("   var isDevelopment = builder.Environment.IsDevelopment();");
        Console.WriteLine("");
        Console.WriteLine("   // Conditional resource configuration");
        Console.WriteLine("   var postgres = isDevelopment");
        Console.WriteLine("       ? builder.AddPostgres(\"document-db\").WithDataVolume().WithPgAdmin()");
        Console.WriteLine("       : builder.AddPostgres(\"document-db\", password: secretParam);");
        Console.WriteLine("");
        Console.WriteLine("   var redis = isDevelopment");
        Console.WriteLine("       ? builder.AddRedis(\"cache\").WithRedisCommander()");
        Console.WriteLine("       : builder.AddRedis(\"cache\", password: secretParam);");

        // Simulate environment-based configuration
        var currentEnvironment = "Development";
        var isDevelopment = currentEnvironment == "Development";
        
        var databaseConfig = isDevelopment 
            ? "postgres://localhost:5432/document-db-dev"
            : "postgres://prod-server:5432/document-db";
            
        var cacheConfig = isDevelopment
            ? "redis://localhost:6379"
            : "redis://prod-cache:6379";

        Console.WriteLine($"   Environment: {currentEnvironment}");
        Console.WriteLine($"   Database: {databaseConfig}");
        Console.WriteLine($"   Cache: {cacheConfig}");
        Console.WriteLine("");
    }

    /// <summary>
    /// Demonstrates hierarchical configuration with overrides
    /// </summary>
    private static void HierarchicalConfigurationExample()
    {
        Console.WriteLine("3. Hierarchical Configuration:");
        Console.WriteLine("   // Configuration hierarchy:");
        Console.WriteLine("   1. Default values (in code)");
        Console.WriteLine("   2. appsettings.json");
        Console.WriteLine("   3. appsettings.{Environment}.json");
        Console.WriteLine("   4. User secrets (Development)");
        Console.WriteLine("   5. Environment variables");
        Console.WriteLine("   6. Command line arguments");
        Console.WriteLine("");
        Console.WriteLine("   // Configuration builder");
        Console.WriteLine("   builder.Configuration");
        Console.WriteLine("       .AddJsonFile(\"appsettings.json\", optional: false)");
        Console.WriteLine("       .AddJsonFile($\"appsettings.{env}.json\", optional: true)");
        Console.WriteLine("       .AddUserSecrets<Program>()");
        Console.WriteLine("       .AddEnvironmentVariables()");
        Console.WriteLine("       .AddCommandLine(args);");

        // Simulate hierarchical configuration
        var config = new Dictionary<string, object>
        {
            ["Default_MaxDocuments"] = 100,
            ["AppSettings_MaxDocuments"] = 150,
            ["Environment_MaxDocuments"] = 200,
            ["UserSecrets_ApiKey"] = "dev-secret-key",
            ["EnvVar_ApiKey"] = "prod-api-key"
        };

        var finalMaxDocuments = config["Environment_MaxDocuments"];
        var finalApiKey = config["EnvVar_ApiKey"]; // Environment variables override user secrets

        Console.WriteLine($"   Final MaxDocuments: {finalMaxDocuments} (from environment)");
        Console.WriteLine($"   Final ApiKey: {finalApiKey} (from environment variables)");
        Console.WriteLine("");
    }

    /// <summary>
    /// Demonstrates configuration validation
    /// </summary>
    private static void ConfigurationValidationExample()
    {
        Console.WriteLine("4. Configuration Validation:");
        Console.WriteLine("   // Data annotation validation");
        Console.WriteLine("   public class DocumentProcessingOptions");
        Console.WriteLine("   {");
        Console.WriteLine("       [Required]");
        Console.WriteLine("       [StringLength(10)]");
        Console.WriteLine("       public string DefaultLanguage { get; set; } = string.Empty;");
        Console.WriteLine("");
        Console.WriteLine("       [Range(1024, 100 * 1024 * 1024)]");
        Console.WriteLine("       public int MaxDocumentSize { get; set; }");
        Console.WriteLine("");
        Console.WriteLine("       [Range(1, 3600)]");
        Console.WriteLine("       public int ProcessingTimeout { get; set; }");
        Console.WriteLine("   }");
        Console.WriteLine("");
        Console.WriteLine("   // Custom validation");
        Console.WriteLine("   services.AddSingleton<IValidateOptions<DocumentProcessingOptions>,");
        Console.WriteLine("                        DocumentProcessingOptionsValidator>();");

        // Simulate configuration validation
        var validationResults = new List<(string Property, bool IsValid, string Error)>
        {
            ("DefaultLanguage", true, ""),
            ("MaxDocumentSize", false, "Value 200MB exceeds maximum of 100MB"),
            ("ProcessingTimeout", true, ""),
            ("StorageConnectionString", false, "Required field is empty")
        };

        Console.WriteLine("   Validation Results:");
        foreach (var (property, isValid, error) in validationResults)
        {
            var status = isValid ? "✓ Valid" : "✗ Invalid";
            var message = isValid ? "" : $" - {error}";
            Console.WriteLine($"     {property}: {status}{message}");
        }
        Console.WriteLine("");
    }

    /// <summary>
    /// Demonstrates Azure Key Vault integration for secrets
    /// </summary>
    private static void SecretManagementExample()
    {
        Console.WriteLine("5. Secret Management with Azure Key Vault:");
        Console.WriteLine("   // Key Vault configuration");
        Console.WriteLine("   builder.Configuration.AddAzureKeyVault(");
        Console.WriteLine("       keyVaultUrl: \"https://myvault.vault.azure.net/\",");
        Console.WriteLine("       credential: new DefaultAzureCredential());");
        Console.WriteLine("");
        Console.WriteLine("   // Secret rotation handling");
        Console.WriteLine("   services.Configure<SecretManagerOptions>(options =>");
        Console.WriteLine("   {");
        Console.WriteLine("       options.ReloadOnChange = true;");
        Console.WriteLine("       options.RefreshInterval = TimeSpan.FromMinutes(5);");
        Console.WriteLine("   });");
        Console.WriteLine("");
        Console.WriteLine("   // Accessing secrets");
        Console.WriteLine("   var connectionString = configuration[\"ConnectionStrings:DefaultConnection\"];");
        Console.WriteLine("   var apiKey = configuration[\"ExternalApi:ApiKey\"];");

        // Simulate secret management
        var secrets = new Dictionary<string, (string Value, DateTime LastRotated)>
        {
            ["database-password"] = ("encrypted-db-password", DateTime.Now.AddDays(-10)),
            ["api-key"] = ("encrypted-api-key", DateTime.Now.AddDays(-30)),
            ["storage-key"] = ("encrypted-storage-key", DateTime.Now.AddDays(-5))
        };

        Console.WriteLine("   Current secrets status:");
        foreach (var (name, (value, lastRotated)) in secrets)
        {
            var daysSinceRotation = (DateTime.Now - lastRotated).Days;
            var status = daysSinceRotation > 30 ? "⚠️ Needs rotation" : "✓ Current";
            Console.WriteLine($"     {name}: {status} (last rotated {daysSinceRotation} days ago)");
        }
        Console.WriteLine("");
    }

    /// <summary>
    /// Demonstrates feature flag management
    /// </summary>
    private static void FeatureFlagExample()
    {
        Console.WriteLine("6. Feature Flag Management:");
        Console.WriteLine("   // Feature flag configuration");
        Console.WriteLine("   services.AddFeatureManagement()");
        Console.WriteLine("           .AddFeatureFilter<PercentageFilter>()");
        Console.WriteLine("           .AddFeatureFilter<TargetingFilter>();");
        Console.WriteLine("");
        Console.WriteLine("   // Usage in code");
        Console.WriteLine("   if (await featureManager.IsEnabledAsync(\"NewDocumentProcessor\"))");
        Console.WriteLine("   {");
        Console.WriteLine("       // Use new processing logic");
        Console.WriteLine("   }");
        Console.WriteLine("");
        Console.WriteLine("   // Gradual rollout configuration");
        Console.WriteLine("   {");
        Console.WriteLine("     \"FeatureManagement\": {");
        Console.WriteLine("       \"NewDocumentProcessor\": {");
        Console.WriteLine("         \"EnabledFor\": [");
        Console.WriteLine("           { \"Name\": \"Percentage\", \"Parameters\": { \"Value\": 25 } }");
        Console.WriteLine("         ]");
        Console.WriteLine("       }");
        Console.WriteLine("     }");
        Console.WriteLine("   }");

        // Simulate feature flags
        var featureFlags = new Dictionary<string, (bool Enabled, int RolloutPercentage, string[] TargetUsers)>
        {
            ["NewDocumentProcessor"] = (true, 25, new[] { "admin@company.com", "beta-user@company.com" }),
            ["EnhancedML"] = (false, 0, Array.Empty<string>()),
            ["RealTimeProcessing"] = (true, 100, Array.Empty<string>())
        };

        Console.WriteLine("   Feature flags status:");
        foreach (var (name, (enabled, percentage, targets)) in featureFlags)
        {
            var status = enabled ? $"✓ Enabled ({percentage}%)" : "✗ Disabled";
            var targetInfo = targets.Length > 0 ? $" + {targets.Length} targeted users" : "";
            Console.WriteLine($"     {name}: {status}{targetInfo}");
        }
        Console.WriteLine("");
    }

    /// <summary>
    /// Demonstrates configuration audit trails for compliance
    /// </summary>
    private static void ConfigurationAuditExample()
    {
        Console.WriteLine("7. Configuration Audit Trails:");
        Console.WriteLine("   // Audit middleware");
        Console.WriteLine("   app.UseMiddleware<ConfigurationAuditMiddleware>();");
        Console.WriteLine("");
        Console.WriteLine("   // Configuration change tracking");
        Console.WriteLine("   public class ConfigurationAuditService");
        Console.WriteLine("   {");
        Console.WriteLine("       public void LogConfigurationChange(string key, string oldValue, string newValue, string user)");
        Console.WriteLine("       {");
        Console.WriteLine("           var auditEntry = new ConfigurationAuditEntry");
        Console.WriteLine("           {");
        Console.WriteLine("               Key = key,");
        Console.WriteLine("               OldValue = oldValue,");
        Console.WriteLine("               NewValue = newValue,");
        Console.WriteLine("               ChangedBy = user,");
        Console.WriteLine("               ChangedAt = DateTime.UtcNow");
        Console.WriteLine("           };");
        Console.WriteLine("           // Store audit entry");
        Console.WriteLine("       }");
        Console.WriteLine("   }");

        // Simulate audit trail
        var auditEntries = new List<(DateTime Timestamp, string Key, string OldValue, string NewValue, string User)>
        {
            (DateTime.Now.AddDays(-1), "MaxDocumentSize", "10MB", "20MB", "admin@company.com"),
            (DateTime.Now.AddHours(-6), "FeatureFlags:NewProcessor", "false", "true", "devops@company.com"),
            (DateTime.Now.AddHours(-2), "ConnectionStrings:Database", "[hidden]", "[hidden]", "system"),
            (DateTime.Now.AddMinutes(-30), "ML:ConfidenceThreshold", "0.7", "0.8", "ml-engineer@company.com")
        };

        Console.WriteLine("   Recent configuration changes:");
        foreach (var (timestamp, key, oldVal, newVal, user) in auditEntries)
        {
            Console.WriteLine($"     {timestamp:yyyy-MM-dd HH:mm} | {key}");
            Console.WriteLine($"       {oldVal} → {newVal} (by {user})");
        }
        Console.WriteLine("");
    }

    /// <summary>
    /// Demonstrates configuration-as-code with GitOps
    /// </summary>
    private static void ConfigurationAsCodeExample()
    {
        Console.WriteLine("8. Configuration-as-Code (GitOps):");
        Console.WriteLine("   // Infrastructure as Code with Bicep");
        Console.WriteLine("   resource keyVault 'Microsoft.KeyVault/vaults@2021-10-01' = {");
        Console.WriteLine("     name: keyVaultName");
        Console.WriteLine("     location: location");
        Console.WriteLine("     properties: {");
        Console.WriteLine("       tenantId: subscription().tenantId");
        Console.WriteLine("       sku: { family: 'A', name: 'standard' }");
        Console.WriteLine("     }");
        Console.WriteLine("   }");
        Console.WriteLine("");
        Console.WriteLine("   // GitHub Actions deployment");
        Console.WriteLine("   - name: Deploy Configuration");
        Console.WriteLine("     uses: azure/arm-deploy@v1");
        Console.WriteLine("     with:");
        Console.WriteLine("       subscriptionId: ${{ secrets.AZURE_SUBSCRIPTION }}");
        Console.WriteLine("       resourceGroupName: ${{ vars.RESOURCE_GROUP }}");
        Console.WriteLine("       template: ./infrastructure/main.bicep");
        Console.WriteLine("       parameters: ./infrastructure/params.${{ matrix.environment }}.json");

        // Simulate GitOps workflow
        var environments = new[] { "development", "staging", "production" };
        var deploymentStatus = new Dictionary<string, (string Status, DateTime LastDeployed, string CommitHash)>
        {
            ["development"] = ("✓ Success", DateTime.Now.AddMinutes(-15), "abc123ef"),
            ["staging"] = ("⚠️ Pending", DateTime.Now.AddDays(-1), "def456gh"),
            ["production"] = ("✓ Success", DateTime.Now.AddDays(-3), "ghi789jk")
        };

        Console.WriteLine("   Deployment status:");
        foreach (var env in environments)
        {
            if (deploymentStatus.TryGetValue(env, out var status))
            {
                Console.WriteLine($"     {env}: {status.Status} | Last: {status.LastDeployed:yyyy-MM-dd HH:mm} | Commit: {status.CommitHash}");
            }
        }
        Console.WriteLine("");
    }
}

// Supporting configuration classes
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
