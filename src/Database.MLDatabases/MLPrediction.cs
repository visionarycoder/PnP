namespace Database.MLDatabases;

public class MLPrediction
{
    public string Id { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public Dictionary<string, object> InputFeatures { get; set; } = new();
    public Dictionary<string, object> Result { get; set; } = new();
    public double Confidence { get; set; }
    public DateTime CreatedAt { get; set; }
}