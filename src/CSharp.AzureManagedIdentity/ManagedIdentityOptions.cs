using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

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

// Managed Identity service interface