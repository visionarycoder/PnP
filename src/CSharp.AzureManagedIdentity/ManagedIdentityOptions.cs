using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CSharp.AzureManagedIdentity;

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