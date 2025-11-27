namespace Aspire.ConfigurationManagement;

public class MLConfiguration
{
    public string ModelPath { get; set; } = "./models";
    public bool UseRemoteModels { get; set; } = false;
    public string RemoteModelEndpoint { get; set; } = string.Empty;
    public ModelSettings TextClassification { get; set; } = new();
    public ModelSettings SentimentAnalysis { get; set; } = new();
    public ModelSettings TopicModeling { get; set; } = new();
}