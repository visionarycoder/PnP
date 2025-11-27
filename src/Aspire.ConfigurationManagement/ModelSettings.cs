namespace Aspire.ConfigurationManagement;

public class ModelSettings
{
    public string ModelName { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public double ConfidenceThreshold { get; set; } = 0.7;
    public bool EnableCaching { get; set; } = true;
    public int CacheExpirationMinutes { get; set; } = 60;
}