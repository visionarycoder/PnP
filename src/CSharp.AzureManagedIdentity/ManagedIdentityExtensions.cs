using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.AspNetCore.Builder;

namespace CSharp.AzureManagedIdentity;

// Azure service client factory interface

// Azure service client factory implementation

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