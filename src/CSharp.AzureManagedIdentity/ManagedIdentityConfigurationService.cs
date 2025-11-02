using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CSharp.AzureManagedIdentity;

// Configuration service interface
public interface IManagedIdentityConfigurationService
{
    Task<string> GetConfigurationValueAsync(string key, CancellationToken cancellationToken = default);
    Task<T> GetConfigurationValueAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task RefreshConfigurationAsync(CancellationToken cancellationToken = default);
}

// Managed Identity configuration service
public class ManagedIdentityConfigurationService : IManagedIdentityConfigurationService
{
    private readonly IManagedIdentityService managedIdentityService;
    private readonly IConfiguration configuration;
    private readonly ILogger<ManagedIdentityConfigurationService> logger;
    private readonly Dictionary<string, object> configCache;
    private readonly SemaphoreSlim cacheLock;

    public ManagedIdentityConfigurationService(
        IManagedIdentityService managedIdentityService,
        IConfiguration configuration,
        ILogger<ManagedIdentityConfigurationService> logger)
    {
        this.managedIdentityService = managedIdentityService;
        this.configuration = configuration;
        this.logger = logger;
        configCache = new();
        cacheLock = new(1, 1);
    }

    public async Task<string> GetConfigurationValueAsync(string key, CancellationToken cancellationToken = default)
    {
        // Check local configuration first
        var localValue = configuration[key];
        if (!string.IsNullOrEmpty(localValue) && !IsKeyVaultReference(localValue))
        {
            return localValue;
        }

        // Check cache
        await cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (configCache.TryGetValue(key, out var cachedValue) && cachedValue is string stringValue)
            {
                return stringValue;
            }
        }
        finally
        {
            cacheLock.Release();
        }

        // Resolve Key Vault reference
        if (IsKeyVaultReference(localValue))
        {
            var (keyVaultUrl, secretName) = ParseKeyVaultReference(localValue!);
            var secretValue = await managedIdentityService.GetSecretAsync(keyVaultUrl, secretName, cancellationToken);
            
            // Cache the result
            await cacheLock.WaitAsync(cancellationToken);
            try
            {
                configCache[key] = secretValue;
            }
            finally
            {
                cacheLock.Release();
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
            logger.LogError(ex, "Failed to deserialize configuration value for key: {Key}", key);
            throw;
        }
    }

    public async Task RefreshConfigurationAsync(CancellationToken cancellationToken = default)
    {
        await cacheLock.WaitAsync(cancellationToken);
        try
        {
            configCache.Clear();
            logger.LogInformation("Configuration cache cleared");
        }
        finally
        {
            cacheLock.Release();
        }
    }

    private static bool IsKeyVaultReference(string? value)
    {
        return !string.IsNullOrEmpty(value) && value.StartsWith("@Microsoft.KeyVault(", StringComparison.OrdinalIgnoreCase);
    }

    private static (string keyVaultUrl, string secretName) ParseKeyVaultReference(string reference)
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