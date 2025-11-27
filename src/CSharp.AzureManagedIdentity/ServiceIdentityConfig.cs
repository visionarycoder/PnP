namespace CSharp.AzureManagedIdentity;

public class ServiceIdentityConfig
{
    public string? ResourceId { get; set; }
    public string? Scope { get; set; }
    public string[]? Scopes { get; set; }
    public string? ClientId { get; set; } // For user-assigned identity
}