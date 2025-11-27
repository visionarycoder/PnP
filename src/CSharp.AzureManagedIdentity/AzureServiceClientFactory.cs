using Azure.Storage.Blobs;

namespace CSharp.AzureManagedIdentity;

public class AzureServiceClientFactory : IAzureServiceClientFactory
{
    private readonly IManagedIdentityService managedIdentityService;

    public AzureServiceClientFactory(IManagedIdentityService managedIdentityService)
    {
        this.managedIdentityService = managedIdentityService;
    }

    public BlobServiceClient CreateBlobServiceClient(string storageAccountUrl)
    {
        var credential = managedIdentityService.GetCredential();
        return new BlobServiceClient(new Uri(storageAccountUrl), credential);
    }

    public T CreateClient<T>(string serviceUrl) where T : class
    {
        var credential = managedIdentityService.GetCredential();
        
        // This is a simplified factory - in practice, you'd have specific implementations
        // for different Azure service clients
        if (typeof(T) == typeof(BlobServiceClient))
        {
            return (T)(object)new BlobServiceClient(new Uri(serviceUrl), credential);
        }

        throw new NotSupportedException($"Client type {typeof(T).Name} is not supported");
    }
}