namespace Database.MLDatabases;

public class ModelPerformanceTrend
{
    public DateTime Hour { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public double AverageAccuracy { get; set; }
    public double AverageInferenceTime { get; set; }
    public long RequestCount { get; set; }
    public double P95InferenceTime { get; set; }
    public double P99InferenceTime { get; set; }
}