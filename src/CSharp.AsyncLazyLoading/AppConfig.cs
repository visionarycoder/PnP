namespace CSharp.AsyncLazyLoading;

public class AppConfig
{
    public string DatabaseConnectionString { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public int MaxConcurrentUsers { get; set; }
    public bool EnableFeatureX { get; set; }
}