using Azure.Core;
using Azure.Storage.Blobs;

using Microsoft.Data.SqlClient;

namespace CSharp.AzureManagedIdentity;

public interface IManagedIdentityService
{
    Task<AccessToken> GetAccessTokenAsync(string resource, CancellationToken cancellationToken = default);
    Task<AccessToken> GetAccessTokenAsync(string[] scopes, CancellationToken cancellationToken = default);
    Task<string> GetSecretAsync(string keyVaultUrl, string secretName, CancellationToken cancellationToken = default);
    Task<SqlConnection> GetSqlConnectionAsync(string connectionString, CancellationToken cancellationToken = default);
    Task<BlobServiceClient> GetBlobServiceClientAsync(string storageAccountUrl, CancellationToken cancellationToken = default);
    TokenCredential GetCredential(string? clientId = null);
}