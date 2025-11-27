namespace Aspire.ConfigurationManagement;

public class ConnectionStrings
{
    public string DefaultConnection { get; set; } = string.Empty;
    public string ClusteringConnection { get; set; } = string.Empty;
    public string CacheConnection { get; set; } = string.Empty;
}