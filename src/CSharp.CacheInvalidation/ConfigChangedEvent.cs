namespace CSharp.CacheInvalidation;

public class ConfigChangedEvent
{
    public string ConfigKey { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
}