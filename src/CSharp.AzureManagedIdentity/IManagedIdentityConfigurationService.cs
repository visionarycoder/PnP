namespace CSharp.AzureManagedIdentity;

public interface IManagedIdentityConfigurationService
{
    Task<string> GetConfigurationValueAsync(string key, CancellationToken cancellationToken = default);
    Task<T> GetConfigurationValueAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task RefreshConfigurationAsync(CancellationToken cancellationToken = default);
}