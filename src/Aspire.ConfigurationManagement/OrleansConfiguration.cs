namespace Aspire.ConfigurationManagement;

public class OrleansConfiguration
{
    public const string SectionName = "Orleans";
    
    public string ClusterId { get; set; } = "document-processing-cluster";
    public string ServiceId { get; set; } = "DocumentProcessorService";
    public ConnectionStrings ConnectionStrings { get; set; } = new();
    public ClusteringOptions Clustering { get; set; } = new();
    public DashboardOptions Dashboard { get; set; } = new();
}