using Azure.Storage.Blobs;

namespace CSharp.AzureManagedIdentity;

public interface IAzureServiceClientFactory
{
    BlobServiceClient CreateBlobServiceClient(string storageAccountUrl);
    T CreateClient<T>(string serviceUrl) where T : class;
}