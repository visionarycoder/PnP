using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CSharp.AzureManagedIdentity;

// Managed Identity middleware for health checks
public class ManagedIdentityHealthCheckMiddleware
{
    private readonly RequestDelegate next;
    private readonly IManagedIdentityService managedIdentityService;
    private readonly ILogger<ManagedIdentityHealthCheckMiddleware> logger;

    public ManagedIdentityHealthCheckMiddleware(
        RequestDelegate next,
        IManagedIdentityService managedIdentityService,
        ILogger<ManagedIdentityHealthCheckMiddleware> logger)
    {
        this.next = next;
        this.managedIdentityService = managedIdentityService;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Equals("/health/managed-identity", StringComparison.OrdinalIgnoreCase))
        {
            await HandleHealthCheckAsync(context);
            return;
        }

        await next(context);
    }

    private async Task HandleHealthCheckAsync(HttpContext context)
    {
        try
        {
            // Test managed identity by getting a token for Azure Resource Manager
            var token = await managedIdentityService.GetAccessTokenAsync("https://management.azure.com/");
            
            var response = new
            {
                Status = "Healthy",
                TokenExpiry = token.ExpiresOn,
                Message = "Managed Identity is working correctly"
            };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Managed Identity health check failed");
            
            context.Response.StatusCode = 503;
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                Status = "Unhealthy",
                Error = ex.Message,
                Message = "Managed Identity is not working correctly"
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}