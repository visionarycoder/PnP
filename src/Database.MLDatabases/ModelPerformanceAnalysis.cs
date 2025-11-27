namespace Database.MLDatabases;

public class ModelPerformanceAnalysis
{
    public string ModelName { get; set; } = string.Empty;
    public double AverageAccuracy { get; set; }
    public double AccuracyStandardDeviation { get; set; }
    public int TotalRuns { get; set; }
    public int P95InferenceTime { get; set; }
    public double AverageMemoryUsage { get; set; }
}