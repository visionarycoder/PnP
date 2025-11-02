using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CSharp.AzureManagedIdentity;

// Managed Identity service implementation
public class ManagedIdentityService : IManagedIdentityService
{
    private readonly ManagedIdentityOptions options;
    private readonly ILogger<ManagedIdentityService> logger;
    private readonly Dictionary<string, TokenCredential> credentialCache;
    private readonly SemaphoreSlim credentialCacheLock;

    public ManagedIdentityService(
        IOptions<ManagedIdentityOptions> optionsAccessor,
        ILogger<ManagedIdentityService> logger)
    {
        options = optionsAccessor.Value;
        this.logger = logger;
        credentialCache = new();
        credentialCacheLock = new(1, 1);
    }

    public async Task<AccessToken> GetAccessTokenAsync(string resource, CancellationToken cancellationToken = default)
    {
        var credential = GetCredential();
        var tokenRequestContext = new TokenRequestContext(new[] { $"{resource}/.default" });
        
        try
        {
            var token = await credential.GetTokenAsync(tokenRequestContext, cancellationToken);
            logger.LogDebug("Successfully obtained access token for resource: {Resource}", resource);
            return token;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to obtain access token for resource: {Resource}", resource);
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
            logger.LogDebug("Successfully obtained access token for scopes: {Scopes}", string.Join(", ", scopes));
            return token;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to obtain access token for scopes: {Scopes}", string.Join(", ", scopes));
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
            logger.LogDebug("Successfully retrieved secret: {SecretName} from Key Vault: {KeyVaultUrl}", secretName, keyVaultUrl);
            return response.Value.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve secret: {SecretName} from Key Vault: {KeyVaultUrl}", secretName, keyVaultUrl);
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
            logger.LogDebug("Successfully established SQL connection using managed identity");
            return connection;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to establish SQL connection using managed identity");
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
            
            logger.LogDebug("Successfully created Blob Service client for: {StorageAccountUrl}", storageAccountUrl);
            return client;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Blob Service client for: {StorageAccountUrl}", storageAccountUrl);
            throw;
        }
    }

    public TokenCredential GetCredential(string? clientId = null)
    {
        var cacheKey = clientId ?? "system";
        
        credentialCacheLock.Wait();
        try
        {
            if (credentialCache.TryGetValue(cacheKey, out var cachedCredential))
            {
                return cachedCredential;
            }

            TokenCredential credential = CreateCredential(clientId);
            credentialCache[cacheKey] = credential;
            return credential;
        }
        finally
        {
            credentialCacheLock.Release();
        }
    }

    private TokenCredential CreateCredential(string? clientId = null)
    {
        var credentialOptions = new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = false,
            ExcludeWorkloadIdentityCredential = false,
            ExcludeManagedIdentityCredential = false,
            ExcludeSharedTokenCacheCredential = !options.EnableLocalDevelopment,
            ExcludeVisualStudioCredential = !options.EnableLocalDevelopment,
            ExcludeVisualStudioCodeCredential = !options.EnableLocalDevelopment,
            ExcludeAzureCliCredential = !options.EnableLocalDevelopment,
            ExcludeAzurePowerShellCredential = !options.EnableLocalDevelopment,
            ExcludeInteractiveBrowserCredential = true
        };

        if (!string.IsNullOrEmpty(options.TenantId))
        {
            credentialOptions.TenantId = options.TenantId;
        }

        // If a specific client ID is provided, use user-assigned identity
        if (!string.IsNullOrEmpty(clientId))
        {
            credentialOptions.ManagedIdentityClientId = clientId;
            logger.LogDebug("Creating credential with user-assigned managed identity: {ClientId}", clientId);
        }
        else if (!string.IsNullOrEmpty(options.UserAssignedClientId) && !options.UseSystemAssigned)
        {
            credentialOptions.ManagedIdentityClientId = options.UserAssignedClientId;
            logger.LogDebug("Creating credential with configured user-assigned managed identity: {ClientId}", options.UserAssignedClientId);
        }
        else
        {
            logger.LogDebug("Creating credential with system-assigned managed identity");
        }

        return new DefaultAzureCredential(credentialOptions);
    }
}