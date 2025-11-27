namespace Database.MLDatabases;

public class ModelPerformanceMetric
{
    public string ExperimentId { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public int InferenceTimeMs { get; set; }
    public double MemoryUsageMb { get; set; }
    public DateTime Timestamp { get; set; }
}