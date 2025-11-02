using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.AspNetCore.Builder;

namespace CSharp.AzureManagedIdentity;

// Azure service client factory interface
public interface IAzureServiceClientFactory
{
    BlobServiceClient CreateBlobServiceClient(string storageAccountUrl);
    T CreateClient<T>(string serviceUrl) where T : class;
}

// Azure service client factory implementation
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

        return services;
    }

    public static IServiceCollection AddManagedIdentity(
        this IServiceCollection services,
        Action<ManagedIdentityOptions> configureOptions)
    {
        services.Configure(configureOptions);
        
        services.AddSingleton<IManagedIdentityService, ManagedIdentityService>();
        services.AddSingleton<IAzureServiceClientFactory, AzureServiceClientFactory>();
        services.AddSingleton<IManagedIdentityConfigurationService, ManagedIdentityConfigurationService>();

        return services;
    }

    public static IServiceCollection AddAzureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAzureClients(clientBuilder =>
        {
            var managedIdentityService = services.BuildServiceProvider().GetRequiredService<IManagedIdentityService>();
            var credential = managedIdentityService.GetCredential();

            // Configure Azure clients with managed identity
            var storageUrl = configuration["Azure:StorageAccount:Url"];
            if (!string.IsNullOrEmpty(storageUrl))
            {
                clientBuilder.AddBlobServiceClient(new Uri(storageUrl))
                    .WithCredential(credential);
            }

            var keyVaultUrl = configuration["Azure:KeyVault:Url"];
            if (!string.IsNullOrEmpty(keyVaultUrl))
            {
                clientBuilder.AddSecretClient(new Uri(keyVaultUrl))
                    .WithCredential(credential);
            }
        });

        return services;
    }

    public static IApplicationBuilder UseManagedIdentityHealthCheck(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ManagedIdentityHealthCheckMiddleware>();
    }
}