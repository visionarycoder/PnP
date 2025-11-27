namespace Aspire.ConfigurationManagement;

public class ClusteringOptions
{
    public string Provider { get; set; } = "AdoNet";
    public int SiloPort { get; set; } = 11111;
    public int GatewayPort { get; set; } = 30000;
    public bool EnableDistributedTracing { get; set; } = true;
}