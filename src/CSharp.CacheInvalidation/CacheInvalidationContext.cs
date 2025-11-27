namespace CSharp.CacheInvalidation;

public class CacheInvalidationContext
{
    public string TriggerKey { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}